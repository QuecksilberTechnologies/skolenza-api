# Student admissions API handoff for UI developer

Shared-key rotation is **not implemented**. Do not replace an institution key as a prerequisite for testing these routes; historical portal emails, gateway credentials and other institution data require a coordinated migration. Synthetic compatibility evidence is in the [rotation scenario](../testing/student/portal-email-key-rotation/2026-10-30.md).

## Guardian fee self service - source update 30 October 2026

Five Guardian endpoints are in the uncommitted API source. They use the existing
`StudentPortal` bearer policy; these routes do not use staff ModuleId or
OperationId. Institution and account come from signed token claims. The account
must have a currently active Guardian persona linked to the requested student;
an optional Student persona alone does not authorize these endpoints. Admission
must belong to that student and institution, be approved, and have a fee quote.
Other students, admissions or installments are not exposed. An invalid or
missing portal token returns 401; unavailable or unauthorized records return
404. Subject to current configured exception middleware.

Base: `/api/Student/portal/students/{studentId}/fees`. IDs are 64-bit.

| Method and path | Purpose | Input |
| --- | --- | --- |
| GET `/admissions?pageNumber=1&pageSize=10` | Approved admissions with quote, session, campus and group summary | Paged; defaults 1 and 10 |
| GET `/admissions/{admissionId}/quote` | Immutable fee components, discount and installment plan | Admission ID from admissions response |
| GET `/admissions/{admissionId}/balance` | Current installment balances, overdue and paid amounts | Admission ID |
| GET `/admissions/{admissionId}/receipts?pageNumber=1&pageSize=10` | Receipts restricted to the chosen admission | Paged; defaults 1 and 10 |
| POST `/admissions/{admissionId}/installments/{installmentId}/cashfree-checkout` | Creates or resumes an idempotent school-owned Cashfree order for the server-calculated balance | JSON `{"idempotencyKey":"parent-order-2028-001"}`; max 100 characters |

Example request (replace placeholder IDs and token):

```http
GET /api/Student/portal/students/101/fees/admissions/202/balance
Authorization: Bearer <portal-access-token>
```

Representative balance envelope (amounts are examples, not school fees):

```json
{"isSucceeded":true,"message":"Student fee balance retrieved successfully.","data":{"studentAdmissionId":202,"studentId":101,"quoteId":303,"currencyCode":"INR","currencyDecimalPlaces":2,"netAnnualAmount":12000,"totalLateFeeAmount":0,"totalPaidAmount":3000,"totalOutstandingAmount":9000,"overdueAmount":0,"installments":[{"id":404,"quoteId":303,"installmentNumber":1,"dueDate":"2028-04-10","recurringAmount":3000,"oneTimeAmount":0,"lateFeeAmount":0,"paidAmount":3000,"outstandingAmount":0,"status":3}]}}
```

Checkout success returns `data.paymentAttemptId`, `gatewayOrderId`,
`paymentSessionId`, `currencyCode`, `amount` and `expiresAt`. Reuse the same
idempotency key only for retries of the same installment; generate a new key
for a new attempt. The server decides the amount. The school must configure
its own active Cashfree credentials; three-decimal currencies are rejected for
Cashfree checkout. The payment callback is not proof of payment: rely on the
server-side webhook/reconciliation and refresh balance/receipts. No Guardian
reconciliation API is included. A previously issued payment session can be
returned on an idempotent retry.

Access is rechecked in the payment-attempt database transaction. If access is
revoked first, checkout is denied. An attempt accepted before revocation may
finish its provider request; revocation does not cancel an existing provider
order. Subsequent requests require a currently authorized Guardian link.

Other representative responses (illustrative IDs, labels and amounts):

```json
{"isSucceeded":true,"message":"Student fee admissions retrieved successfully.","data":[{"admissionId":202,"academicSessionId":12,"academicSessionName":"Academic Year 2028-29","campusId":2,"campusName":"Main Campus","academicGroupId":5,"academicGroupName":"Grade 5","quoteId":303,"currencyCode":"INR","netAnnualAmount":12000}],"pageNumber":1,"pageSize":10,"totalRecords":1,"totalPages":1}
```

```json
{"isSucceeded":true,"message":"Student fee quote retrieved successfully.","data":{"id":303,"studentAdmissionId":202,"currencyCode":"INR","currencyDecimalPlaces":2,"discountPercent":0,"grossRecurringAmount":12000,"discountAmount":0,"oneTimeAmount":0,"netAnnualAmount":12000,"installmentsPerYear":4,"lines":[{"componentCode":"TUITION","componentName":"Tuition","chargeNature":1,"annualAmount":12000,"discountAmount":0,"netAmount":12000}],"installments":[]}}
```

```json
{"isSucceeded":true,"message":"Student fee receipts retrieved successfully.","data":[{"id":505,"receiptNumber":"REC-EXAMPLE","studentAdmissionId":202,"studentFeeInstallmentId":404,"paymentTransactionId":606,"amount":3000,"currencyCode":"INR","issuedAt":"2028-04-10T09:00:00Z","paymentMethod":1,"recordedReference":"offline-example"}],"pageNumber":1,"pageSize":10,"totalRecords":1,"totalPages":1}
```

```json
{"isSucceeded":true,"message":"Cashfree checkout session created successfully.","data":{"paymentAttemptId":707,"gatewayOrderId":"S-example","paymentSessionId":"example-provider-session","currencyCode":"INR","amount":3000,"expiresAt":"2028-04-10T09:15:00Z"}}
```

Treat the above as examples, not exact serialized payload snapshots: other
fields may be present. The central options API supplies numeric statuses and
methods. On checkout validation or provider errors, inspect the HTTP status and
the common API error envelope (`isSucceeded`, `message`, `errors`, optional
`errorCode`); handle retry and configuration failures without assuming payment
was completed.

No schema change or new staff permission mapping was needed for these routes.
Local synthetic authenticated HTTP tests cover all four reads, wrong student and
admission, anonymous access, checkout and retry, and a wrong installment. The
checkout HTTP test uses a fake Cashfree provider. Real Cashfree checkout/webhooks,
production hosting and deployment remain unverified. UI integration is pending
with the UI developer. Detailed results: [Guardian fee scenario](../testing/student/guardian-fee-portal/2026-10-30.md).

> **Codex continuation:** For current implementation order and resume
> instructions, read [Student API continuation handoff](../STUDENT_CONTINUATION.md).
> Latest source scenario: [approval readiness and currency precision](../testing/student/admission-approval-currency-precision/2026-10-28.md).
> Word copy for UI review: [Student Fees and Guardian API handoff](Student_Fees_Guardian_API_Handoff.docx).

Status: **API source is implemented in the current uncommitted working tree. Local and Render schema application and parity are verified (38/38 tables per target; identical columns/constraints/index fingerprint); four dynamic modules have active View/Add/Update/Delete operation mappings in both. Restore-tested backups exist. Student tests: 93/93 pass with disposable PostgreSQL concurrency cases enabled (2026-10-29); protected gate: 41 pass, 0 skips, 0 fail; Release build: 0 errors, 22 warnings. Local anonymous smoke: Swagger 200 and protected Student options 401; 59 Student/CountryCurrency routes discovered. Authenticated workflows, role grants, provider round trips and deployed acceptance are not verified. Angular is untouched; API is not deployed. See [schema parity and local runtime report](../testing/student/schema-parity-and-runtime-smoke/2026-10-28.md).**

## Newly confirmed Student lifecycle and identifier decisions (2026-10-27)

- Student deletion is an exit operation: preserve the Student and related history, close the active enrollment, mark the Student exited/inactive, and append one `StudentExitHistory` event in the same transaction. No physical delete endpoint. Record previous and destination school/country when known.
- Generate a Skolenza institution-unique `StudentCode` separately from government identifiers. Store official education IDs in country/authority-qualified Student identifier records with verification state and provenance. Do not synthesize APAAR/UPN values. APAAR issuance is through UDISE and requires PEN; England's UPN follows the DfE/local-authority generator process. See the [official APAAR portal](https://apaar.education.gov.in/) and [DfE UPN guidance](https://www.gov.uk/government/publications/unique-pupil-numbers).
- The internal StudentCode, admission lifecycle, Student/enrollment creation and exit-history API now exist in the working tree. The additive schema is `database-scripts/CreateStudentAdmissionsAndRecords.sql`; it has been applied to Local and Render after restore-tested backups. Country/authority identifier-type CRUD now exists in source; encrypted storage and verification APIs are implemented; authenticated Local/Render acceptance remains pending. Do not generate APAAR/UPN values locally.
Updated: 2026-10-28

## Identifier type contract

All Student-owned primary keys and foreign keys are 64-bit (`long` in API
contracts and `bigint` in PostgreSQL). This includes the four implemented
master IDs, `TemplateSourceId`, and the benefit scheme's
`EvidenceRequirementId`. Local was converted from integer to bigint while
preserving existing values; Local and Render column and identity-sequence types
were queried after migration. `CountryId` and `StateId` keep the existing
geography catalogue types (`int`) because those IDs belong to shared tables.
Every update/delete route uses the ASP.NET `:long` route constraint to match its
.NET `long` parameter and PostgreSQL `bigint` key; callers may send IDs above
the signed 32-bit range without the router rejecting the request.
The current .NET JSON settings serialize `long` as a JSON number; consumers
must preserve 64-bit values and must not narrow IDs to 32-bit integers. If IDs
may exceed JavaScript's safe integer range, agree on a string-ID wire format
before that scale is reached.

Admission status values are centralized in `StudentAdmissionStatus`: Draft=1,
Submitted=2, UnderReview=3, Approved=4, Rejected=5,
ReturnedForCorrection=6 and Cancelled=7. User-approved transitions:
Draft→Submitted/Cancelled; Submitted→UnderReview/Cancelled;
UnderReview→Approved/Rejected/ReturnedForCorrection; ReturnedForCorrection→
Submitted/Cancelled. Rejection, correction return and cancellation require a
reason. Status changes append `StudentAdmissionStatusHistory` rows.

## Student admission and exit APIs (implemented in source; schema applied; authenticated runtime acceptance pending)

All staff routes use the existing authenticated Institution Staff bearer token
and the dynamic `STUDENT_ADMISSIONS` module permission. Resolve ModuleId and
OperationId through the authenticated menu/permission response. The API takes
institution and actor IDs from the trusted server context. All `{id}` routes
bind 64-bit IDs.

### Centrally defined Student workflow values

`GET /api/Student/options?moduleId={resolved}&operationId={resolved}` returns
values sourced directly from `StudentEnums.cs`, without a second UI catalogue.
It requires the authenticated Institution Staff bearer token and the existing
`STUDENT_ADMISSIONS` View permission. Institution, campus and session
configurable values (document requirements, fee frequency, fee components,
benefit schemes and education identifier types) remain in their respective
master APIs and must be loaded there rather than copied from this response.

The response envelope's `data` contains `admissionStatuses`,
`lifecycleStatuses`, `enrollmentStatuses`, `documentPersonScopes`,
`feeChargeNatures`, `educationIdentifierVerificationStatuses` and `exitReasons`.
It also returns `guardianRelationships` (Father=1, Mother=2, Guardian=3,
Grandparent=4, Other=5).
Each entry has a numeric `value` and enum `name`, for example:

```json
{
  "isSucceeded": true,
  "message": "Student workflow options retrieved successfully.",
  "data": {
    "admissionStatuses": [
      { "value": 1, "name": "Draft" },
      { "value": 2, "name": "Submitted" },
      { "value": 3, "name": "UnderReview" },
      { "value": 4, "name": "Approved" },
      { "value": 5, "name": "Rejected" },
      { "value": 6, "name": "ReturnedForCorrection" },
      { "value": 7, "name": "Cancelled" }
    ],
    "lifecycleStatuses": [],
    "enrollmentStatuses": [],
    "documentPersonScopes": [],
    "feeChargeNatures": [],
    "educationIdentifierVerificationStatuses": [],
    "exitReasons": [],
    "guardianRelationships": [
      { "value": 1, "name": "Father" },
      { "value": 2, "name": "Mother" },
      { "value": 3, "name": "Guardian" },
      { "value": 4, "name": "Grandparent" },
      { "value": 5, "name": "Other" }
    ]
  }
}
```

The empty arrays are abbreviated illustrative output; the running API returns
all members for every enum. Send the numeric `value`, not the enum name, in
write requests.

| Method | Route | Purpose | Permission |
|---|---|---|---|
| GET | `/api/Student/options` | Centrally sourced Student workflow enum values for dropdown/status controls. | View |
| GET | `/api/Student/admissions` | Paged list; optional session/campus/status filters. Defaults are page 1 and 10 records. | View |
| GET | `/api/Student/records` | Paged institution-scoped Student records; optional session/campus/group filters; exited learners are excluded unless `includeExited=true`. Defaults are page 1 and 10. | View |
| POST | `/api/Student/admissions` | Create a Draft application. | Add |
| PUT | `/api/Student/admissions/{id}` | Edit Draft or Returned for Correction. | Update |
| POST | `/api/Student/admissions/{id}/review` | Submit, move to review, approve, reject, return, or cancel under the approved transition map. | Update |
| DELETE | `/api/Student/records/{id}` | Record exit, close active enrollment, mark Student inactive and append exit history. It never physically deletes. | Delete |
| GET | `/api/Student/records/{id}/exit-history` | Paged exit history for a Student in the caller's institution. Defaults are page 1 and 10. | View |
| GET | `/api/Student/guardians` | Paged institution Guardian directory; `activeOnly` defaults true; optional name/email/phone search. | View |
| POST | `/api/Student/guardians` | Create Guardian profile; does not issue credentials. | Add |
| PUT | `/api/Student/guardians/{id}` | Update Guardian profile; changes are shared across its Student links. | Update |
| GET | `/api/Student/records/{id}/guardians` | List Student's active links; `includeInactive=true` includes retained closed links. | View |
| POST | `/api/Student/records/{id}/guardians` | Link an existing Guardian profile to a Student. | Add |
| PUT | `/api/Student/records/{id}/guardians/{linkId}` | Update relationship/access flags or effective dates; Guardian identity cannot be swapped. | Update |
| DELETE | `/api/Student/records/{id}/guardians/{linkId}` | Close the link, end portal access and retain history/profile. JSON body includes `effectiveTo` and permission fields. | Delete |

POST/PUT admission JSON fields: `academicSessionId`, `campusId`,
`academicGroupId`, `firstName`, optional `middleName`, `lastName`,
`dateOfBirth` (`YYYY-MM-DD`), `genderId`, and `countryId`, plus dynamic
`moduleId` and `operationId`. IDs are validated against the authenticated
institution and their parent relationships. Example (IDs are illustrative):

```json
{
  "moduleId": "resolve-from-authenticated-menu",
  "operationId": "resolve-Add-for-STUDENT_ADMISSIONS",
  "academicSessionId": 2028,
  "campusId": 3,
  "academicGroupId": 17,
  "firstName": "Aarav",
  "middleName": null,
  "lastName": "Sharma",
  "dateOfBirth": "2018-04-12",
  "genderId": 1,
  "countryId": 105
}
```

Review JSON carries `status` and optional `reason`. Reason is mandatory for
Rejected, ReturnedForCorrection and Cancelled. Before an Approved transition,
the source requires exactly one active primary Guardian, all captured Guardian
links to remain active, every applicable required Student/Guardian document to
have an approved current version that is not expired, and a saved immutable fee
quote with its complete installment schedule. Student, first StudentEnrollment,
copied Guardian links and admission status/history are saved through the
existing transaction. A source contract asserts readiness validation precedes
Student creation; PostgreSQL transaction/rollback and authenticated HTTP remain
unverified. StudentCode is `STD-` plus the generated institution-unique
ApplicationNumber; it is not an official national education identifier.

Exit JSON carries `effectiveExitDate`, `exitReason` (Transfer=1, Graduation=2,
Withdrawal=3, Other=4), optional `previousSchoolName`,
`previousSchoolCountryId`, `destinationSchoolName`,
`destinationSchoolCountryId` and `notes`, plus `moduleId` / `operationId` for
Delete. A repeat exit returns Conflict and cannot create duplicate history.
The transaction stores the `StudentExitHistory` event, closes an active
enrollment as Withdrawn and changes Student lifecycle to Exited/inactive.

Errors follow the common API response: 400 invalid fields/status/references,
401 invalid staff identity, 403 denied module operation, 404 absent or
other-institution record, 409 invalid lifecycle transition or duplicate exit.
These are synchronous requests; cancellation uses the request token. Retry GET
only; POST can create a new application and has no idempotency key.

### Guardian profile, admission capture and Student link contracts (source implemented; Local/Render schema applied)

Guardian directory uses the same institution staff bearer and
`STUDENT_ADMISSIONS` permission pipeline. Profile IDs and Student/link IDs are
64-bit. Email is trimmed and normalized to lowercase; it is unique per
institution among non-deleted Guardian profiles. A profile can be linked to
multiple Students. Linking never creates portal credentials; a link's
`hasPortalAccess` is only the relationship-level authorization flag for the
future Guardian portal.

Create a profile with `POST /api/Student/guardians` using Add permission:

```json
{
  "moduleId": "resolve-from-authenticated-menu",
  "operationId": "resolve-Add-for-STUDENT_ADMISSIONS",
  "firstName": "Kavita",
  "middleName": null,
  "lastName": "Sharma",
  "email": "kavita.sharma@example.test",
  "phoneNumber": "+91-9000000000"
}
```

Link the returned `id` using `POST /api/Student/records/{studentId}/guardians`:

```json
{
  "moduleId": "resolve-from-authenticated-menu",
  "operationId": "resolve-Add-for-STUDENT_ADMISSIONS",
  "guardianId": 42,
  "relationshipType": 1,
  "isPrimary": true,
  "isEmergencyContact": true,
  "isPickupAuthorized": false,
  "hasPortalAccess": true,
  "effectiveFrom": "2026-10-28",
  "effectiveTo": null
}
```

`relationshipType` must come from `GET /api/Student/options`;
`moduleId`/`operationId` values above are explanatory placeholders only. A
Student may have at most one active primary Guardian. Duplicate active
Student/Guardian links and duplicate normalized emails in one institution are
rejected. Updates cannot move a link to another Guardian; create a new link and
close the old link. DELETE sets link inactive, clears primary/portal access and
records `effectiveTo`; it does not delete the Guardian profile. The parent
login/authentication API is a later phase, so this endpoint does not prove
email/phone ownership or send invitations.

Admission drafts capture existing Guardian profiles before a Student row
exists. First create or find an institution Guardian using the directory above,
then attach it to the Draft or Returned for Correction admission. These
admission links are retained independently from the eventual Student links.

Admission detail and audit reads use the same authenticated
`STUDENT_ADMISSIONS` View permission and institution scope as the paged list:

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/Student/admissions/{id:long}?moduleId={resolved}&operationId={resolved}` | Return current admission fields and, after approval, linked Student ID/code; cross-institution and missing IDs return 404. |
| GET | `/api/Student/admissions/{id:long}/status-history?pageNumber=1&pageSize=10` | Return paged append-only admission status transitions, earliest first. Defaults are page 1 and 10 rows. |

Status history returns `id`, `admissionId`, nullable `fromStatus`, `toStatus`,
`reason`, `changedById`, and UTC `changedDateTime`. `fromStatus` is null only
for the initial Draft creation event (whose stored source status is zero).
Status values use the central Student options API. The response uses the
standard `ApiResponse` envelope and paging metadata. These routes have source
contract coverage; they have not been exercised against PostgreSQL or Local
HTTP. See the [admission detail/history scenario report](../testing/student/admission-detail-status-history/2026-10-28.md)
for current source regression evidence and the [portal document report](../testing/student/portal-document-upload/2026-10-28.md)
for portal-upload coverage.

For the single-admission read, resolve the `STUDENT_ADMISSIONS` module and
its `View` operation through the authenticated menu/permission flow, then send
both IDs as query parameters. A request without positive permission IDs is
rejected; an unrelated module, non-View operation, or denied role is forbidden.
Example, with symbolic values to be replaced by the resolved IDs:

```http
GET /api/Student/admissions/123?moduleId={studentAdmissionsModuleId}&operationId={viewOperationId}
Authorization: Bearer {staffAccessToken}
```

The controller and permission-behavior tests for this contract are recorded in
the [2026-10-29 permission behavior scenario](../testing/student/permission-behavior/2026-10-29.md).
Local Swagger exposes the permission query parameters and anonymous admission
detail/reconciliation requests returned 401; authenticated Local HTTP and
deployed acceptance are still pending.

Admission status transitions, Draft edits, admission Guardian link changes,
admission document metadata/review and quote creation are serialized on the
institution-scoped admission row. If a competing write has made the admission
ineligible, reload its current state before asking the user to try again; do
not blindly repeat the previous change. Conflict and validation responses use
the existing API error envelope. The tested interleavings and their limits are
in the [admission concurrency scenario](../testing/student/admission-concurrency/2026-10-29.md).

| Method | Route | Purpose | Permission operation |
|---|---|---|---|
| GET | `/api/Student/admissions/{id}/guardians?includeInactive=false` | List captured admission Guardian relationships | View |
| POST | `/api/Student/admissions/{id}/guardians` | Attach an existing active institution Guardian | Add |
| PUT | `/api/Student/admissions/{id}/guardians/{linkId}` | Change relationship/access flags; GuardianId is immutable | Update |
| DELETE | `/api/Student/admissions/{id}/guardians/{linkId}` | Close the admission relationship while retaining it | Delete |

All admission and link IDs are `long` and route-constrained with `:long`.
Mutation is allowed only while the admission is Draft or Returned for
Correction. GuardianId must identify an active, non-deleted Guardian in the
authenticated institution. Relationship choices come from
`GET /api/Student/options`; one active primary Guardian is enforced by API
validation and a filtered unique database index. Duplicate active Guardian
links are rejected. A closed link clears primary and portal-access flags and
sets its effective end date. The Guardian profile itself is never removed.

Example `POST /api/Student/admissions/{admissionId}/guardians` body:

```json
{
  "moduleId": "resolve-from-authenticated-menu",
  "operationId": "resolve-Add-for-STUDENT_ADMISSIONS",
  "guardianId": 42,
  "relationshipType": 1,
  "isPrimary": true,
  "isEmergencyContact": true,
  "isPickupAuthorized": false,
  "hasPortalAccess": true,
  "effectiveFrom": "2026-10-28",
  "effectiveTo": null
}
```

GET returns the relationship fields plus link ID and Guardian profile. POST/PUT
return the created/updated relationship. Invalid relationship/date inputs
return the common 400 response; invalid institution or Guardian ownership
returns 404; denied role operation returns 403; duplicate link/primary or
immutable Guardian reassignment returns the common conflict/validation response.
Requests are synchronous and cancellation propagates; only GET is safe to retry
automatically.

When an admission is approved, exactly one active primary Guardian link is
required, all captured links must still be active, every applicable required
Student/Guardian document must have an approved current unexpired version, and
an immutable fee quote with its matching installment schedule must exist. The
source validates these conditions before Student creation, then copies Guardian
links and creates Student + Enrollment and status history inside the existing
approval transaction. The source contract passes; actual PostgreSQL rollback
and authenticated HTTP behavior remain unverified.

Student list query parameters include `moduleId`, `operationId`, `pageNumber`,
`pageSize`, optional `academicSessionId`, `campusId`, `academicGroupId`, and
`includeExited`. Exit history accepts `moduleId`, `operationId`, `pageNumber`
and `pageSize`. The API validates the Student institution before returning its
exit history; it does not expose a cross-institution row.

Required schema: `Student`, `StudentAdmission`,
`StudentAdmissionStatusHistory`, `StudentEnrollment`, `Guardian`,
`StudentAdmissionGuardian`, `StudentGuardian`, `StudentExitHistory`, `StudentEducationIdentifierMaster`
and `StudentEducationIdentifier`; script:
`database-scripts/CreateStudentAdmissionsAndRecords.sql`. It adds no sample
student or personal data. Database application, role grants and authenticated
HTTP verification are pending. Do not call these APIs against Local/Render
until this schema script and the corrected existing master operation mappings
are applied after a recoverable backup.

## Current implemented API

### Purpose and ownership

The school administrator configures required or optional evidence per institution. A rule can be scoped to an existing `Campus` (physical location) and `AcademicSession`; a null scope means it applies across all campuses or sessions. Student and Guardian requirements can be configured separately or together. No default document list is seeded. The examples in the supplied screenshot (birth certificate, address/identity proof, photo, transfer certificate, marksheet, caste/medical documents) are optional examples; each school chooses its own local requirements.

All routes require the existing authenticated Institution Staff bearer token and the dynamic `STUDENT_ADMISSIONS` module permission. Institution and actor IDs are taken from the verified server context, never from request data. `ModuleId` and `OperationId` must be resolved from the authenticated menu/permission response after applying the migration. Do not persist numeric IDs in Angular. The API verifies that the selected Operation row is active and matches the HTTP operation (View/Add/Update/Delete), then uses the existing institution permission pipeline. The platform catalogue names creation permission `Add`; POST remains the HTTP method. Host credentials are not accepted by this feature.

### Routes

Base route: `/api/Student/admission-document-requirements`

| Method | Route | Purpose | Permission operation |
|---|---|---|---|
| GET | `/api/Student/admission-document-requirements` | Paged list, optionally scoped to a campus/session/person | View |
| POST | `/api/Student/admission-document-requirements` | Create an institution master | Add |
| PUT | `/api/Student/admission-document-requirements/{id}` | Update a master | Update |
| DELETE | `/api/Student/admission-document-requirements/{id}` | Soft-delete a master | Delete |

For PUT and DELETE, `{id}` is constrained as a signed 64-bit value (`:long`),
not a 32-bit `:int` route parameter.

The GET query accepts `ModuleId`, `OperationId`, `PageNumber` (default 1), `PageSize` (default 10), `CampusId`, `AcademicSessionId`, `ActiveOnly` (default true), and `AppliesTo` (`Student=1`, `Guardian=2`). A selected campus/session returns both global rules and rules matching the selection. `AppliesTo=Student` or `Guardian` also includes `StudentAndGuardian=3` rules. Results sort by `DisplayOrder`, `Name`, then `Id` ascending. The response uses the established `ApiResponse` paginated envelope and `PaginationDefaults`/`PagedResponseDTO`.

### Create/update JSON

```json
{
  "moduleId": "resolve-from-authenticated-menu",
  "operationId": "resolve-Add-or-Update-for-module",
  "code": "BIRTH_CERTIFICATE",
  "name": "Birth certificate",
  "description": "Document used to verify date of birth",
  "campusId": 14,
  "academicSessionId": 28,
  "appliesTo": 1,
  "isRequired": true,
  "allowedMimeTypes": ["application/pdf", "image/jpeg", "image/png"],
  "maxFileSizeBytes": 10485760,
  "requiresExpiryDate": false,
  "displayOrder": 10,
  "isActive": true
}
```

`campusId` and `academicSessionId` may be null for a wider institution-wide scope. `appliesTo` is an enum, not a master table. At least one MIME type is required; the API stores normalized MIME values as JSONB. `maxFileSizeBytes` must be 1–104857600. The API validates that supplied Campus and AcademicSession are active, not soft-deleted, and belong to the authenticated institution. Code is trimmed and uppercased. Codes are unique among non-deleted records in the same institution/campus/session scope. Delete is soft; it preserves the master row and audit values.

### Representative response

```json
{
  "isSucceeded": true,
  "message": "Student document requirements retrieved successfully.",
  "errors": [],
  "data": [
    {
      "id": 7,
      "code": "BIRTH_CERTIFICATE",
      "name": "Birth certificate",
      "description": "Document used to verify date of birth",
      "campusId": 14,
      "academicSessionId": 28,
      "appliesTo": 1,
      "isRequired": true,
      "allowedMimeTypes": ["application/pdf", "image/jpeg", "image/png"],
      "maxFileSizeBytes": 10485760,
      "requiresExpiryDate": false,
      "displayOrder": 10,
      "isActive": true
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalRecords": 1,
  "totalPages": 1
}
```

### Expected errors

- 400: missing/invalid code or name, invalid `AppliesTo`, invalid MIME list/file-size limit, or a Campus/AcademicSession outside the institution.
- 401: no valid Institution Staff bearer identity.
- 403: module code mismatch or operation does not match the route / permission denied.
- 404: update/delete target is absent, soft-deleted, or belongs to another institution.
- 409: duplicate live code in the same scope.

### Persistence and upload boundary

Table: `skolenza.StudentDocumentRequirementMaster`, using the existing institution `*Master` audit/soft-delete fields. The additive script is `database-scripts/CreateStudentDocumentRequirementMasterAndModule.sql`; it creates the table, scope indexes, the `STUDENT_ADMISSIONS` module, and dynamic View/Add/Update/Delete operation mappings. It intentionally does not grant role permissions; an institution administrator must grant the relevant current operations through the existing permission management flow. Apply or rerun the idempotent schema/module script before deploying the API.

This API configures requirements only. Admission uploads and staff review are implemented separately below. Post-enrollment Guardian/Student portal upload is documented in the portal document section at the end of this handoff.

## Admission document upload, review and download APIs (implemented in working tree)

These endpoints are staff-assisted: an authenticated Institution Staff member uploads a Student or linked Guardian document against an admission requirement; an authorized reviewer then approves, rejects, or returns the submitted version for correction. Separate post-enrollment Guardian/Student self-service endpoints are documented in the portal document section at the end of this handoff.

All routes require the current Institution Staff bearer identity and the existing `STUDENT_ADMISSIONS` permission pipeline. The API resolves InstitutionId and actor from the authenticated context. Resolve current ModuleId/OperationId from the authenticated menu/permission response; never hardcode IDs. Reads/downloads use `View`, upload uses `Add`, and review uses `Update`.

### Routes

| Method | Route | Purpose | Permission |
|---|---|---|---|
| POST | `/api/Student/admissions/{id}/documents` | Multipart upload and create a new version | Add |
| GET | `/api/Student/admissions/{id}/documents` | Paged document-version list | View |
| GET | `/api/Student/admissions/{id}/documents/{documentId}/download` | Issue a five-minute signed URL after institution/admission scoping | View |
| POST | `/api/Student/admissions/{id}/documents/{documentId}/review` | Approve, reject, or return for correction | Update |

All path IDs use `:long`. List query parameters include `moduleId`, `operationId`, `pageNumber` (default 1), `pageSize` (default 10), and optional `personScope`, `guardianId`, and `status`. Page totals use the existing `ApiResponse` paginated envelope.

### Multipart upload

Send `multipart/form-data`; the binary field is `file`. `requirementId`, `personScope`, `moduleId`, and `operationId` are required. `guardianId` is required for Guardian or StudentAndGuardian evidence and must identify an active Guardian linked to this admission; it must be omitted for Student-only evidence. `expiryDate` is required only when the selected requirement says so.

```text
moduleId=<current STUDENT_ADMISSIONS module id>
operationId=<current Add operation id>
requirementId=27
personScope=1
expiryDate=2028-06-30   # only if required by the requirement master
file=@birth-certificate.pdf
```

The API enforces the requirement's configured MIME list and byte ceiling, plus an absolute 100 MB limit; verifies the admission is institution-owned and not terminal; checks requirement Campus/Session applicability and Guardian linkage; stores a generated object name beneath the institution/admission path in the existing object-storage service; computes SHA-256; and creates version and initial history rows. A failed database save triggers object cleanup. Declared content type is checked against the requirement; byte-signature/malware scanning has not been implemented or accepted.

A version can be replaced only after the prior version is Rejected or ReturnedForCorrection. Pending or Approved versions cannot be overwritten. Rejections and returns require a reason. Review history is retained; there is no document hard-delete endpoint. Signed download URLs expire after five minutes.

### Review JSON

```json
{
  "moduleId": "resolve-from-authenticated-menu",
  "operationId": "resolve-Update-for-module",
  "status": 4,
  "reason": "Readable copy received"
}
```

`status` values are served by `GET /api/Student/options` in `documentReviewStatuses`: `AwaitingReview=1`, `Approved=2`, `Rejected=3`, `ReturnedForCorrection=4`. Only AwaitingReview documents can be reviewed. `reason` is mandatory for Rejected and ReturnedForCorrection and limited to 1000 characters. Do not duplicate the enum in Angular.

### Representative response and errors

Successful upload/review returns an `ApiResponse<StudentAdmissionDocumentResponseDTO>` with document ID, requirement code/name, scope, Guardian ID where applicable, version number, superseded document ID, original file name, content type, size, SHA-256, expiry, current status, submitter/reviewer IDs and timestamps, and reason. The file contents and object key are not returned. The download route returns a temporary URL string in the standard response envelope.

- 400: empty/oversized file, unacceptable configured MIME/size, invalid scope/Guardian combination, invalid expiry, or invalid review transition/reason.
- 401: no valid Institution Staff identity.
- 403: wrong module/operation or permission denied.
- 404: admission/document/requirement/Guardian is unavailable in this institution or scope.
- 409: disallowed duplicate or version/review state conflict.

Requests are synchronous and honor cancellation through HTTP, hashing, and database operations. `IFileStorageService.UploadFileAsync` currently has no cancellation-token parameter; the existing storage abstraction was reused. Do not automatically retry upload POST because the endpoint does not accept an idempotency key. GET may be retried. Review POST should only be retried after reading current status.

Persistence: `StudentAdmissionDocument` stores version metadata/object key/checksum; `StudentAdmissionDocumentReviewHistory` stores append-only review decisions. Both are mapped in EF and included in `database-scripts/CreateStudentAdmissionsAndRecords.sql`; the script is applied to Local and Render and verified in the schema parity report. No role grant, authenticated HTTP upload, S3 round-trip, malware scan or deployed acceptance has been run. See the [admission document scenario report](../testing/student/admission-documents/2026-10-28.md).

## Fee configuration APIs (implemented locally)

## Benefit-scheme master API (implemented in working tree; schema applied)

School administrators configure benefit/concession schemes through the existing
authenticated Institution Staff pipeline. Scheme codes and labels are
institution data, not hardcoded catalogue values. Optional Country, State,
Campus and Academic Session references scope schemes to existing geography and
academic records; omitted scope means institution-wide. State requires a
Country; the state must belong to that country. A selected Campus must belong
to the authenticated institution and agree with its submitted Country/State.

The master stores a minimum and maximum discount percentage (0–100, with up to
two decimal places), eligibility description, optional active document
requirement for evidence, display order and active state. Admission quote
calculation applies one active scheme at most and checks the selected discount
against those bounds; only recurring charges are discounted.

Base route: `/api/Student/benefit-scheme-masters`

| Method | Route | Purpose | Permission operation |
|---|---|---|---|
| GET | `/api/Student/benefit-scheme-masters` | Paged, location/session-applicable schemes | View |
| POST | `/api/Student/benefit-scheme-masters` | Create scheme | Add |
| PUT | `/api/Student/benefit-scheme-masters/{id}` | Update scheme | Update |
| DELETE | `/api/Student/benefit-scheme-masters/{id}` | Soft-delete scheme | Delete |

For PUT and DELETE, `{id}` is constrained as a signed 64-bit value (`:long`),
not a 32-bit `:int` route parameter.

GET accepts `ModuleId`, `OperationId`, `PageNumber` (default 1), `PageSize`
(default 10), `ActiveOnly` (default true), and optional `CountryId`, `StateId`,
`CampusId`, and `AcademicSessionId`. When filtering by a selected scope, global
records and records matching that scope are included. The client must resolve
the current permission IDs via the authenticated menu/permission response.

```json
{
  "moduleId": "resolve-from-authenticated-menu",
  "operationId": "resolve-Add-or-Update-for-STUDENT_ADMISSIONS",
  "code": "STATE_STUDENT_SUPPORT",
  "name": "State student support",
  "countryId": 105,
  "stateId": 12,
  "campusId": 14,
  "academicSessionId": 28,
  "minimumDiscountPercent": 5.00,
  "maximumDiscountPercent": 25.00,
  "eligibilityDescription": "Eligibility is reviewed by authorized school staff.",
  "evidenceRequirementId": 6,
  "displayOrder": 10,
  "isActive": true
}
```

Errors use the common API response: 400 for invalid bounds or mismatched
geography/session/campus/evidence scope, 401 for invalid Institution Staff
identity, 403 for a denied module operation, 404 for missing or other-
institution records and 409 for a duplicate live code. The endpoint stores the
scheme bounds but does not calculate a fee quote.

Persistence table: `skolenza.StudentBenefitSchemeMaster`. The table and the
initial `STUDENT_ADMISSIONS` mappings are applied in Local and Render. The
initial mapping audit found the Add operation was omitted because the first
script version selected `Create`; the corrected source now selects `Add`, but
the script must be rerun against both databases before API deployment. The source script
is `database-scripts/CreateStudentBenefitSchemeMaster.sql`; the identifier
width alignment script is
`database-scripts/MigrateStudentMasterIdentifiersToBigint.sql`. The migration
preserves current values and widens identity sequences; it reuses
`STUDENT_ADMISSIONS`, discovers active operation IDs from `Operation` and
creates no role grants.

Both fee-master families use the authenticated Institution Staff bearer token
and `STUDENT_FEES` module with the existing runtime permission pipeline.
Resolve ModuleId and operation IDs dynamically from the authenticated menu or
permissions response. Do not hardcode numeric IDs. The migration creates
View/Add/Update/Delete operation mappings dynamically and does not grant
those operations to a role; role grants must use the existing permission
management flow.

### Frequency master

Base route: `/api/Student/fee-frequency-masters`

| Method | Route | Purpose | Permission operation |
|---|---|---|---|
| GET | `/api/Student/fee-frequency-masters` | Paged frequency catalogue | View |
| POST | `/api/Student/fee-frequency-masters` | Create a frequency | Add |
| PUT | `/api/Student/fee-frequency-masters/{id}` | Update a frequency | Update |
| DELETE | `/api/Student/fee-frequency-masters/{id}` | Soft-delete a frequency | Delete |

For PUT and DELETE, `{id}` is constrained as a signed 64-bit value (`:long`),
not a 32-bit `:int` route parameter.

GET query: `ModuleId`, `OperationId`, `PageNumber` (default 1), `PageSize`
(default 10), and `ActiveOnly` (default true). Ordering is DisplayOrder,
InstallmentsPerYear, Name, Id. The school defines frequency labels and
installments-per-year (1 through 12). A later fee-structure API will associate
allowed frequencies with each structure; this master alone does not make every
frequency available to every parent.

```json
{
  "moduleId": "resolve-from-authenticated-menu",
  "operationId": "resolve-Add-or-Update-for-STUDENT_FEES",
  "code": "QUARTERLY",
  "name": "Quarterly",
  "description": "Four installments in one academic year",
  "installmentsPerYear": 4,
  "displayOrder": 20,
  "isActive": true
}
```

### Fee-component master

Base route: `/api/Student/fee-component-masters`

| Method | Route | Purpose | Permission operation |
|---|---|---|---|
| GET | `/api/Student/fee-component-masters` | Paged component catalogue; may filter by `ChargeNature` | View |
| POST | `/api/Student/fee-component-masters` | Create a component | Add |
| PUT | `/api/Student/fee-component-masters/{id}` | Update a component | Update |
| DELETE | `/api/Student/fee-component-masters/{id}` | Soft-delete a component | Delete |

For PUT and DELETE, `{id}` is constrained as a signed 64-bit value (`:long`),
not a 32-bit `:int` route parameter.

GET query: `ModuleId`, `OperationId`, `PageNumber` (default 1), `PageSize`
(default 10), `ActiveOnly` (default true), and optional `ChargeNature`
(`RecurringAnnual=1`, `OneTime=2`). Ordering is DisplayOrder, ChargeNature,
Name, Id. No fee labels are seeded. The institution defines labels. Only
RecurringAnnual lines will be eligible for the approved benefit discount base;
OneTime admission/examination fees are excluded.

```json
{
  "moduleId": "resolve-from-authenticated-menu",
  "operationId": "resolve-Add-or-Update-for-STUDENT_FEES",
  "code": "TUITION",
  "name": "Annual tuition",
  "description": "Recurring charge for the academic year",
  "chargeNature": 1,
  "displayOrder": 10,
  "isActive": true
}
```

### Fee-master responses, errors and persistence

Paged GET responses use the standard `ApiResponse` array envelope with
`pageNumber`, `pageSize`, `totalRecords` and `totalPages`. A representative
frequency `data` row is:

```json
{
  "id": 18,
  "code": "QUARTERLY",
  "name": "Quarterly",
  "description": "Four installments in one academic year",
  "installmentsPerYear": 4,
  "displayOrder": 20,
  "isActive": true
}
```

Component `data` rows return the same common fields plus `chargeNature` (1 or
2). Common errors: 400 for missing code/name, installments outside 1–12 or
unknown ChargeNature; 401 for missing/invalid Institution Staff identity; 403
for wrong module/operation or denied permission; 404 for a missing, deleted or
other-institution row; 409 for duplicate live code. POST retries with the same
code will conflict; PUT is repeatable for the same body; DELETE is a soft
delete and a repeated delete returns 404. These are synchronous JSON APIs:
there is no polling endpoint or background job. Request cancellation is
propagated by the API CancellationToken. Client retry should be limited to GET
or a PUT whose desired state is unchanged; no POST idempotency key exists.

Tables: `skolenza.StudentFeeFrequencyMaster` and
`skolenza.StudentFeeComponentMaster`, each following the existing institution
`*Master` audit/soft-delete convention. Apply
`database-scripts/CreateStudentFeeMastersAndModule.sql` before deploying the
API. It creates `STUDENT_FEES`, dynamic View/Add/Update/Delete operation mappings and indexes; it does
not seed fee labels/frequency rows or role grants. Both additive scripts have
been applied and verified in local PostgreSQL (`SkolenzaDB`) and Render
PostgreSQL (`skolenza`). No authenticated PostgreSQL HTTP CRUD scenario has
been run; deploy the matching API code and assign permissions through the
existing role pipeline before UI integration.

## Fee ledger APIs (source implemented; schema applied; authenticated runtime acceptance pending)

Base route: `/api/student/fees`. All routes require an authenticated Institution
Staff bearer token. Resolve `ModuleId` and `OperationId` from the authenticated
menu/permission pipeline; the module code is `STUDENT_FEES`. Reads use View,
creates/payments use Add, updates use Update, and structure retirement uses
Delete. Do not hardcode numeric permission IDs.

| Method | Route | Purpose | Permission |
|---|---|---|---|
| GET | `/structures` | Paged structure list; optional CampusId, AcademicSessionId, AcademicGroupId, ActiveOnly | View |
| POST | `/structures` | Create campus/session/group fee structure with active fee lines and permitted frequencies/due dates | Add |
| PUT | `/structures/{id:long}` | Replace a structure only before a quote references it | Update |
| DELETE | `/structures/{id:long}` | Deactivate an unquoted structure | Delete |
| GET | `/quotes` | Paged immutable quote/installment list | View |
| POST | `/admissions/{admissionId:long}/quotes` | Create the sole immutable quote for an eligible admission | Add |
| GET | `/admissions/{admissionId:long}/balance` | Return quote totals, paid/outstanding/overdue values and installment breakdown | View |
| POST | `/installments/{installmentId:long}/offline-payments` | Record an idempotent staff-entered payment; transaction and receipt commit together | Add |
| GET | `/receipts` | Paged receipts; optional StudentAdmissionId and StudentFeeInstallmentId filters | View |

Structure creation requires a campus, academic session and Academic Group. The
group must belong to that session; section-specific pricing is not supported.
Currency is read from the active primary CountryCurrencyMaster for the campus
country. Every enabled frequency supplies exactly its configured installment
count and an explicit nondecreasing due date for each installment. Do not send
currency or precision as an authoritative client value. Host configures the
country currency and its supported minor-unit precision in
`CountryCurrencyMaster`; the API resolves the active primary campus-country
mapping. Supported `decimalPlaces` values are **0, 2, or 3**, and the server
rounds midpoint away from zero. Host currency responses expose `decimalPlaces`;
fee structure and quote responses expose `currencyDecimalPlaces`, which is
snapshotted so later currency-master edits do not alter an existing quote.
Once a quote exists, the structure is immutable and cannot be deactivated.

Example Host currency mapping request (permission IDs must be resolved from
the authenticated menu/permission response; country and precision are not
client-supplied on Student fee requests):

| Method | Route | Permission |
|---|---|---|
| GET | `/api/host/country-currency-masters` | View |
| POST | `/api/host/country-currency-masters` | Add |
| PUT | `/api/host/country-currency-masters/{id:long}` | Update |
| DELETE | `/api/host/country-currency-masters/{id:long}` | Delete (deactivates; retains history) |

```json
{
  "moduleId": "resolve-from-authenticated-menu",
  "operationId": "resolve-Add-for-HOST_COUNTRY_CURRENCY_MASTER",
  "countryId": 105,
  "currencyCode": "INR",
  "currencyName": "Indian Rupee",
  "currencySymbol": "₹",
  "decimalPlaces": 2,
  "isPrimary": true,
  "isActive": true,
  "effectiveFrom": "2026-04-01",
  "effectiveTo": null
}
```

Example structure POST body (IDs below are placeholders; permission IDs must
be dynamically resolved):

```json
{
  "moduleId": 42,
  "operationId": 7,
  "campusId": 3,
  "academicSessionId": 8,
  "academicGroupId": 15,
  "lateFeePerInstallment": 100,
  "gracePeriodDays": 5,
  "lateFeeCap": 100,
  "items": [
    { "feeComponentMasterId": 2, "amount": 36000 },
    { "feeComponentMasterId": 9, "amount": 2500 }
  ],
  "frequencies": [
    {
      "feeFrequencyMasterId": 1,
      "installments": [
        { "installmentNumber": 1, "dueDate": "2027-04-10" },
        { "installmentNumber": 2, "dueDate": "2027-08-10" },
        { "installmentNumber": 3, "dueDate": "2027-12-10" },
        { "installmentNumber": 4, "dueDate": "2028-02-10" }
      ]
    }
  ],
  "isActive": true
}
```

Example quote POST body:

```json
{
  "moduleId": 42,
  "operationId": 7,
  "studentFeeStructureId": 21,
  "studentFeeStructureFrequencyId": 31,
  "studentBenefitSchemeMasterId": 5,
  "discountPercent": 12.5
}
```

Only one active benefit scheme may apply. The staff-selected percentage must
fall within that scheme's bounds and applies to recurring annual amounts only.
Without a scheme the discount is 0%. Amounts use the precision snapshotted from
the active campus-country currency (`currencyDecimalPlaces`: 0, 2, or 3),
midpoint away from zero. Recurring annual charges are divided across the
selected installments, with any rounding remainder in the last installment.
One-time admission/exam charges are placed in installment 1. The quote snapshots
lines, currency, currency precision and schedule; later master edits do not
change it. The response contains `currencyCode`, `currencySymbol` and
`currencyDecimalPlaces`.

Example offline payment body:

```json
{
  "moduleId": 42,
  "operationId": 7,
  "idempotencyKey": "fee-installment-103-payment-1",
  "amount": 9100,
  "recordedReference": "BANK-REF-REDACTED",
  "paidAt": "2027-04-09T10:30:00Z"
}
```

The server locks the installment and derives the outstanding amount from the
persisted quote/installment; the submitted amount cannot exceed that balance.
Repeating the same idempotency key for the same installment and amount returns
the existing transaction and receipt. Reusing it with different payment details
returns 409. Receipt metadata is paged at page 1 / 10 by default. `GET
/admissions/{id}/balance` reports the quote currency, net annual amount, total
paid, current outstanding, overdue outstanding and installment statuses. Late
fee on a balance read is calculated from the configured grace period for an
overdue unpaid installment; a payment operation persists the late-fee amount.
That read does not write to the ledger.

Persistence is defined in `database-scripts/CreateStudentFeeLedgerAndCountryCurrencyMaster.sql`:
CountryCurrencyMaster, StudentFeeStructure and its item/frequency/due-date
children, immutable StudentAdmissionFeeQuote and lines, StudentFeeInstallment,
StudentFeePaymentAttempt, StudentFeePaymentTransaction, StudentFeePaymentWebhookEvent,
and StudentFeeReceipt. **The SQL has not been applied to Local or Render.**
No Local authenticated HTTP request or database scenario has run. The example
responses are contract illustrations, not observed server payloads. Standard
errors include 400 validation, 401 missing/invalid staff identity, 403 denied
permission, 404 cross-institution or missing resource, and 409 immutable quote
or idempotency conflict. CancellationToken is propagated; these APIs are
synchronous and have no polling route.

### School-owned Cashfree credential configuration — source implemented, not deployed

The institution configures its own Cashfree merchant credentials; these
endpoints are separate from Host subscription-billing configuration. The API
never returns client-secret or webhook-secret values. Secrets are protected
with the AES-GCM `IStudentGatewaySecretProtector`; Staff bank `IEncryptionService`
is unchanged. The current implementation derives the encryption key from the
active institution key, as approved by the user. Replacing an active institution
key without re-encrypting would make stored gateway ciphertext unreadable; key
rotation/re-encryption support remains a prerequisite before rotating keys.

`GET /api/student/fees/gateway-configuration?moduleId={resolved}&operationId={resolved}`
requires authenticated Staff `STUDENT_FEES` / `View` permission. The response
contains `gatewayCode`, numeric `environment`, masked `clientIdMasked`,
`hasClientSecret`, `hasWebhookSecret`, `isActive`, `version`, and `updatedAt`;
it has no secret fields. A missing configuration returns the standard 404.

`POST /api/student/fees/gateway-configuration` creates the institution's only
Cashfree configuration and requires `STUDENT_FEES` / `Add`. `PUT` on the same
route replaces all credentials/environment and requires `STUDENT_FEES` /
`Update`; include the current `version` for optimistic concurrency. For either
body, discover `moduleId` and `operationId` from the authenticated menu/permission
flow; do not copy numeric IDs from examples.

```json
{
  "moduleId": 0,
  "operationId": 0,
  "environment": 1,
  "clientId": "<school Cashfree app ID>",
  "clientSecret": "<school Cashfree API secret>",
  "webhookSecret": "<school Cashfree webhook secret>",
  "isActive": false,
  "version": 1
}
```

Environment enum values are served by the existing Student options endpoint as
`PaymentGatewayEnvironments`: `Sandbox=1`, `Production=2`. Never place real
credentials in UI source, logs, telemetry, screenshots, test fixtures or docs.
Use HTTPS. Do not auto-retry a failed POST; refresh configuration first. PUT
requires a fresh version after a `409` conflict. Requests propagate cancellation.

Persistence: `StudentPaymentGatewayConfiguration` in
`database-scripts/CreateStudentCashfreeGatewayConfiguration.sql`; its EF mapping
and API are in the working tree; SQL is applied to Local/Render, but no
authenticated HTTP verification has run. Response DTO example (the ordinary
Skolenza API envelope may wrap it; this is not an observed HTTP response):

```json
{
  "gatewayCode": "CASHFREE",
  "environment": 1,
  "clientIdMasked": "********1234",
  "hasClientSecret": true,
  "hasWebhookSecret": true,
  "isActive": false,
  "version": 1,
  "updatedAt": null
}
```

Errors: 400 invalid enum or invalid fields; 401 unauthenticated; 403 missing
dynamic permission; 404 missing configuration or institution context; 409
configuration already exists or stale version. Exact 400/401/403/404/409 envelope
is not verified over HTTP. Focused Cashfree provider + AES-GCM/API tests: **10
passed, 0 failed, 0 skipped**. Combined Student + protected Staff/Curriculum
gate: **86 passed, 0 failed, 9 skipped / 95 total**; DB-dependent skips are not
passes. See [configuration report](../testing/student/cashfree-gateway-configuration/2026-10-28.md)
and [tuition provider report](../testing/student/cashfree-tuition-payments/2026-10-28.md).

Cashfree's [Create Order API](https://www.cashfree.com/docs/api-reference/payments/latest/orders/create-order)
accepts at most two decimal places. The approved behavior is to reject a
3-decimal currency at checkout; keep the quoted balance unchanged and let staff
use offline payment entry. The API never rounds a 3-decimal amount to fit
Cashfree.

### Cashfree tuition checkout, reconciliation and webhook — source implemented; acceptance pending

These tuition endpoints use the school's own Cashfree merchant configuration
described above and the Student fee ledger. They do not use Host subscription
credentials. Amount and currency always come from the saved installment quote;
the client submits no amount. Cashfree order creation supports only 0- and
2-decimal currencies. A 3-decimal quote receives a validation error before any
provider call.

| Method | Route | Purpose | Permission |
|---|---|---|---|
| POST | `/api/student/fees/installments/{installmentId}/cashfree-checkout` | Creates or resumes the same order/session for an idempotency key. Returns provider session ID for Cashfree's checkout UI. | `STUDENT_FEES` / Add |
| POST | `/api/student/fees/cashfree-attempts/{attemptId}/reconcile` | Reads the order's payment list from Cashfree and posts a verified result. | `STUDENT_FEES` / Update |
| POST | `/api/student/payment-gateways/cashfree/webhook` | Receives Cashfree callback; validates raw-body HMAC, re-reads payment from Cashfree and atomically updates ledger/receipt/installment. | Cashfree signature; anonymous route by design |

Checkout request (resolve ModuleId/OperationId from authenticated menu data;
illustrative zeros are not IDs):

```json
{
  "moduleId": 0,
  "operationId": 0,
  "idempotencyKey": "admission-2028-installment-1-attempt-1"
}
```

Success returns `paymentAttemptId`, `gatewayOrderId`, `paymentSessionId`,
`currencyCode`, server-calculated `amount`, and `expiresAt`. Use
`paymentSessionId` to launch Cashfree checkout. Keep the attempt ID and poll the
reconcile endpoint after a customer returns; only the API's reconciled status
or an issued receipt is evidence of success. Do not mark success from a
browser redirect. The same idempotency key may be retried for the same
installment. Reuse for another installment returns 409. Provider order creation
uses a stable derived idempotency key and the saved gateway order ID.

Reconciliation can update the payment ledger, installment and receipt, so its
request must carry the dynamically resolved `STUDENT_FEES` **Update** operation
ID. A View-only role cannot perform this action:

```json
{ "moduleId": 0, "operationId": 0 }
```

The response includes attempt/order IDs, central attempt/payment statuses,
confirmed transaction ID and receipt number (null until successful), amount,
currency and paid time. Reconciliation queries Cashfree server-to-server; the
browser must not submit a payment status or provider-confirmed amount.

Configure Cashfree to POST the exact URL
`/api/student/payment-gateways/cashfree/webhook`. The webhook controller reads
the unmodified request body (maximum 64 KiB) and the headers
`x-webhook-signature`, `x-webhook-timestamp`, and optional
`x-idempotency-header`. The API verifies HMAC-SHA256 over
`timestamp + rawBody` with the school's encrypted webhook secret, then fetches
the payment from Cashfree before posting it. Repeated callback event IDs are
deduplicated; successful provider payment, transaction row, receipt, installment
balance and attempt state are saved in one serializable transaction. The
callback returns `{ "received": true }` on accepted processing. Configure a
public HTTPS URL and the webhook secret from that same school's Cashfree app.

Persistence uses `StudentFeePaymentAttempt`,
`StudentFeePaymentWebhookEvent`, `StudentFeePaymentTransaction`,
`StudentFeeReceipt` and `StudentFeeInstallment` in
`database-scripts/CreateStudentFeeLedgerAndCountryCurrencyMaster.sql`. The SQL
is applied to Local and Render. APIs and provider unit tests are in the working
tree; there is no authenticated Local HTTP, DB rollback, Cashfree sandbox or
Render verification yet. Focused Cashfree provider + AES-GCM/config tests:
**10 passed, 0 failed, 0 skipped**. Combined Student + protected
Staff/Curriculum gate: **86 passed, 0 failed, 9 skipped / 95 total**; the
skipped DB cases are not passes. See [Cashfree tuition scenario report](../testing/student/cashfree-tuition-payments/2026-10-28.md).

Errors include 400 for invalid amount/currency or malformed provider data, 401
for missing/invalid staff bearer token on authenticated routes, 403 for denied
permission, 404 for missing/cross-institution resources or configuration, 409
for idempotency/order/amount mismatch, and 502/provider error mapping when
Cashfree is unavailable. Exact HTTP error envelope has not been verified. The
UI must offer the already implemented offline path for unsupported 3-decimal
currencies. Host subscription-billing calls are not tuition evidence.

## Country/authority education identifier type master — implemented in source, not deployed

The school configures the identifier types its campus/country recognizes. The
API does not hardcode APAAR, UPN or any country's authority catalogue. Country
is validated against the shared active `Country` table; `Code` is unique per
institution and country, case-insensitively. `IssuingAuthority` remains
institution-configured text because no canonical global authority catalogue
was approved. All list requests are paged (default page 1, 10 records).

| Method | Route | Purpose | Permission |
|---|---|---|---|
| GET | `/api/Student/education-identifier-masters?countryId={countryId}&pageNumber=1&pageSize=10&activeOnly=true` | Paged institution-owned identifier types; country filter optional. | `STUDENT_ADMISSIONS` / View |
| POST | `/api/Student/education-identifier-masters` | Create type. Body fields: `countryId`, `issuingAuthority`, `code`, `name`, optional `description`, `displayOrder`, `isActive`, and dynamic `moduleId`/`operationId`. | `STUDENT_ADMISSIONS` / Add |
| PUT | `/api/Student/education-identifier-masters/{id}` | Update a type. `id` binds as `long`; body same as POST. | `STUDENT_ADMISSIONS` / Update |
| DELETE | `/api/Student/education-identifier-masters/{id}?moduleId={resolved}&operationId={resolved}` | Soft-delete. | `STUDENT_ADMISSIONS` / Delete |

Example create request (replace permission IDs with values from the
authenticated menu/permission flow):

```json
{
  "moduleId": 0,
  "operationId": 0,
  "countryId": 105,
  "issuingAuthority": "Configured national education authority",
  "code": "NATIONAL_EDU_ID",
  "name": "National education identifier",
  "description": "Authority-issued learner identifier for this campus location.",
  "displayOrder": 1,
  "isActive": true
}
```

The sample country and permission IDs are illustrative; resolve them
dynamically. List returns country name, issuer, code, name and active/display
metadata. Mutations require the existing staff bearer token and permission
pipeline; server tenant and actor come from the authenticated context. Duplicate
codes return 409, invalid/inactive country or field lengths return 400, missing
or cross-institution records return 404, and missing permission returns 403.
Requests propagate cancellation. Local/Render schemas are applied and matched; no authenticated HTTP request was run. See
[scenario contract report](../testing/student/education-identifier-masters/2026-10-28.md).

## Issued education identifier values — implemented in source, not deployed

These endpoints store values already issued by an external authority. They do
not create APAAR, UPN or other official identifiers and do not call a government
verification service. Staff must use the identifier type configured for the
Student's country. Values are encrypted with the existing active institution
encryption key and `IEncryptionService`; neither plaintext nor ciphertext is
returned. Responses contain `maskedValue` only. Values of four characters or
fewer are fully masked. A value update resets the review state to `Unverified`
and appends a history event when a prior review existed. Verified/Rejected
decisions append actor, time, prior/new status and optional reason; rejection
requires a reason. One record per Student and identifier type is allowed.

All routes require the authenticated Institution Staff bearer token. The
permission behavior resolves canonical `STUDENT_ADMISSIONS` View/Add/Update
permissions through the existing menu/permission pipeline. Resolve current
`moduleId` and `operationId` dynamically; never hardcode permission IDs. The
server derives institution and staff actor from trusted auth context.

| Method | Route | Permission | Purpose |
|---|---|---|---|
| GET | `/api/Student/{studentId}/education-identifiers?pageNumber=1&pageSize=10&moduleId={resolved}&operationId={resolved}` | View | Paged masked values for one Student; default page 1, 10. `{studentId}` is `long`. |
| POST | `/api/Student/{studentId}/education-identifiers` | Add | Record externally issued identifier. Route Student ID overrides any body Student ID. |
| PUT | `/api/Student/education-identifiers/{id}` | Update | Replace value/issuer metadata, reset verification, preserve audit. `{id}` is `long`. |
| POST | `/api/Student/education-identifiers/{id}/verification` | Update | Mark Verified or Rejected and append review history. |

Create body (replace permission IDs with values resolved from the current menu
flow):

```json
{
  "moduleId": 0,
  "operationId": 0,
  "identifierMasterId": 12,
  "value": "AUTHORITY-ISSUED-VALUE",
  "issuerReference": "school-record-ref-42",
  "issuedOn": "2026-08-15"
}
```

Update body uses the same fields except `studentId` and `identifierMasterId`:

```json
{
  "moduleId": 0,
  "operationId": 0,
  "value": "UPDATED-AUTHORITY-VALUE",
  "issuerReference": "school-record-ref-43",
  "issuedOn": "2026-08-15"
}
```

Review body uses numeric values from `GET /api/Student/options`;
`Unverified=1`, `Verified=2`, `Rejected=3`. Rejection requires a reason.

```json
{
  "moduleId": 0,
  "operationId": 0,
  "verificationStatus": 3,
  "reason": "Document value did not match the authority record."
}
```

Representative success data (the common API envelope wraps this object):

```json
{
  "id": 43,
  "studentId": 1204,
  "identifierMasterId": 12,
  "countryId": 105,
  "countryName": "India",
  "identifierCode": "NATIONAL_EDU_ID",
  "identifierName": "National education identifier",
  "issuingAuthority": "Configured national education authority",
  "maskedValue": "****************ALUE",
  "verificationStatus": 1,
  "issuerReference": "school-record-ref-42",
  "issuedOn": "2026-08-15",
  "addedById": 8,
  "addedDateTime": "2026-10-28T08:00:00",
  "verifiedById": null,
  "verifiedDateTime": null,
  "updatedById": null,
  "updatedDateTime": null
}
```

Validation returns the standard API error envelope for required/length/status
errors; typical outcomes are 400 validation, 401 unauthenticated, 403 denied
permission, 404 missing or cross-institution Student/type/value, and 409 a
duplicate value record or invalid review transition. Exact runtime envelopes
are pending authenticated HTTP acceptance. Request cancellation is passed
through MediatR and EF. Persistence is `StudentEducationIdentifier` with
encrypted and masked values plus `StudentEducationIdentifierVerificationHistory`
for review/reset events, both defined in
`database-scripts/CreateStudentAdmissionsAndRecords.sql`; that script is
applied to Local and Render. Source contract tests passed 5/5 after the audit and
masking checks; there is no DB, authenticated HTTP, or deployed verification.
See [identifier value and verification scenario report](../testing/student/education-identifiers/2026-10-28.md).

## Remaining API work and acceptance limits

All currently inventoried Student, Guardian, admission, fee, payment, identifier, portal-authentication, domain, currency and document routes have source implementations. The active route/input/response contracts remain in this handoff and the companion Word document. Source coverage is not deployed acceptance.

1. Local and Render backup recovery is proven by full restore to disposable scratch databases; both live schemas are applied and have matching verified fingerprints. See the current [schema parity report](../testing/student/schema-parity-and-runtime-smoke/2026-10-28.md).
2. Assign role grants only through the existing permission-management flow. SQL migrations intentionally do not grant roles; effective authorization remains to be exercised with an authorized bearer session.
4. Run authenticated Local HTTP/DB scenarios for CRUD, paging, tenant/location/session isolation, permission allow/deny, validation, approval/exit transaction rollback, duplicate/idempotency behavior and storage round trips.
5. Verify email invitation/reset delivery, live DNS TXT ownership, object-storage upload/download, Cashfree sandbox checkout/reconciliation/signed webhook/duplicate delivery, then deploy and record Render API acceptance separately.

Current source audit: 75 Student-filtered tests passed, 0 failed, 0 skipped; protected Staff/Curriculum gate 41 passed and 0 skipped with Local rollback probes enabled; Release solution build 0 errors and 22 warnings. Anonymous local HTTP smoke is documented separately. See [phasewise source audit](../testing/student/phasewise-source-audit/2026-10-28.md) and [continuation handoff](../STUDENT_CONTINUATION.md).

## Institution portal-domain ownership and DNS verification (source implemented; runtime acceptance pending)

The Institution Administrator registers their institution's portal hostname and proves DNS ownership. Portal authentication handlers resolve login, invitation and reset tenant context only from an active, DNS-verified hostname. The schema has been applied on Local and Render, but authenticated HTTP and live DNS acceptance remain pending; do not treat a saved domain as a live login URL. On 30 October, both environments had the invitation and reset templates and no active verified portal domain.

### Authentication and permissions

Use the existing Institution Staff bearer token and permission pipeline. Discover the current module and operation IDs from authenticated menu/permission data; do not hardcode numeric IDs. Module code: `STUDENT_PORTAL_DOMAINS`; operations are View, Add, Update and Delete. The authenticated actor and institution are read from server context; do not send InstitutionId or actor ID. Do not grant roles from SQL.

### Routes

| Method and route | Permission | Purpose |
|---|---|---|
| `GET /api/Student/portal-domains?pageNumber=1&pageSize=10&activeOnly=false&moduleId={resolved}&operationId={resolved}` | View | Tenant-scoped list; default page 1 / 10 |
| `POST /api/Student/portal-domains` | Add | Register hostname and receive one-time TXT challenge; permission IDs are JSON body fields |
| `POST /api/Student/portal-domains/{id}/challenge` | Update | Renew a pending challenge; permission IDs are JSON body fields |
| `POST /api/Student/portal-domains/{id}/verify` | Update | Resolve DNS TXT and mark verified/active; permission IDs are JSON body fields |
| `DELETE /api/Student/portal-domains/{id}?moduleId={resolved}&operationId={resolved}` | Delete | Deactivate and soft-delete mapping |

All `{id}` values are 64-bit `long`. `moduleId` and `operationId` must be current IDs resolved by the normal permission flow, not values copied from examples. POST bodies for create:

```json
{
  "domainName": "portal.example-school.org",
  "moduleId": 0,
  "operationId": 0
}
```

For challenge renewal and verification, include the respective resolved permission IDs in each JSON body:

```json
{
  "moduleId": 0,
  "operationId": 0
}
```

GET includes permission IDs in its query DTO. DELETE binds its permission DTO from query parameters. Replace all `0` values with IDs resolved by the existing authenticated permission flow.

The hostname must be a bare public DNS hostname. Scheme, path, port, wildcard, IP literal and localhost are rejected. IDN hostnames normalize to lowercase ASCII.

### Challenge workflow

1. Call POST create. Keep the returned TXT value only in the current UI state long enough to show/copy it once. It is a bearer-like ownership proof: do not put it in logs, analytics, local storage or error telemetry.
2. Ask the institution administrator to create the TXT record name/value returned by the API. The record name is `_skolenza-verification.<normalized-domain>`.
3. After DNS publication, call the verify route. DNS propagation can take time; a mismatch returns a conflict and can be retried after propagation. DNS resolver/network failure returns 503 and can be retried. The challenge expires after 24 hours; renew it to receive a replacement.
4. On success, reload the list. The API exposes only `Pending=1` or `Verified=2` and never returns the stored challenge hash/value in a list response. A verified domain cannot be challenged again. To replace it, soft-delete the current mapping, register the new hostname and verify it.

Create/renew response shape:

```json
{
  "isSucceeded": true,
  "message": "Portal domain added. Publish the returned TXT record, then request verification.",
  "data": {
    "domain": {
      "id": 123,
      "domainName": "portal.example-school.org",
      "verificationStatus": 1,
      "challengeExpiresAt": "2026-10-29T00:00:00Z",
      "verifiedAt": null,
      "isActive": false
    },
    "dnsTxtRecordName": "_skolenza-verification.portal.example-school.org",
    "dnsTxtRecordValue": "skolenza-portal-domain-verification=<one-time-random-value>"
  }
}
```

The challenge response above is illustrative; never reuse its example ID/value. List returns the standard paginated envelope with `data` array and page metadata. The resolver endpoint is configured server-side at `PortalDomainVerification:DnsOverHttpsEndpoint` (default `https://cloudflare-dns.com/dns-query`); this is not a UI input.

### Errors, persistence, and tested state

- 400: malformed hostname / validation error.
- 401/403: missing or insufficient authenticated permission.
- 404: domain ID is not owned by the authenticated institution or is deleted.
- 409: domain already claimed, another entry already exists for this institution, challenge expired/mismatch, challenge changed concurrently, or already verified.
- 503: configured DNS-over-HTTPS resolver is unavailable; retry with backoff and let the user retry after DNS propagation.
- Cancellation token is passed through DB and DNS HTTP requests. Do not automatically create repeated challenge records when verification fails.

Persistence: additive `skolenza.InstitutionPortalDomainMaster`; unique live institution and case-insensitive hostname indexes; foreign key to Institution; status/check constraints; challenge SHA-256 hash, 24-hour expiry, audit fields and soft-delete. The companion script `database-scripts/CreateInstitutionPortalDomainMaster.sql` also adds the dynamic `STUDENT_PORTAL_DOMAINS` module and View/Add/Update/Delete operation mappings; it grants no roles. Do not run the script until the authorized backup/apply workflow is followed.

Focused contract tests on 2026-10-28: **6 passed, 0 failed, 0 skipped**. Combined Student + protected Staff/Curriculum gate: **106 passed, 0 failed, 0 skipped / 106 total**. Full solution Release build succeeded with 0 errors and 22 existing warnings. The tests cover authorization metadata, host normalization, challenge entropy/hash/TXT parsing, mocked DNS-over-HTTPS TXT parsing/provider outage, EF mapping/indexes and dynamic permission resolution; the combined gate also ran the configured local DB rollback probe. No portal-domain schema application/CRUD, live DNS propagation, role-grant, or Render deployment/acceptance was performed. Angular is untouched. See [portal-domain scenario report](../testing/student/portal-domain-master/2026-10-28.md) and [continuation handoff](../STUDENT_CONTINUATION.md).

## Student/Guardian portal authentication (source WIP; UI contract preview)

Staff authentication is unchanged. Portal accounts and refresh tokens use separate `StudentPortalAccount`, `StudentPortalAccountGuardian`, `StudentPortalAccountStudent`, `StudentPortalRefreshToken` and `StudentPortalOneTimeToken` tables. `InstitutionPortalDomainMaster` must be active and DNS-verified; the request `Host` selects the institution. Email is normalized to uppercase for institution-scoped lookup. Login responses intentionally use the same Unauthorized result for unknown email, bad password, inactive account, unverified email or no active persona. The configured lockout counts failed attempts even when an account has never been locked; the current configured limit is five failures for fifteen minutes. A disposable PostgreSQL test verified persistence, while real-host HTTP and deployed acceptance are still pending. [Scenario](../testing/student/portal-login-lockout/2026-10-30.md).

An isolated Local HTTP harness verified synthetic Guardian login, authenticated profile, refresh rotation/replay rejection, logout/revocation, anonymous-profile denial and unverified-host denial using the real controller, handlers, JWT signer and bearer policy. The harness used a synthetic domain and does not establish live school DNS, email or deployed acceptance. [HTTP test evidence](../testing/student/portal-auth-http/2026-10-30.md). On 30 October, the invitation and password-reset templates were seeded on Render after verified backup and disposable restore; both templates match Local. No live verified domain or delivery was confirmed. [Database evidence](../testing/student/render-template-schema-sync/2026-10-30.md).

### Current source routes

| Method and route | Authentication | Request | State |
| --- | --- | --- | --- |
| `POST /api/Student/portal-auth/login` | Anonymous; verified institution hostname required | `{"email":"guardian@example.org","password":"<password>"}` | Implemented; no HTTP acceptance |
| `POST /api/Student/portal-auth/refresh` | Anonymous; verified institution hostname must match token tenant | `{"refreshToken":"<opaque-refresh-token>"}` | Implemented; no HTTP acceptance |
| `POST /api/Student/portal-auth/logout` | StudentPortal access token policy | same refresh-token request | Implemented; no HTTP acceptance |
| `GET /api/Student/portal-auth/me` | StudentPortal access token policy | no body | Implemented; no HTTP acceptance |
| `POST /api/Student/portal-auth/invitations/accept` | Anonymous; verified institution hostname required | `{"token":"<invitation-token>","password":"<12-to-128-character-password>"}` | Implemented; no HTTP/email acceptance |
| `POST /api/Student/portal-auth/invitations/claim` | StudentPortal access token policy; verified hostname must match account tenant | `{"token":"<invitation-token>"}` | Implemented; no HTTP/email acceptance |
| `POST /api/Student/portal-auth/password-reset/request` | Anonymous; verified institution hostname required | `{"email":"guardian@example.org"}` | Implemented; generic response; no HTTP/email acceptance |
| `POST /api/Student/portal-auth/password-reset/complete` | Anonymous; verified institution hostname required | `{"token":"<reset-token>","newPassword":"<12-to-128-character-password>"}` | Implemented; no HTTP/database acceptance |

Login returns `{ userType, institutionId, portalAccountId, email, accessToken, refreshToken, accessTokenExpiresAtUtc, refreshTokenExpiresAtUtc }`. Access JWT uses the existing signing service and issuer/audience, but claims include `UserType=StudentPortal`, `StudentPortalAccountId`, `InstitutionId`, email, and `TokenPurpose=Access`. Portal-only authorization requires these portal claims. Refresh tokens are random opaque values; only SHA-256 hashes are persisted and each refresh rotates/revokes the prior value. Account failures are locked after five failures for 15 minutes. Access-token and refresh-token values must be held as secrets and never logged.

`GET /me` returns only active persona links scoped to the token's institution and portal account. Guardian personas include their active linked children (Student ID, internal StudentCode and display name) only where `StudentGuardian.HasPortalAccess` and effective dates allow. Optional Student persona returns that Student's own internal code and display name. Example:

```json
{
  "isSucceeded": true,
  "data": {
    "portalAccountId": 42,
    "institutionId": 8,
    "email": "GUARDIAN@EXAMPLE.ORG",
    "personas": [
      {
        "type": 1,
        "id": 72,
        "displayName": "Parent Name",
        "studentCode": null,
        "linkedStudents": [
          { "studentId": 105, "studentCode": "STD-APP-2026-001", "displayName": "Learner Name" }
        ]
      }
    ]
  }
}
```

Guardian is the default persona. A Guardian gains access only through explicit account-to-Guardian mapping plus active StudentGuardian relationships with `HasPortalAccess`; this supports siblings without linking accounts by email alone. Student persona is optional and requires a separate explicit Student link.

### Invitation and password-reset behavior

Admission approval automatically queues an invitation for the single active primary Guardian as part of the existing approval transaction. The Guardian link must already be effective and not expired on the current UTC date. The verified portal domain must exist, and the global EmailTemplate catalogue must have an active `STUDENT_PORTAL_INVITATION` template before admission can be approved. Available placeholders include `GuardianName`, `GuardianEmail`, `InvitationUrl`, `PortalDomain`, `ExpiresAtUtc`. The invite URL is `https://{verified-domain}/auth/portal/accept-invitation?token=...`; the UI must read the token and POST it to `invitations/accept`. The token table stores its SHA-256 hash. New Student portal email placeholders are AES-GCM encrypted while stored in the queue; older plaintext queue records remain compatible and require separate review. The active institution key must remain available until queued messages are delivered. The token expires in 30 minutes and is single-use. If the normalized email already has a portal account, that user signs in on the same institution domain and calls `invitations/claim`; the account must match the invited Guardian email. No automatic cross-institution linking occurs. Local disposable-database tests verify encrypted queue persistence using a synthetic domain; actual email and authenticated HTTP remain pending. See [email protection evidence](../testing/student/portal-email-encryption/2026-10-29.md).

Password reset request always returns the same generic response, whether the email/account/persona exists or not. Email is queued only when the account is active, verified, has an active persona and the hostname is a verified institution domain. Configure an active global `STUDENT_PORTAL_PASSWORD_RESET` template with placeholders `Email`, `PasswordResetUrl`, `PortalDomain`, `ExpiresAtUtc`. The reset link is `https://{verified-domain}/auth/portal/reset-password?token=...`; POST the token and new password to `password-reset/complete`. The token table stores the hash; new queue placeholders are encrypted at rest. Tokens are single-use and expire in 30 minutes. Completion atomically changes the password hash/security stamp, clears lockout, revokes all active refresh tokens and invalidates other outstanding password-reset links for that institution/account. An invalidated link must not be retried; request a new reset. If simultaneous requests conflict, the API returns HTTP 409 with the standard error envelope and instructs the user to request a new link. Passwords must be 12–128 characters. A missing reset-email template silently suppresses delivery while retaining the generic response; monitor/configure email templates operationally. Local and Render invitation and reset templates are active; actual email delivery remains unverified. A forced PostgreSQL lock race passed through the real error middleware in process; full running HTTP and deployment remain pending. [Scenario](../testing/student/password-reset-token-revocation/2026-10-30.md).

Key rotation is not yet supported for queued portal emails: the shared key table currently enforces one key row per institution, and changing that key can make an encrypted pending email unreadable. The same key is used by other existing features; the [rotation investigation](../testing/student/portal-email-key-rotation/2026-10-29.md) documents the failed Local test and the pending shared-key design.

Local readiness for the `RRSMS` institution: a read-only check found no active granted role-permission rows for the Student Admissions, Student Fees, or Student Portal Domains modules. Use the existing role-management flow to assign and verify the intended operations. No permission IDs are hardcoded, no grants were modified by this check, and authenticated acceptance remains pending. See [role-grant readiness](../testing/student/access-readiness/2026-10-29.md).

Example reset request:

```json
{"email":"guardian@example.org"}
```

Example completion request:

```json
{"token":"<one-time-token-from-email>","newPassword":"<new-password>"}
```

Expected generic request response data:

```json
{"message":"If an active portal account exists for this email, password reset instructions will be sent."}
```

Invalid, expired, consumed or wrong-institution tokens are unauthorized. The application API does not reveal whether a reset email was actually queued to the requester. Email template readiness and delivery remain runtime acceptance prerequisites.

## Guardian/Student self-service documents (API source implemented; runtime acceptance pending)

The separate portal token policy protects the self-service routes. UI must first load `/api/Student/portal-auth/me`, select an authorized linked Student, and call the requirements endpoint separately for `Student=1` and/or `Guardian=2`. The route derives `InstitutionId` and `StudentPortalAccountId` from signed claims; never send either ID from the browser. Guardian scope requires the account's explicit Guardian link to that Student, active effective dates, `HasPortalAccess`, and an active Guardian profile. Student scope requires the Student persona or an active linked Guardian. Student records must be active and have a current active enrollment; requirements are filtered by that enrollment's campus/session.

| Method | Route | Auth | Behavior |
| --- | --- | --- | --- |
| `GET` | `/api/Student/portal/students/{studentId}/documents/requirements?personScope=1` | StudentPortal bearer | Lists active Student or StudentAndGuardian requirements for the current campus/session. Use `personScope=2` for Guardian requirements. |
| `POST` | `/api/Student/portal/students/{studentId}/documents` | StudentPortal bearer | Multipart upload of a Student or linked Guardian document; server enforces requirement scope, declared MIME, configured/absolute size, expiry and link access. |
| `GET` | `/api/Student/portal/students/{studentId}/documents?pageNumber=1&pageSize=10&personScope=1&status=1` | StudentPortal bearer | Paged document versions visible to the account; defaults to page 1, size 10. `personScope` and `status` are optional filters. |
| `GET` | `/api/Student/portal/students/{studentId}/documents/{documentId}/download` | StudentPortal bearer | Returns a five-minute signed URL after checking that the caller can see that exact document. |
| `POST` | `/api/Student/records/{studentId}/documents/{documentId}/review` | Institution Staff bearer + `STUDENT_ADMISSIONS` Update | Staff approves, rejects or returns a post-enrollment upload. Resolve ModuleId/OperationId dynamically from authenticated menu/permissions. |

Upload `multipart/form-data` fields:

- `requirementId` (required positive `long`) selects a current active master requirement.
- `personScope` (required enum `1=Student`, `2=Guardian`). Do not submit `3=StudentAndGuardian`; that value is a requirement applicability scope only.
- `guardianId` (required only for Guardian scope) must be the signed-in account's Guardian profile with an active relationship granting portal access to this Student.
- `expiryDate` is required only when the requirement says so and cannot be in the past.
- `file` (required) must match requirement MIME and size limits; absolute limit is 100 MB.

Example values (send as multipart fields; replace IDs with values returned by this institution's APIs):

```text
requirementId: 31
personScope: 1
expiryDate: 2027-06-30
file: <binary>
```

Guardian-document example adds `personScope: 2` and `guardianId: 72`. A newly submitted file gets a generated private storage object name, SHA-256 checksum, version number, `AwaitingReview=1` status and initial portal-actor history row. The response excludes the object key and binary contents. The portal can list/download visible versions but cannot approve or delete them. A new version is accepted only after the preceding version was `Rejected=3` or `ReturnedForCorrection=4`; pending/approved versions cannot be overwritten. Staff rejection/return requires a reason up to 1000 characters; expired evidence cannot be approved. Review decisions append staff-actor history and only pending records are reviewable. Required document review statuses are supplied by `GET /api/Student/options` (`AwaitingReview=1`, `Approved=2`, `Rejected=3`, `ReturnedForCorrection=4`).

Successful upload returns the standard `ApiResponse<StudentRecordDocumentResponseDTO>` with IDs, requirement code/name, scope, optional GuardianId, version/supersedes, original filename, MIME, byte size, checksum, expiry, status and submit/review timestamps/reason. Example shape:

```json
{
  "isSucceeded": true,
  "message": "Student document uploaded and queued for review.",
  "data": {
    "id": 501,
    "requirementId": 31,
    "requirementCode": "BIRTH_CERTIFICATE",
    "requirementName": "Birth Certificate",
    "studentId": 105,
    "personScope": 1,
    "guardianId": null,
    "versionNumber": 1,
    "supersedesDocumentId": null,
    "originalFileName": "birth-certificate.pdf",
    "contentType": "application/pdf",
    "fileSizeBytes": 248310,
    "checksumSha256": "<lowercase-sha256>",
    "expiryDate": null,
    "status": 1,
    "submittedDateTime": "<UTC timestamp>",
    "reviewedDateTime": null,
    "reviewReason": null
  }
}
```

Expected errors: 400 for invalid scope, missing/expired expiry date, empty/oversized file, MIME/size mismatch, or invalid review transition/reason; 401 for missing/invalid portal token; 403 for wrong token policy; 404 for Student, requirement or document outside authorized institution/link scope; 409 for absent active enrollment or when the current version cannot be replaced. Cancellation is passed to repository/database operations; the current storage service upload/delete methods do not accept cancellation tokens. On DB save failure, the uploaded object is deleted as compensation; monitor storage failures because a failed compensating delete may require orphan cleanup.

Persistence uses `StudentRecordDocument` and append-only `StudentRecordDocumentReviewHistory`; requirement values come from the existing location/session-scoped `StudentDocumentRequirementMaster`, and files go through existing `IFileStorageService` under the configured `AWSUploads:BasePath`. Additive DDL is `database-scripts/CreateStudentPortalDocuments.sql`, applied only after `CreateStudentAdmissionsAndRecords.sql` and `CreateStudentPortalAuthentication.sql`. Local/Render schemas are applied and matched;  no HTTP, S3, staff review, login/email, or deployment acceptance is claimed. Contract and source verification is recorded in the [portal document scenario report](../testing/student/portal-document-upload/2026-10-28.md).

**Still pending before production UI acceptance:** authenticated Local workflows and effective role assignment via existing permission flow; PostgreSQL transaction acceptance; S3/email/DNS/Cashfree round trips; API deployment and authenticated Render acceptance. The portal auth/document APIs are implemented in source, but deployed behavior is not yet accepted.

Latest combined Student source checks (2026-10-28): portal document focused tests **4/4**; admission detail/history tests **5/5**; all Student-filtered tests **75/75**; protected Staff/Curriculum gate **41 passed, 0 failed, 0 skipped / 41 total** (Local rollback probes enabled); Release solution build **0 errors, 22 warnings**. No authenticated HTTP, object-storage round trip, invitation/reset email or deployment is claimed. Local/Render schemas are applied and matched. See [portal document scenario report](../testing/student/portal-document-upload/2026-10-28.md), [admission detail/history scenario report](../testing/student/admission-detail-status-history/2026-10-28.md), [portal-auth scenario report](../testing/student/portal-authentication/2026-10-28.md) and [continuation handoff](../STUDENT_CONTINUATION.md).


## Current acceptance status (2026-10-28)

The database changes in the ten Student SQL scripts were dry-run against a restored scratch copy and applied to both Local and Render. Both targets contain 38/38 Student tables and have the same column/constraint/index fingerprint. The four associated dynamic modules expose active View/Add/Update/Delete operation mappings, without role grants. Full backup restoration, Student tests (75/75), protected gate (41 pass, 0 skips) and Release build (0 errors) are evidenced in the current [scenario report](../testing/student/schema-parity-and-runtime-smoke/2026-10-28.md). Anonymous Local HTTP was smoke-tested only; no authenticated workflow or provider round trip is claimed. The API code is not deployed.

Before UI/client acceptance, run the authenticated Local scenarios listed in the [continuation plan](../STUDENT_CONTINUATION.md), configure effective role grants with the existing permission flow, then publish and test the deployed API. The database row contents are environment-specific and were not synchronized.
# Local fee payment correction 2026-10-30

The offline payment and Cashfree checkout preparation paths now execute their PostgreSQL idempotency locks with the required scalar alias. Payment timestamps, checkout expiry and receipt issue times use UTC in their existing columns; the offline payment retry compares timestamps at PostgreSQL microsecond precision. Under a serialization race the repository retries once, then returns a conflict instructing the client to retry with the same idempotency key. Clients must retain the same key and identical payment details for a retry; a different timestamp or payment detail is a conflict. A guarded Local PostgreSQL scenario verified partial and final payment, replay, overpayment rejection, receipt times, checkout expiry, forced concurrent offline requests, and simulated failure and concurrent duplicate success webhook events. This is not an authenticated HTTP or Cashfree sandbox acceptance. See [fee-payment scenario](../testing/student/fee-payment-database/2026-10-30.md).

Student exit and Guardian-link changes now serialize on the Student row. A competing second exit or a second primary Guardian receives the existing conflict response; a Guardian link cannot be created after an exit completes. Education identifier creation, verification and updates use UTC values in the existing timestamp columns. A guarded Local DB verified these paths, masked/encrypted test identifiers and retained previous/destination school history. Authenticated HTTP and deployed acceptance remain pending. See [exit, Guardian and identifier scenario](../testing/student/exit-guardian-identifier/2026-10-30.md).
