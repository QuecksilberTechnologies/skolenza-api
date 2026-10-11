# Employee profile tabs — CRUD, validation and percentage contract

## Scope

This handoff covers Overview, Bank, Contact, Experience, Insurance, Identity,
Education and Dependent. Leave is excluded by the user request. Files currently
renders demonstration data and its upload method is marked TODO in Angular; it
does not yet have an authorized persistence/category contract.

All employee IDs remain tenant-salted encoded IDs. Every request must use the
existing authenticated menu/permission pipeline and resolve current ModuleId and
OperationId values dynamically.

## Published CRUD surface

| Tab | Create | Read | Update | Delete |
| --- | --- | --- | --- | --- |
| Overview | Employee create flow | Employee get | Employee update | Employee delete/status flow |
| Bank | `POST /api/Employee/Bank/create` | `GET /api/Employee/Bank/get` | `POST /api/Employee/Bank/update` | `DELETE /api/Employee/Bank/delete` |
| Contact | `POST /api/Employee/Contact/create` | `GET /api/Employee/Contact/get` | `POST /api/Employee/Contact/update` | `DELETE /api/Employee/Contact/delete` |
| Experience | `POST /api/Employee/Experience/create` | `GET /api/Employee/Experience/get` | `POST /api/Employee/Experience/update` | `DELETE /api/Employee/Experience/delete`; document delete uses `delete-doc` |
| Insurance | `POST /api/Employee/Insurance/employee-insurance-enroll` (create/update same employee-policy pair) | `GET /api/Employee/Insurance/get-all-enroll` | same enrollment route | `DELETE /api/Employee/Insurance/delete` |
| Identity | `POST /api/Employee/Sensitive/Create` (create/update same employee-document pair) | `GET /api/Employee/Sensitive/get` | same upsert route | `DELETE /api/Employee/Sensitive/delete` |
| Education | `POST /api/Employee/Education/create` | `GET /api/Employee/Education/get` | `POST /api/Employee/Education/update-education` | `DELETE /api/Employee/Education/delete` |
| Dependent | `POST /api/Employee/Dependent/create` | `GET /api/Employee/Dependent/get` and `get-in-detail` | `POST /api/Employee/Dependent/update` | `DELETE /api/Employee/Dependent/delete` |

## Central percentage contract

`EmployeeProfileCompletionCalculator` owns row and section percentage rules.
Legacy helper entry points delegate to it. Verification and edit workflow flags
never add percentage. Section percentage is the rounded mean of its row values;
empty sections are zero. Bank is zero until a Primary account exists. Once the
Primary requirement is satisfied, all saved Bank rows contribute through their
average; the Primary row also requires the cancelled-cheque upload already
enforced by Bank create/update validation. Contact retains its existing primary
rule.

Identity completion is country-driven through active `CountryIdentityRule`
records. A mandatory country rule participates even when the employee has not
created a row. An optional rule participates only after a saved identity exists.
Every participating identity row requires both its identity value and uploaded
document, with equal weight. No country or identity-document name is hardcoded
in the calculator.

`GET /api/Employee/get-all`, `GET /api/Employee/get-all-percentage`, individual
Bank reads and Identity reads use the same backend-owned calculation. The
existing response properties are unchanged, so the UI only renders the corrected
percentages and does not need a contract change.

The profile summary and individual tab APIs now use the same central rules for
Overview, Bank, Contact, Experience, Insurance, Identity, Education and
Dependent. Files has no percentage until its persistence flow is approved.

## Shared validation

Education validates required degree/institute/score type, date order and
conditional gap details. Experience validates company/designation/start date,
date order and conditional gap details. Dependent validates name, relation and
non-future date of birth. Insurance validates target employee access, policy
IDs, date order and dependent selection. Identity validates country eligibility,
value length, duplicate request documents and effective-date order.

Bank create validates the branch name, 9–18 digit account number, Indian IFSC
shape, optional UPI ID, and the cancelled-cheque requirement for a primary
account on the API as well as in Angular. Adding a non-primary account does not
clear the employee's existing primary account.

Bank `AccountNumber`, `IFSCCode` and optional `UPIId` are encrypted at rest with
the existing tenant encryption key and `IEncryptionService`. Create and Update
encrypt before persistence. Get decrypts only inside the authenticated tenant
request and returns the original values over HTTPS so the existing UI mask/reveal
and Edit flows continue to work. Delete and verification/edit-status operations
do not transform these values. The database column capacities are 128, 128 and
512 characters respectively to hold AES ciphertext. A hash is not used because
these three values must remain reversible for the authorized Edit/reveal flow.

Contact relation options come from the bearer-authenticated, permission-ID-free
`GET /api/Employee/Contact/relation-options` endpoint. Employee creation also
creates one editable contact placeholder in the same transaction with the full
employee name and selected `CountryId`; relation and remaining contact/address
fields are null. Manual Contact Add/Edit follows Country -> State -> District ->
Locality and persists nullable `LocalityId`. See
[Employee Contact Relation and Location](EMPLOYEE_CONTACT_RELATION_LOCATION.md)
for the complete API, payload, migration and UI contract.

## Error response and display contract

The Angular `BaseService` is the shared error boundary for these tab services.
It displays the API `Message`/`message`, appends validation entries from
`Errors`/`errors`, and retains `ErrorCode`/`errorCode`. JSON returned as text is
parsed before displaying it. When the response body has no usable message, the
UI supplies an actionable status-specific explanation for 400, 401, 403, 404,
409, network failures and other HTTP failures.

Expected application failures must use the standard API envelope. Example:

```json
{
  "IsSucceeded": false,
  "Message": "The submitted bank information is invalid.",
  "Data": null,
  "Errors": [
    "Account number must contain 9 to 18 digits."
  ],
  "ErrorCode": "VALIDATION_ERROR"
}
```

Unexpected server failures use a safe public message and include a request ID
in `Errors` so support can find the corresponding server log. Internal exception
and database details are never returned to the browser.

## Persistence tables

The profile CRUD path writes these 11 primary transaction tables:

1. `Employee`
2. `EmployeeImage`
3. `EmployeeBankDetail`
4. `EmployeeContact`
5. `EmployeeExperience`
6. `EmployeeExperienceDocument`
7. `EmployeePolicyEnrollment`
8. `EmployeePolicyDependentMapping`
9. `EmployeeIdentity`
10. `EmployeeEducation`
11. `EmployeeDependent`

Country, state, district, locality, identity catalogue, policy, bank account type and other
lookup masters are read-only inputs and require seed/master data. Transaction
tables must be populated through application workflows, not fabricated seed
employees. The exact master-table seed list is module-specific; identity uses
the four seeded masters documented in `EMPLOYEE_COUNTRY_IDENTITY.md`.

### Education create FormData contract

`POST /api/Employee/Education/create` consumes `multipart/form-data`. `EmployeeId`
is the tenant-salted encoded employee ID. `ModuleId` and `OperationId` must be
resolved through the authenticated menu/permission flow; numeric values must not
be hardcoded. Dates use ISO form values and are persisted as date-only values.
`ScoreType` is the existing numeric-string contract (`1` for Percentage and `2`
for CPG); invalid or non-positive values return validation failure instead of a
generic server error.

```text
EmployeeId=<encoded-employee-id>
Degree=Bachelor of Technology in Computer Science
InstituteName=Example Institute
StartDate=2016-07-01
EndDate=2020-06-30
ScoreType=1
ScoreValue=80
GradeDivision=First
IsEducationGapBeforeDegree=false
GapYears=0
Remark=test
ModuleId=<dynamic-module-id>
OperationId=<dynamic-operation-id>
```

`EducationDocument` is optional. When supplied, it uses the same FormData request.
No retry or polling contract applies to this synchronous create endpoint.

## Tested and deployed status

Local backend build and focused tests plus Angular employee-profile tests are
recorded in `docs/testing/employee/profile-crud/2026-09-30.md`. Authenticated
HTTP CRUD against the deployed Render API and direct persistence reconciliation
have not been executed in this change and must not be treated as passed.
