## Current implementation and environment status — 2026-10-28

Student API work is API-only and remains uncommitted/not deployed. Local and Render each have all 38 expected Student tables; columns, constraints and indexes match fingerprint `d11f7c18a9ca97080d89fcd57cedbcbe`. The ten additive scripts were dry-run against a restored scratch backup before applying to both targets. Both backups were full-restored to disposable scratch databases (216 `skolenza` tables each), and the scratch databases were removed. Four related dynamic modules have active View/Add/Update/Delete mappings in each database; role grants are not seeded and must follow the existing permission pipeline. Student tests pass 75/75; protected regression gate passes 41/41 including Local PostgreSQL rollback probes; Release solution build has 0 errors and 22 warnings. Anonymous local smoke returned Swagger 200 and protected options 401. Authenticated workflows, external provider round trips and deployment acceptance remain open. Angular is untouched. See [Student continuation](../STUDENT_CONTINUATION.md), [API handoff](../UIdeveloperDoc/STUDENT_ADMISSION_API_HANDOFF.md), and [schema/runtime report](../testing/student/schema-parity-and-runtime-smoke/2026-10-28.md).

# Student Teacher Guardian and Login Structure

> **Continuation entry point:** For the current task order, approved decisions,
> exact implementation status and resume instructions, read
> [Student API continuation handoff](../STUDENT_CONTINUATION.md) first.

Status: **WIP — API-ONLY; admission lifecycle, Student/Guardian/exit APIs, staff-assisted and Guardian/Student portal document upload/review, fee configuration/quote/offline ledger, school-owned Cashfree credentials, Cashfree checkout/reconciliation/webhook APIs, official education-identifier value/verification APIs, source-level atomic admission-readiness validation and institution portal-domain DNS verification are implemented in the working tree. Separate portal login/refresh/logout/`me`, invitation and recovery APIs are implemented in source. Local and Render Student schemas are applied and structurally matched; authenticated runtime/provider acceptance remains unverified. Angular excluded.**
Initial decision date: **2026-10-17**
Expanded proposal date: **2026-10-27**
Implementation priority: **Authorized to start 2026-10-27**

## Purpose and current boundary

Skolenza must keep Students separate from the existing StaffMember HR aggregate. A Student is an academic learner, while a Teacher is a StaffMember whose professional Position and academic teaching assignments describe their work. Student, StaffMember and Guardian identities may receive portal access through a common authentication layer, but their business records and permissions must remain isolated.

This document records the authorized API-only scope and design. The working tree contains Student admissions, Guardian, staff-assisted and portal self-service documents, education-ID, fee, currency, Cashfree and portal-domain source APIs. Portal authentication source includes separate institution-scoped accounts/persona links, verified-host tenant lookup, login/refresh/logout/`/me`, invitation accept/claim, generic password reset, approval-triggered Guardian invite/email queue, StudentPortal-specific JWT claims, hash-only tokens and account lockout. Portal documents use the existing storage service and check portal account, Student, Guardian and enrollment scopes. Official identifier values use the existing institution-key encryption service and return masked values; authority issuance is outside this API. Cashfree credentials use AES-GCM from the approved institution key; 3-decimal checkout is rejected without rounding. Existing Student masters are on Local and Render, but dynamic operation mappings are applied and verified on both targets; role grants still use the existing permission UI. Registry, fee/currency, portal-domain, portal-auth and portal-document entities are mapped in EF/additive SQL but their schemas have been applied to Local and Render with matching catalog fingerprints. Admission approval readiness and Student creation/invitation enqueue share a source transaction; PostgreSQL rollback/runtime acceptance is unverified. No authenticated HTTP, email delivery, S3 round-trip, live DNS, Cashfree sandbox or deployed acceptance has occurred. Angular has not been changed.

### Current implementation update — 2026-10-28

Phases 1–2 now have source-level Guardian capture, staff-assisted admission-document APIs, and post-enrollment Guardian/Student self-service document APIs. Portal document endpoints require the separate StudentPortal token, derive institution/account identity from signed claims, scope requirements to the active enrollment campus/session, validate linked Guardian access, version uploads, retain review history and use short-lived signed downloads. Review states come from the central Student options API; staff review uses existing permission behavior. Admission approval checks required Student/Guardian uploads for approved status and validity before Student creation. Source contracts pass; registry, portal-auth and portal-document SQL is applied to Local and Render. Local/Render schema parity is verified; authenticated PostgreSQL and storage round-trip acceptance is not claimed. See the [document API handoff](../UIdeveloperDoc/STUDENT_ADMISSION_API_HANDOFF.md), [admission document report](../testing/student/admission-documents/2026-10-28.md) and [portal document report](../testing/student/portal-document-upload/2026-10-28.md).

Institution portal-domain source API was added on 2026-10-28 following the user's DNS TXT verification decision. Focused tests passed 5/5; combined Student + protected Staff/Curriculum tests passed 105/105 with the configured local DB rollback probe; Release solution build passed with zero errors. Its additive SQL is applied to Local and Render, and authenticated CRUD, real DNS propagation, role grant and Render acceptance remain pending. The verified-domain master is a prerequisite for the portal login resolver, not a login feature by itself. See the [portal-domain report](../testing/student/portal-domain-master/2026-10-28.md).

Earlier source verification on 2026-10-28: Cashfree provider + AES-GCM/configuration contracts passed 10/10. An earlier combined Student + protected gate passed 86, failed 0, skipped 9 of 95; the latest protected gate subsequently ran all 41 checks with Local DB rollback probes and passed 41/41. Release API/test build completed with 0 errors; existing package advisories and compiler warnings remain. Coverage includes sandbox order URL/session parsing, 3-decimal checkout refusal before provider I/O, webhook timestamp/raw-body HMAC, AES-GCM configuration contracts, fee/quote snapshots and admission prerequisites. No authenticated HTTP, S3, Cashfree sandbox or deployed acceptance was executed; schema parity is now verified. See the [Cashfree tuition scenario report](../testing/student/cashfree-tuition-payments/2026-10-28.md).

Official education-identifier value API verification on 2026-10-28: focused source-contract tests passed 5/5; combined Student + protected Staff/Curriculum gate passed 91, failed 0, skipped 9 of 100. Coverage confirms authenticated route metadata, central paging defaults, masked-only response contract, EF master/history mapping, permission-pipeline resolution and full masking for short identifiers. No DB application, authenticated HTTP, runtime encryption/key test or deployment was performed. See the [identifier scenario report](../testing/student/education-identifiers/2026-10-28.md).

### Decisions added 2026-10-27: student exit and education identifiers

- A Student deletion request is an **exit workflow**, not a physical delete. It must close the active enrollment, mark the Student exited/inactive and append a `StudentExitHistory` row in the same transaction. Preserve the Student, admissions, documents, identifiers, enrollments, and financial/audit history. Repeating an exit must not create duplicate history. The record must capture the last school/country attended and (when supplied) the destination school/country, plus effective exit date, exit reason and actor/audit timestamps. No hard-delete endpoint is planned.
- After a Student admission is approved, the system will automatically invite the linked Guardian to activate the institution-scoped portal account. Student portal persona remains optional.
- Portal login is planned as email/password under a verified institution portal domain. The user approved a separate `InstitutionPortalDomainMaster`, managed and DNS TXT-verified by the Institution Administrator. `InstitutionCode` and `CompanyEmailDomain` are not domain-ownership proof. The management API and additive SQL exist in the working tree; schema application and portal login tenant resolution are still pending.

### Institution portal-domain ownership

Each Institution Administrator registers the institution's hostname through the authenticated `STUDENT_PORTAL_DOMAINS` permission pipeline. The API normalizes IDN hostnames to lowercase ASCII and rejects URLs, ports, paths, IP literals, wildcards and localhost. It returns a one-time random TXT value at `_skolenza-verification.<domain>`; only SHA-256 of that value is stored, and the challenge expires after 24 hours. The administrator publishes the TXT record and calls verification. The API resolves DNS TXT through the configured DNS-over-HTTPS endpoint and activates the domain only if the current, unexpired challenge matches. A verified domain cannot be challenged again; the administrator must soft-delete it before replacing it. A tenant may not claim a domain already owned by another active record.

The API routes are `GET/POST /api/Student/portal-domains`, `POST /api/Student/portal-domains/{id:long}/challenge`, `POST /api/Student/portal-domains/{id:long}/verify`, and `DELETE /api/Student/portal-domains/{id:long}`. The list defaults to page 1/10. The default resolver endpoint is Cloudflare DNS JSON and can be configured via `PortalDomainVerification:DnsOverHttpsEndpoint`. The returned challenge must not be logged or cached. Portal login, invitation and reset handlers resolve only an active, verified hostname and reject unknown domains. `CreateInstitutionPortalDomainMaster.sql` is additive and applied to Local/Render; schema parity is recorded in the current scenario report.
- Every Student receives an institution-unique Skolenza `StudentCode`, generated by the API. Government or national education identifiers are separate, country/authority-qualified records; Skolenza must not manufacture an official value. Keep an authoritative identifier catalogue (`StudentEducationIdentifierMaster`) and Student-owned values (`StudentEducationIdentifier`) so one learner can have more than one external identifier and each value keeps its country, authority, status and verification provenance.
- APAAR is created through the UDISE system using the student's PEN and parental consent where required; England's UPN follows the Department for Education/local-authority generation process. The implementation will capture separately issued identifiers and their verification status; authority issuance requires the applicable official integration and cannot be represented by a locally invented format. Sources: [APAAR official portal](https://apaar.education.gov.in/), [DfE Unique Pupil Number guidance](https://www.gov.uk/government/publications/unique-pupil-numbers).

## Recommended relationship structure

```text
Institution
  -> AcademicSession
      -> Programme
          -> AcademicGroup or Grade
              -> ClassSection
              -> CourseOffering or Subject

StudentAdmission
  -> Student
      -> StudentEnrollment
          -> StudentSectionAssignment
          -> StudentCourseEnrollment

Guardian
  <-> StudentGuardian <-> Student

StaffMember
  -> StaffPositionAssignment -> StaffPositionMaster
  -> StaffTeachingAssignment -> ClassSection and CourseOffering

StaffLoginCredential -> StaffMember
PortalLoginCredential -> Student or Guardian
Guardian -> StudentGuardian -> one or more Students
```

## Student aggregate

### Student

`Student` is the permanent institution-owned learner identity. Academic year, grade and section must not be stored as replaceable current values on this table.

Recommended data includes InstitutionId, StudentCode, AdmissionNumber, name, date of birth, GenderId, CountryId, optional contact information, lifecycle status, active state and audit fields. StudentCode must be unique inside one Institution.

The same Student row continues across academic sessions. Promotion creates a new enrollment; it does not create a duplicate Student.

Student exit never deletes this aggregate. `StudentExitHistory` is an append-only exit event linked to the Student and the final enrollment; the transaction closes the active enrollment and changes the Student lifecycle state. Previous and destination school details belong to the admission/exit transitions, not to a mutable single "school" field on the permanent Student record.

### Country education identifiers

`StudentEducationIdentifierMaster` defines the country and issuing authority's identifier type and applicability using the existing Country catalogue. It must not hardcode country names or pretend to generate authority-owned identifiers. `StudentEducationIdentifier` stores an issued value separately from Skolenza's internal StudentCode, with source authority, issue/verification status, verified timestamp and audit actor. Identifier values must use the repository's approved sensitive-data handling before storing or returning them; API responses should mask values unless an authorized operation explicitly requests the full value. Historical institution/country transfers do not rewrite previously issued identifiers.

### StudentAdmission

`StudentAdmission` owns the application and approval lifecycle before a permanent Student is created. The approved status enum is Draft, Submitted, Under Review, Approved, Rejected, Returned for Correction and Cancelled (`StudentAdmissionStatus` in `StudentEnums.cs`).

Approval must create the Student, StudentEnrollment and approved Guardian mappings atomically. A rejected or cancelled application must not create a partial Student graph. Correction, rejection and cancellation must preserve reasons and audit history.

### StudentEnrollment

`StudentEnrollment` represents a Student's academic placement for a particular session. It links Student, AcademicSession, Programme, AcademicGroup or Grade and Campus, and stores enrollment date, roll number, effective dates, status and audit information.

Recommended statuses are Draft, Active, Completed, Withdrawn, Transferred and Cancelled. A completed 2028-29 enrollment remains historic; promotion creates a new 2029-30 enrollment for the same Student.

### StudentSectionAssignment

`StudentSectionAssignment` is effective-dated so a mid-session section transfer does not overwrite history. It links StudentEnrollment to ClassSection and includes roll number, EffectiveFrom, EffectiveTo, primary flag, transfer reason and audit fields.

### StudentCourseEnrollment

Mandatory subjects may be derived from the active Grade or Programme course offerings. `StudentCourseEnrollment` records elective, optional, added or dropped offerings when an Institution needs learner-level course selection. It links StudentEnrollment to CourseOffering and preserves status and effective dates.

## Guardian aggregate

`Guardian` is an independent person record with name, verified contact details, address, occupation, active state and audit fields.

`StudentGuardian` is a many-to-many relationship. It records relationship type, primary guardian, emergency contact, pickup authorization, academic and fee communication preferences, portal access and effective dates. One Guardian may access multiple linked Students through one account, subject to Institution isolation and the active relationship permissions.

## Teacher structure

A Teacher remains a `StaffMember`; no duplicate Teacher person table is required.

```text
StaffMember
  -> StaffCategory: Teaching
  -> StaffPositionAssignment: Mathematics Teacher
  -> UserAccessRole: Faculty
  -> StaffTeachingAssignment: Grade 5 Section A Mathematics
```

`StaffPositionAssignment` describes the professional job. `StaffTeachingAssignment` describes the session, class section and course offering that the Teacher actually teaches. One Teacher may have multiple effective teaching assignments.

Position and AccessRole must remain separate. Position describes the job; AccessRole controls application permissions. Capabilities and duties describe professional requirements and expected work, but they do not grant software access.

## Staff and Student/Guardian authentication boundary

The current `LoginCredential` is directly tied to a mandatory StaffMemberId and
the staff refresh-token flow references it. It cannot safely hold Student or
Guardian credentials by treating them as StaffMembers.

The recommended first portal design keeps staff authentication unchanged and
introduces a separate `PortalLoginCredential` record for Student/Guardian portal
accounts. The portal can have its own login page and audience-specific API
routes, while reusing the approved password hashing, reset, lockout, token,
logging and account-security services. Separate pages and endpoints do not
require separate security algorithms or a second permission system.

A Guardian account links to one Guardian record; `StudentGuardian` determines
which children that Guardian may access, so one parent account can serve several
children. A Student portal account is optional and links to that Student. Young
learners can use the Guardian portal without receiving their own credentials.
Staff, Student and Guardian remain separate business identities. Portal API
authorization must scope every request to the authenticated portal identity and
active StudentGuardian/Student relationship, using the established permission
pipeline and server-side data-scope checks.

The user confirmed a separate portal login per institution, separate credential
table and API, Guardian account as the default, and optional Student login.
Credential resolution and uniqueness must therefore be institution-scoped.
Whether one person may hold both Guardian and Student portal personas, account
invitation/recovery policies and refresh-token owner model still require a
contract decision before implementing auth. Do not copy the staff login
handler or weaken its refresh-token ownership rules.

## End to end operating flow

1. The Institution configures AcademicSession, Programme, AcademicGroup or Grade, ClassSection, Subject and CourseOffering.
2. The Institution configures Teaching StaffCategory, academic JobFamily, required capabilities, duties, Teacher Position and Faculty AccessRole.
3. A StaffMember is created, assigned the Teacher Position and Faculty role, then linked to classes and subjects through StaffTeachingAssignment.
4. Admission staff save a StudentAdmission as Draft, add Guardian relationships and documents, then submit it for review.
5. An authorized reviewer approves, rejects, cancels or returns the application with an auditable reason.
6. Approval creates Student, active StudentEnrollment, Guardian mappings and an initial StudentSectionAssignment in one transaction.
7. Mandatory subjects are derived from approved offerings; elective selections are stored through StudentCourseEnrollment where applicable.
8. Portal invitations are sent only to eligible Student or Guardian identities. Credential activation and academic enrollment remain separate states.
9. A Teacher sees only Students in their effective StaffTeachingAssignments and records lesson plans, delivery, attendance, homework and assessments against the correct class and course.
10. A Student sees only their own academic information. A Guardian sees only Students linked through active StudentGuardian permissions.
11. At session end, enrollment is completed or withdrawn. Promotion creates a new enrollment and section assignment while the permanent Student, Guardian links, account and historic results remain intact.

## Integrity and security rules

- Student must never reuse StaffMember or recruitment Candidate records.
- Every Student-owned primary key and Student-owned foreign key uses a 64-bit integer (`long` in .NET, `bigint` in PostgreSQL). References to existing shared catalogues retain the referenced catalogue's native type (for example CountryId and StateId remain `int` where their source tables use integer keys).
- Every relationship must validate that all referenced records belong to the same Institution.
- Section and course assignments must belong to the enrollment's active AcademicSession and academic structure.
- Effective date ranges must be valid and historical assignments must not be overwritten.
- Position, capabilities and duties must never grant application permissions implicitly.
- Student, Guardian and Teacher visibility must be enforced by the existing server permission pipeline and data-scope checks.
- Deactivating a login must not delete the Student, StaffMember, Guardian, enrollment or academic history.
- Withdrawing an enrollment must not delete the permanent Student identity.
- Login IDs must be normalized and unique according to the future approved multi-institution account rule.
- Passwords, tokens and sensitive identity data must continue to use the existing security and encryption controls.

## Deferred implementation boundary

The user authorized API-only implementation on 2026-10-27. Angular routes/stores and all other Angular work remain excluded. The active backend work is staged so each API contract, persistence script, tests and UI-developer handoff can be reviewed independently. The working-tree API includes location/session-scoped document requirements, Guardian capture, fee quote/schedule APIs, and source-level approval readiness checks. Full workflow remains WIP because authenticated PostgreSQL workflow verification, external provider round trips and deployment acceptance remain pending. Student API source and Local/Render schema parity are now verified.

---

## Admission, guardian documents, benefits and fee design (proposal)

This section expands the earlier Student proposal using the current Skolenza
academic structure and the screenshots supplied for the registration and
database discussion. Implementation authorization is API-only; Angular work and
deployment remain excluded.

### Decisions recommended

1. Keep the permanent learner, admission application and each academic-year
   enrollment as separate records. An application is not yet an enrolled
   Student.
2. Keep Teachers as `StaffMember` records. Link them to class/course delivery
   through staff teaching assignments; do not create duplicate Teacher people.
3. Keep Guardian and Student as separate people and connect them through
   `StudentGuardian`. A guardian account can serve more than one linked child.
4. Configure fees against the academic session, campus/location and academic
   group/programme. Do not put the annual fee amount on the reusable Group form;
   the session-specific structure allows next year's price to change without
   rewriting historical fees.
5. Configure required admission documents and benefit schemes in institution
   masters that can be scoped to a location and effective academic session.
   Registration should retrieve these options from the API, not duplicate lists
   in the UI.
6. Keep Staff login and its `LoginCredential` unchanged. For the separate
   Student/Guardian portal, use a dedicated `PortalLoginCredential` table and
   dedicated portal login API, while reusing approved lower-level password and
   token-security services. Link a Guardian account to the Guardian identity;
   use `StudentGuardian` to scope its children. Do not create one parent login
   per child or insert a Student into the staff credential table.

### User-approved design decision — 2026-10-27

The user confirmed that Student/Guardian portal authentication will have a
separate credential table and a separate API. The existing Staff login page,
API, `LoginCredential` table and staff refresh-token ownership remain outside
this change. Backend APIs, persistence/schema work needed by those APIs, tests,
and UI-developer handoff documentation are authorized. Angular work and
deployment are excluded. Portal recovery and token ownership must reuse or
extend verified existing security services before the corresponding auth API
is implemented.

The user also confirmed these Student/fees rules on 2026-10-27:

- Guardian portal account is the default; Student portal login is optional.
- The school configures allowed fee frequencies; the parent chooses among the
  active options shown for that fee structure.
- Every admission with a selected benefit scheme must enforce that scheme's
  configured minimum and maximum discount bounds. The percentage applies to
  gross recurring annual charges only. A no-scheme quote receives 0% discount.
  Currency uses the minor-unit precision from the active Campus-country
  `CountryCurrencyMaster` mapping (0, 2 or 3 decimal places), midpoint away
  from zero. Fee structures and immutable quotes snapshot this precision. Scheme
  exceptions must remain within the configured bounds; no override workflow
  has been approved.
- Fee structures are campus/location and academic-session specific, have no
  section-specific price, and require an Academic Group/grade. The linked
  Programme is derived from that Group.
- At most one benefit scheme may be selected for an admission. The approved
  discount applies only to recurring annual fee components; one-time admission
  and examination charges are outside the discount base.
- Discount minimum/maximum percentages are configured per benefit scheme. A
  quote without a selected scheme has a zero-percent discount.
- Currency calculation rounds to the mapped minor-unit precision (0, 2 or 3
  decimal places), midpoint away from zero; fee structure and quote snapshot it.
- Late fee is a school-configured fixed amount per overdue installment after
  configurable grace days, with an optional cap.
- Portal accounts are institution-scoped and use a normalized identity/email;
  one person may hold Guardian and optional Student personas. Staff credentials
  remain isolated.
- Uploaded documents use the established object-storage service. Until the
  school configures its jurisdictional retention/deletion rule, application
  behavior preserves records and archived objects.
- Admission transition map: Draft to Submitted or Cancelled; Submitted to
  UnderReview or Cancelled; UnderReview to Approved, Rejected or
  ReturnedForCorrection; ReturnedForCorrection to Submitted or Cancelled.
  Rejection, correction return and cancellation require an audit reason.
- Admission statuses are Draft, Submitted, Under Review, Approved, Rejected,
  Returned for Correction and Cancelled. These are centralized in
  `StudentAdmissionStatus` and implemented in the current admission API source.
- Student-owned identifiers use 64-bit IDs. Shared Country/State catalogues
  keep their existing key types.

The user authorized backend API implementation and its UI-developer
documentation only. The Angular workspace must not be edited, generated,
formatted, built, tested or installed as part of this work. Every API contract
will be documented under `docs/UIdeveloperDoc` with tested/deployed status.

### Proposed records and responsibilities

| Proposed record | Responsibility and key links |
| --- | --- |
| `StudentAdmission` | Separate application lifecycle: Institution, application number, requested session, campus/location, programme/group, submitted details, status, review outcome and audit history. Approved statuses are Draft, Submitted, Under Review, Approved, Rejected, Returned for Correction and Cancelled. |
| `Student` | Permanent learner identity, independent of a grade, section or year. Unique institution-scoped student code/admission identity, verified demographics and lifecycle state. Do not overwrite the identity when promoting a Student. |
| `StudentEnrollment` | One learner's placement for one AcademicSession, programme, group/grade and campus/location. Keep status, dates and roll number here. Promotion creates a new row and retains the old year's record. |
| `StudentSectionAssignment` | Effective-dated class/section history under an enrollment. A section transfer adds/closes an assignment rather than erasing the earlier section. |
| `Guardian` | Guardian's own identity and verified contact details. Do not duplicate the same parent for each child. |
| `StudentGuardian` | Student-to-Guardian link with relationship, primary/emergency/pick-up flags, communication and portal-access scope, verification and effective dates. |
| `StudentDocumentRequirementMaster` | Institution-admin-managed required/optional document catalogue and rules, scoped to applicable location, session, admission type and person (Student/Guardian). Proposed properties include display label, required flag, accepted file types, size limit, expiry requirement and ordering. |
| `StudentDocument` | Uploaded file metadata, StudentAdmission/Student/Guardian owner, requirement link, secure object-storage key, upload/version time, review status, reviewer and audit. Store file bytes in approved object storage, not as a database blob. |
| `StudentBenefitSchemeMaster` | Institution-managed scheme catalogue and eligibility rules scoped to the applicable campus/location and academic session, with configurable scheme-specific minimum/maximum discount percentages and required proof. Examples such as government schemes, BPL/Laxmi card, caste category, female concession or disability concession are not seeded policy. At most one scheme can be selected for one admission. |
| `StudentBenefitApplication` | Scheme claimed for a particular admission/enrollment, the applicant's declaration, evidence links, review decision, applied amount and reason. A scheme master alone must never silently discount a fee. |
| `StudentFeeFrequencyMaster` | Institution-configured payment frequency catalogue. Each row defines installments per year; active options are data, not hardcoded UI choices. A fee structure separately controls which frequencies parents may select. |
| `StudentFeeComponentMaster` | Institution-configured fee labels and charge nature: recurring annual or one-time. Recurring annual charges form the discount base; one-time admission/exam charges do not. Labels are institution-owned, not seeded universally. |
| `StudentFeeStructureMaster` | Versioned fee schedule for Institution + campus/location + AcademicSession + required AcademicGroup/grade. Programme is derived from the group; section-specific pricing is not supported. A new session creates a new structure while historic enrollments retain their original quote/schedule. |
| `StudentFeeStructureLine` | Amount and component reference for each structure. Annual total is derived from recurring lines; one-time charges remain explicit. |
| `StudentFeeStructureFrequency` | Allowed payment frequency links for a fee structure; the parent can choose only one of the active linked frequencies. |
| `StudentAdmissionFeeQuote` | Registration-time snapshot of fee structure, gross lines, selected scheme, percent and amount discount, net amount and parent-selected frequency shown before submission/approval. Later master edits must not silently alter the quote. |
| `StudentFeeSchedule` | Per-enrollment due installments with period, due date, charge, approved discount/adjustment, amount due and paid/balance state. Supports clear “paid for which months/period, paid on, next due date and overdue amount” views. |
| `StudentFeePayment` / allocation / receipt | Record a verified payment and allocate it to the Student's due items; preserve gateway/reference, amount, date, status, refund/reversal and receipt audit. Whether the existing platform `Payment*` tables are suitable must be checked before implementation; do not assume subscription billing tables are school-fee ledgers. |

Every proposed institution-owned `*Master` record should follow the existing
institution master conventions (institution ownership, active/soft-delete and
audit fields where the established model requires them). Geography must reuse
the existing Country/State/Locality and institution-location references after
their actual keys and relationships are verified; do not create duplicate
country/state catalogues. The exact location FK remains an implementation
inventory item because this proposal has not yet inspected every location
mapping.

### Which screen owns fees?

**Recommendation: keep fee setup separate from the Group/Grade screen.** The
current `AcademicGroup` is already tied to an `AcademicSession` and can have a
programme; use those existing academic records as references. A fee structure
then references the applicable session and group plus the institution's
location/campus. This keeps academic grouping focused on grade/programme while
fees can vary by year and location.

For example, the school could configure a 2028–29 Grade 5 structure for Campus
A with tuition and examination lines, and a different approved 2029–30 structure
without changing Grade 5 or the old year's student balance. The parent sees the
applicable structure and a dated quote during registration. After enrollment,
the generated schedule makes each installment's covered period, amount, due
date, paid date and balance visible. The numeric amounts and payment terms must
come from the school's approved data; this proposal does not invent prices.

The annual total should be derived from the component lines, not typed a second
time as an independent amount. “Exam fee included/excluded” should be represented
by the exam component's explicit package/charge treatment and then displayed as
a separate line or included line, never both. The institution should be able to
copy last year's structure as a draft, change amounts, review it and publish a
new session version while preserving the old one.

### Discount and benefit calculation

**Approved rule:** when a scheme is selected, enforce that scheme's minimum and
maximum percentage against the gross sum of recurring annual fee components.
Store the applied percentage and amount in the immutable admission quote. A
quote without a scheme receives 0% discount. At most one scheme is selectable
for an admission. One-time admission and examination fees are outside the
discount base. Rounding precision and any exception/override approval remain to
be specified before implementing final quote calculation.

Before a final quote, calculate component totals, apply only approved and
eligible concessions, show each adjustment and compute the net amount due. The
school must approve the order for overlapping schemes (for example a
location-specific programme plus a female concession) and whether a cap applies
per fee component or to the whole annual fee. Do not infer legal eligibility
from a label such as caste, card or disability; configure the local rule and
review evidence under the institution's approved policy.

### Location-specific documents and parent upload flow

The school administrator configures a document requirement for the relevant
location and effective session. During registration, the API returns the
requirements for the selected campus/location and application type. The parent
can upload each requested item, replace a rejected/expired upload and see its
review state. Authorized school staff review the file and record approval or a
clear correction reason. The system links each upload to the requirement so a
missing-document check is deterministic.

The supplied screenshot lists possible examples: Student birth certificate,
address proof, identity proof, recent photo, transfer certificate and previous
marksheet; it also shows parent/guardian identity, caste certificate and
vaccination/medical records as conditional examples. **These are not universal
requirements and must not be seeded as mandatory for every school/location.**
The institution selects its own requirements and legal basis by location. Avoid
collecting a full national identity number when a document or masked reference
is sufficient. Sensitive uploads require least-privilege access, authenticated
short-lived download links, file type/size validation, audit trail, retention
policy and secure deletion/archival rules approved for the school's jurisdiction.

### Login recommendation and compatibility boundary

The database screenshot shows the existing `LoginCredential` associated with
`StaffMemberId`; the current domain entity also requires that staff link. It is
therefore not safe to place a Student/Guardian row into that table with a fake
StaffMember. Separate Student and Guardian business tables are required
regardless of the login design.

**Approved design and current source:** keep the staff login page/API and
`LoginCredential` unchanged. The separate portal API uses institution-scoped
`StudentPortalAccount`, explicit Guardian and optional Student persona links,
and its own hash-only refresh/one-time-token tables. A verified DNS portal
hostname resolves the tenant. Existing password hashing and central JWT signing
are reused; portal tokens have distinct `StudentPortal` claims and policy.
Guardian data access must re-check active `StudentGuardian` relationships.
Login/refresh/logout/`me`, invitation acceptance/claim, password reset and
approval-triggered invitation queue source exists. Portal login is not
end-to-end available until schema application and authenticated HTTP/email
acceptance; Guardian self-service document upload remains pending. Staff login
remains unchanged.

### Registration and fee lifecycle

```text
School configures location + session + admission document rules
       -> School configures session/group fee structure and benefit schemes
       -> Parent selects campus, session, programme/group and enters application
       -> API returns required documents and eligible scheme options
       -> Parent links guardian(s), uploads evidence and declares any benefit
       -> API builds an itemized fee quote and payment schedule preview
       -> Parent reviews the net amount and submits the application
       -> Authorized school reviewer verifies documents and scheme evidence
       -> Approval transaction creates Student + StudentEnrollment + section
          assignment + Guardian links + approved fee quote/schedule
       -> Guardian (and optionally Student) account invitation is issued by
          the approved authentication flow
       -> Payments are recorded against due periods and receipts/balance shown
```

Approval must be atomic: either the permanent Student, initial enrollment,
approved Guardian links and fee snapshot/schedule are all created, or none are.
Rejected, returned or cancelled applications retain an auditable history but
must not leave an active enrollment or payable invoice. A later promotion adds
an enrollment for the next session; it does not create a second permanent
Student.

### Module and permission recommendation

Use the existing `Module`, `Operation`, `ModuleOperationMapping`, role/module
permission and institution-enabled module/operation pipeline shown in the
database. Proposed menu modules are:

- **Student Admissions & Records** — application list/create/edit, document
  review, admission decision, enrollment, guardian links and student record.
- **Student Fees** — fee component/frequency masters, session/location fee
  structures, concession/scheme review, quotes, schedules, payments, receipts
  and balances.
- **Parent/Student Portal** — scoped application status, document upload,
  approved fee quote, schedule, payment history and receipts.

These are functional boundaries, not final Module/Operation names. Exact module
codes, route/page identity, operation set, plan availability and staff role
assignments must be mapped through the existing authenticated permission/menu
flow. Do not hardcode numeric ModuleId/OperationId in UI or API examples. A
Guardian must not receive staff capabilities, and a teaching position must not
implicitly grant application permissions.

### Decisions still required for complete behavior

This document deliberately leaves these items for explicit approval rather
than inventing school policy:

1. Confirmed: fee structures are campus/session/group scoped, with no
   section-level price. Programme is derived through the selected group.
2. School-configured due-date calendar and refund rules remain to be specified;
   late fee is already approved as fixed amount per overdue installment after
   configurable grace days, with an optional cap. Frequency is selected per
   fee structure from its active allowed options.
3. Confirmed: at most one scheme per admission; scheme-specific
   minimum/maximum discounts are percentages of
   gross recurring annual charges only; no-scheme means zero discount; currency
   uses `CountryCurrencyMaster.DecimalPlaces` (0, 2 or 3), midpoint away from
   zero, and snapshots the precision in fee structure and quote records.
4. Country/state/location-specific schemes and eligibility documents, including
   which cases are guaranteed benefits versus discretionary concessions.
5. Which admission documents are mandatory by location/application type, who can
   see them, verification workflow, retention duration and deletion rules.
6. Final module/operation names, staff roles and subscription/plan availability.
7. The existing location FK and whether existing payment infrastructure can
   safely support institution fee receipts without mixing subscription billing.

### Authorized API-only implementation phases

1. Inventory exact current academic, location, document-storage, payment,
   authentication and permission patterns; record affected locked baselines.
2. Implement institution-owned document, benefit, payment-frequency and fee
   component masters, scoped to verified Campus/location and AcademicSession.
3. Implement admission, guardian-link and document-upload/review APIs with
   approval-readiness validation in the existing approval transaction and
   source contract tests; runtime rollback acceptance remains pending.
4. Implemented session/location fee structures, discount validation, immutable
   quote/schedule and offline payment-ledger APIs. Continue with separate
   Cashfree tuition checkout/reconciliation/webhook; subscription billing stays
   isolated.
5. **Source implemented; runtime WIP:** separate Guardian-default / optional
   Student portal auth, invitation accept/claim, password recovery,
   approval-triggered invite/email, and portal-scoped Guardian/Student document
   self-service. Apply/accept prerequisite schemas and test those flows while
   preserving Staff login/refresh.
6. Seed approved module/operation mappings through the existing permission
   pipeline and deliver handoff documentation without changing Angular.
7. Run locked regression gates and new scenario tests; document local and
   deployed status separately, and only then mark a phase complete.

Implementation progress through 2026-10-28: document-requirement,
fee-frequency, fee-component and benefit-scheme master APIs are implemented in
the working tree. Their four tables and module/operation mappings were verified
in Local and Render. Student-owned master IDs, template references and the
benefit evidence reference are `bigint`; identity sequences are also `bigint`
in both databases. Local conversion preserved existing values. Unit tests and
solution builds pass as recorded in the scenario report. A route audit found
that PUT/DELETE actions still constrained IDs to 32-bit despite their `long`
parameters; all eight route constraints now use `:long`, and the focused
Student suite protects this contract. Reflection tests verify route metadata;
an authenticated HTTP call above the 32-bit range has not yet been run. Role
grants, live authenticated CRUD, API deployment and production acceptance are
still pending. Student/admission/enrollment and non-destructive exit/history
APIs are implemented and their schema is applied to Local/Render. Guardian capture and
staff-assisted document upload/review flows, plus fee structure/quote/offline
payment/receipt/balance source APIs, are implemented. Student registry/document
and fee/currency SQL is applied and structurally matched on both targets. School-owned Cashfree
credential configuration and AES-GCM protection are implemented in source;
Cashfree checkout/reconciliation/webhook source APIs are now implemented. Their
provider/configuration contract tests pass, while database, authenticated HTTP,
Cashfree sandbox, webhook endpoint and deployed acceptance remain pending.
Separate Guardian-default/optional Student portal authentication, Guardian
self-upload and official education-identifier value read/write/verification
are implemented in source; authenticated and external-service acceptance remains open. The country/authority education identifier type master
API and registry SQL are applied to both databases. Angular is
out of scope and unchanged.

### Continuation state (2026-10-28, refreshed)

User-confirmed implementation defaults for the next API phases:

- Guardian is the default portal identity; Student login is optional. Portal
  credentials are separate per institution, while existing Staff login stays
  isolated. A normalized email/person identity may have Guardian and optional
  Student personas.
- Schools configure allowed fee frequencies; the parent chooses among them.
  Fees are Campus/Session/Group scoped, with no Section price. One scheme at
  most applies; its minimum/maximum percentage applies to recurring annual
  charges only. No scheme means zero discount. Currency uses the active campus
  country mapping's 0/2/3 minor-unit precision, midpoint away from zero. Late fee is a configurable fixed charge per overdue
  installment after a grace period, optionally capped.
- Required documents are configured by the institution's relevant location
  and admission context. Use the existing object-storage service; preserve
  uploaded material until a retention/deletion policy is configured.
- APAAR, UPN and similar official IDs are issued by their authorities. Store
  internal StudentCode separately; official IDs must be stored/verified via
  country/authority-scoped APIs and never generated by Skolenza.
- Admission approval source now checks the active primary Guardian/link set,
  applicable approved/unexpired required documents, and saved immutable fee
  quote/schedule before creating Student. Those operations and Student,
  enrollment, Guardian copies and status history share the existing transaction.
  PostgreSQL transaction/rollback acceptance is still pending.
- Admission status lifecycle and reason requirements are recorded above; the
  previously open due-date/late-fee default is now resolved by the user choice.

Work completed on 2026-10-28: added `GET /api/Student/options`, returning the
values from central Student enums under the existing Admissions `View`
permission. The query handler contract and authenticated controller route are
covered by `StudentRegistryApiTests` (5/5 passed). This is source/unit evidence
only; authenticated HTTP workflows and deployment are still pending. Local/Render schema parity is verified.
The handoff is in `docs/UIdeveloperDoc/STUDENT_ADMISSION_API_HANDOFF.md`; the
scenario evidence is `docs/testing/student/options-api/2026-10-28.md`.

Guardian progress in the same working session: added paged institution-scoped
Guardian profile list/create/update, per-Student link list/create/update/close,
primary-Guardian uniqueness and the user-approved relationship options
(Father, Mother, Guardian, Grandparent, Other). Guardian email is normalized
and unique within an institution. Link closure preserves the link/profile and
revokes relationship-level portal access. These routes use the existing
`STUDENT_ADMISSIONS` View/Add/Update/Delete permission pipeline. The additive
registry SQL and EF model include matching unique Guardian email and
one-primary-link indexes plus the relationship range check. Guardian links
are captured on admission drafts and copied during source-level approval.
Runtime/database acceptance remains pending.

Document progress (2026-10-28): staff-assisted versioned admission document
upload, paginated list, review decisions and short-lived signed download were
implemented through the existing storage service and permission pipeline.
Focused Student contracts passed 9/9; the locked pre/post gate passed 32/41
with nine explicitly skipped DB-dependent cases each time. Authenticated HTTP,
schema application, S3 round-trip, malware scanning and deployment remain
unverified. Guardian/Student portal document self-service is implemented as a
separate post-enrollment API using the StudentPortal policy and existing storage
service. Focused portal document tests pass 4/4; admission detail/history tests
pass 5/5; current Student suite passes 75/75; current protected gate is 32
passed with no skips; current full Release solution build has
0 errors and 22 warnings. These are source checks only. See
`docs/testing/student/admission-documents/2026-10-28.md`,
`docs/testing/student/portal-document-upload/2026-10-28.md` and
`docs/testing/student/admission-detail-status-history/2026-10-28.md`.

Next work order for continuation:

1. Use the verified backups and applied schemas. Test invitation/email, reset,
   Guardian/Student document scope and storage over authenticated Local HTTP.
2. Verify source-level atomic admission readiness in PostgreSQL with rollback
   and authenticated HTTP tests; separately add official-ID association only
   when its approved API and persistence are implemented.
3. With schema parity verified, configure effective role grants through the
   existing permission flow and run authenticated Local acceptance.
4. Run Cashfree sandbox, duplicate-webhook and atomic receipt scenarios; database
   schema is already applied; authenticated sandbox tests remain open.
5. Deploy only when authorized and record Render acceptance separately.

Keep this document and the UI handoff updated after each phase; do not touch
Angular.
