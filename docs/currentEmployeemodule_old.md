# Current Employee Module and Proposed Working Module

## 2026-09-12 implementation update

- `GET /api/Employee/get-all-percentage` now returns 13 sections: Overview, Bank, Contact, Experience, Insurance, Identity, Education, Dependent, Work Locations, Devices, Work Arrangement, Work Pattern, and Overrides.
- Existing Education, Bank, and Contact formulas are unchanged. New sections use the common `EmployeeProfileCompletionCalculator`; verification/edit workflow fields do not inflate data completion.
- Response `completionPercentage` is the rounded arithmetic mean of all returned sections; absent sections contribute zero.
- Persistence now enforces `IsInfoVerified = true => IsEditAllowed = false`; an edit-enable request cannot reopen a verified row.
- Bulk status now covers all seven verification-capable persisted profile areas plus Overview and validates tenant ownership.
- Angular now reads `ApiResponse.data`, sends numeric `tabInfoType`, omits assignment-only sections from the legacy status mutation, and forces edit false when verified.
- Backend build passed with zero errors; focused Employee profile tests passed 16/16.
- Angular compilation is blocked because this checkout has no `node_modules` and the machine npm launcher references a missing `npm-cli.js`; it is not claimed as passed.

Status: **IMPLEMENTATION IN PROGRESS — VERIFIED CHANGES RECORDED BELOW**  
Reviewed: 2026-09-12  
Scope: Angular Employee profile tabs, Employee APIs, backend handlers/repositories, permissions,
completion percentages, verification/editability, identity/statutory/compliance schema, dashboard,
and Employee bulk-import continuity.

> This document records the current behavior and a proposed implementation sequence. It does not
> authorize or claim any business-logic, schema, seed-data, API, UI, or deployment change. The
> screenshots supplied by the user are evidence of the current UI/database only, not instructions.
> Country rules, percentage weights, mandatory fields, and dependent eligibility must not be
> invented. They remain PENDING until approved and supported by an authoritative source.

## 1. User-required invariants

1. Existing behavior must remain unchanged during professionalization/refactoring.
2. Existing Employee handler/repository patterns, `#region` layout, XML summaries, endpoint
   documentation, constants, enums, mappings, and permission pipeline must be retained.
3. `IsEditAllowed = true` means the corresponding employee may edit that section.
4. `IsEditAllowed = false` removes that employee's ability to edit that section.
5. Verification wins over editability: when `IsInfoVerified = true`, effective edit permission must
   be false. A verified section must not remain editable.
6. Work Locations, Devices, Work Arrangement, Work Pattern, and Overrides require a tenant-wide
   default-editability facility, initially intended to be off. Its exact contract, affected existing
   rows, future-row behavior, exceptions, and rollback semantics require approval before coding.
7. Every added feature needs meaningful tests in `skolenza.automationtests`; COMPLETE means the
   relevant automated and authenticated real-time tests pass with zero hidden skips.
8. Employee bulk behavior must continue to follow
   `docs/AI_ASSISTED_BULK_IMPORT_REFERENCE.md`; reporting-manager, location/work arrangement,
   policy assignment, and device enrollment are currently documented as deferred scopes.

## 2. Current Angular profile surface

The Employee profile currently exposes 15 routes. The employee row action menu exposes 13 of them;
Leave and Files appear in the profile shell but not in the row action menu.

| UI tab | Route | Current API family | Current CRUD assessment |
| --- | --- | --- | --- |
| Overview | `basic-info` | `/api/Employee` | Read/update; separate official update and image get/update. Employee creation/list/status/delete live in the same family. |
| Bank | `bank-info` | `/api/Employee/Bank` | Create, read, update, delete present. |
| Contact | `contact-info` | `/api/Employee/Contact` | Create, read, update, delete present. |
| Experience | `experience-info` | `/api/Employee/Experience` | Create, read, update, delete and document-delete present. |
| Insurance | `insurance-info` | `/api/Employee/Insurance` | Enrol, list and delete present; no active update endpoint in this controller. |
| Identity (WIP) | `identity-info` | Angular currently calls `/api/Employee/Sensitive` | Read/create present; Angular declares update, but the backend Sensitive update action is commented out. This is a contract gap. |
| Education | `education-info` | `/api/Employee/Education` | Create, read, update, delete present. |
| Dependent | `dependent-info` | `/api/Employee/Dependent` | Create, list, detail, update, delete present. Multiple dependents are structurally supported; legal eligibility rules are not established by count alone. |
| Leave | `leave-info` | Leave API families | Separate leave/policy domain; must not be folded into personal-profile completion without an approved rule. |
| Files | `files-info` | No dedicated Employee CRUD family established by this audit | Requires contract/ownership review. |
| Work Locations | `location-assignment-info` | `/api/EmployeeLocationAssignment` | List, get-by-id, create, update, status, delete present. |
| Devices | `device-enrollment-info` | `/api/EmployeeDeviceEnrollment` | List, get-by-id, create, update, status, bind card, remove credential, delete present. |
| Work Arrangement | `work-arrangement-info` | `/api/EmployeeWorkArrangement` | List, get-by-id, create, update, status, delete present. |
| Work Pattern | `work-pattern-info` | `/api/EmployeeWorkPattern` | List, get-by-id, create, update, status, delete present. |
| Overrides | `work-mode-override-info` | `/api/EmployeeWorkModeOverride` | List, get-by-id, create, update, status, delete present. |

### 2.1 Exact Angular endpoint inventory

| Family | Methods and routes used by Angular |
| --- | --- |
| Employee | `POST /create`, `GET /get`, `GET /get-all`, `POST /update`, `POST /official/update`, `DELETE /delete-all`, `PUT /update-status`, `GET /get-summary`, `GET /get-profile-summary`, `GET /Image/get`, `POST /profile/pic/update`, `GET /get-all-percentage`, `POST /update-bulk` |
| Bank | `POST /create`, `GET /get`, `POST /update`, `DELETE /delete` |
| Contact | `POST /create`, `GET /get`, `POST /update`, `DELETE /delete` |
| Experience | `POST /create`, `GET /get`, `POST /update`, `DELETE /delete`, `DELETE /delete-doc` |
| Education | `POST /create`, `GET /get`, `POST /update-education`, `DELETE /delete` |
| Dependent | `POST /create`, `GET /get`, `GET /get-in-detail`, `POST /update`, `DELETE /delete` |
| Insurance | `POST /employee-insurance-enroll`, `GET /get-all-enroll`, `DELETE /delete` |
| Identity UI | `GET /Employee/Sensitive/get`, `POST /Employee/Sensitive/create`, Angular expects `POST /Employee/Sensitive/update` but backend action is inactive |
| Work Locations | `GET /get-all`, `GET /get-by-id/{id}`, `POST /create`, `POST /update`, `POST /update-status`, `DELETE /delete/{id}` |
| Devices | same core CRUD/status routes plus `POST /card/bind` and `POST /credential/remove` |
| Work Arrangement | `GET /get-all`, `GET /get-by-id/{id}`, `POST /create`, `POST /update`, `POST /update-status`, `DELETE /delete/{id}` |
| Work Pattern | same six CRUD/status routes |
| Overrides | same six CRUD/status routes |

This inventory proves route presence, not correctness. DTO parity, tenant isolation, soft-delete,
audit fields, ownership, concurrency, and authorization must be tested per operation.

## 3. Current percentage behavior

### 3.1 `get-all-percentage`

Current request:

```http
GET /api/Employee/get-all-percentage?employeeId={encodedEmployeeId}
```

Current backend flow:

```text
EmployeeController
  -> GetEmployeeProfileStatusQuery
  -> ValidateTenantUserRequestAsync
  -> decode employeeId with tenant encryption key
  -> CanAccessEmployeeDataAsync(PersonalDetails)
  -> BaseEmployeeRepository.GetEmployeeCompletionAsync
```

Despite the name, `GetEmployeeCompletionAsync` currently loads and returns only:

- Education
- Bank
- Contact

It does **not** currently return Overview, Experience, Insurance, Identity, Dependent, Leave, Files,
Work Locations, Devices, Work Arrangement, Work Pattern, or Overrides. The example response using
`data` matches the backend `ApiResponse<List<CompletionSectionDTO>>`; the Angular interface currently
expects `sections` at envelope level, while the component reads `res.sections`. This apparent API/UI
shape mismatch must be confirmed with a live response before changing either side.

Each section item contains:

- `SectionName`
- `CompletionPercent`
- `IsInfoVerified`
- `IsEditAllowed`
- `IsSectionCreate`

### 3.2 Existing calculation basis

`CompletionCalculatorHelper` is already a common static calculator, but repositories and DTO
extension methods call it inconsistently. Current behavior includes:

- percentage = filled checks / total checks, rounded to a whole number;
- multiple-record sections generally average row percentages;
- basic Employee includes active status, edit-disabled status, verified status and primary image as
  completion checks;
- Bank includes verification/editability and primary-account/document rules;
- Contact repeats several checks and includes verification/editability and primary selection;
- Education uses data/document fields but not the same verification/editability treatment;
- Experience counts boolean business states such as WFH, foreign experience, gaps and verification
  flags as if `true` always meant “complete”; legitimate `false` values therefore reduce completion;
- Dependent uses name, relation, DOB, nullable boolean presence and proof document;
- the Employee list's displayed overall percentage is an average of employees on the returned page,
  not necessarily a tenant-wide aggregate;
- dashboard Employee Overview uses counts, while its top summary endpoint is currently hardcoded;
  the current Admin Dashboard template does not call `get-all-percentage` directly.

### 3.3 Professional assessment without changing behavior

The calculator's location is common, but the policy is not yet a single source of truth. Calculation,
aggregation, section creation, verification, and edit authorization are mixed. Verification and edit
flags should not silently alter data-completeness semantics unless that definition is explicitly
approved. The safe first step is characterization testing, not formula replacement.

## 4. Verification and editability

The UI loads section states through `get-all-percentage` and saves them through:

```http
POST /api/Employee/update-bulk
```

The payload maps each section to `sectionName`, `isVerified`, and `isEditAllowed`. Current intended
invariant from the user is:

```text
effectiveIsEditAllowed = requestedIsEditAllowed && !isInfoVerified
```

This invariant must be enforced atomically on the server, not only by disabling a UI control. Reads
and every create/update/delete handler must use the same effective authorization decision. Tests must
prove that a verified section cannot be edited through a direct HTTP request. Existing section-name
constants/mappings must be reused; free-text branching must not be introduced.

## 5. Permission review

Current positive finding: `get-all-percentage` validates tenant context, decodes the tenant-bound ID,
and calls `CanAccessEmployeeDataAsync(...PersonalDetails)`.

Static concerns requiring endpoint-by-endpoint verification:

- controller-level `[Authorize]` is not visibly consistent across Bank, Contact, Education,
  Experience, Dependent, Insurance and Sensitive actions;
- absence of an action attribute does not prove an authorization hole because filters/behaviors may
  apply globally, so authenticated integration tests must decide this;
- module/operation IDs must continue through the existing permission interceptor/pipeline;
- access to another tenant's encoded/raw employee or child-record ID must fail;
- self-service and Admin behavior must be tested separately;
- section editability is a data-level rule and cannot replace module/operation authorization;
- verified data must reject mutation even when the caller otherwise has module permission, except
  through an explicitly approved Admin verification/reopen workflow.

## 6. Identity, statutory and compliance review

The database already contains domain building blocks:

- `IdentityCategory` and `IdentityCategoryDocument` define document classifications/types;
- `CountryIdentityRule` maps a country to an identity document and marks it mandatory/active;
- `EmployeeIdentity` stores employee identity values/files and verification state;
- `WorkDocumentType` and `EmployeeWorkDocument` cover work-document definitions and records;
- `CountryStatutoryRule` maps country statutory types and can carry a salary threshold;
- `EmployeeStatutoryAccount` stores employee statutory-account data;
- `ComplianceTypeMaster` and `ComplianceRule` provide country/state/tenant/effective-date JSON rules.

Current screenshots show seeded categories such as TAX, GOVT, SOCIALSECURITY and VOTER and documents
including PAN, AADHAAR, EPIC, SSN, ITIN, EMIRATES_ID, SIN and UAN. They do not establish complete,
correct, current legal rules for every country.

### 6.1 Required country-resolution order (proposal, not implemented)

Before coding, “employee origin location” must be defined. Possible sources—nationality, citizenship,
residence, work location, payroll country, or legal-employer country—are not interchangeable. Once
approved, a deterministic resolver should apply:

1. validate effective tenant context;
2. resolve the approved employee country source;
3. select active rules effective on the relevant date;
4. apply tenant override, then state rule, then country rule, with an approved priority convention;
5. return applicable identity/statutory/work-document requirements and mandatory flags;
6. calculate completion only against applicable requirements;
7. retain historical submissions when rules change; never silently delete or invalidate them.

Examples such as Aadhaar for an Indian employee and Emirates ID for a UAE employee are plausible but
must come from approved seed/legal data. A Dubai employee having two wives is a dependent-domain
scenario, not proof of an identity rule. Spouse limits, marriage recognition, insurance eligibility,
visa sponsorship and payroll treatment are separate country/policy questions and must not be inferred.

### 6.2 Seed-data constraint

No “all countries” seed should be created from memory. Required provenance per rule:

- jurisdiction and authoritative source;
- rule version/effective dates;
- mandatory/conditional status and condition schema;
- state/tenant override behavior;
- uniqueness, expiry and document-side requirements;
- migration idempotency and rollback;
- legal/product-owner approval date.

## 7. Proposed common completion architecture

This section is the requested **employeesmodulePurposedWorkingModule**. Names are conceptual until
existing constants/interfaces are reviewed and the design is approved.

### Phase 0 — behavior lock (PENDING)

1. Capture golden responses for every current CRUD endpoint and percentage path.
2. Add characterization tests for nulls, multiple rows, primary rows, documents, valid false booleans,
   verification/editability combinations, pagination, tenant boundaries and encoded IDs.
3. Record current UI screenshots/network payloads and database fixtures.
4. Do not refactor until this suite passes on unchanged code.

### Phase 1 — contract and permission audit (PENDING)

1. Produce a controller/action/DTO/handler/repository/table matrix for every tab.
2. Confirm each operation's existing ModuleId/OperationId and self/Admin access requirement.
3. Resolve the `data` versus `sections` response mismatch.
4. Resolve Identity's Angular update call versus inactive backend endpoint.
5. Add negative integration tests: unauthenticated, denied operation, cross-tenant, wrong employee,
   raw/tampered ID, soft-deleted row, verified-row mutation and disabled-edit mutation.

### Phase 2 — common completion policy, compatibility mode (PENDING)

1. Introduce one section registry using existing constants—not scattered string literals.
2. Separate `data completeness`, `verification state`, `editability`, and `section existence`.
3. Move field checks behind common policy interfaces while reproducing current output exactly.
4. Centralize rounding and multi-row aggregation, initially in compatibility mode.
5. Compare old/new results for representative fixtures; any difference blocks merge until approved.

### Phase 3 — approve the percentage definition (PENDING USER DECISION)

Recommended semantic model:

```text
section completion = completed applicable required items / applicable required items * 100
overall employee completion = approved weighted average of applicable sections
tenant dashboard completion = server-side aggregate across all in-scope employees
```

Questions that must be answered before switching from compatibility mode:

1. Are only mandatory fields counted, or mandatory plus optional fields?
2. Are all applicable fields equal-weighted, or are section/field weights configurable?
3. Does a missing optional section count as 0, 100, or excluded/not-applicable?
4. For multi-row sections, use average, best row, primary row, or mandatory-record coverage?
5. Are verification and editability displayed separately (recommended), or included in completion?
6. Which tabs belong in overall completion? Leave, Devices, Overrides and work assignments may be
   operational/configurational rather than employee-entered profile completeness.
7. Which employee population and status belong in the dashboard aggregate?
8. What is the rounding rule and snapshot/effective date?
9. How do country rules affect denominator changes and historical scores?

### Phase 4 — verification/editability invariant (PENDING)

1. Enforce verified-implies-not-editable atomically in the shared domain/service path.
2. Ensure all mutation handlers consult the same existing permission/data-access pipeline.
3. Define explicit Admin reopen/unverify semantics and audit trail; do not infer them.
4. Update UI controls only after server enforcement exists.
5. Test simultaneous verification/edit requests and direct API bypass attempts.

### Phase 5 — tenant-wide defaults for five operational tabs (PENDING CONTRACT)

Create a new API only after deciding whether defaults apply to existing records, future records, or
both. Scope: Work Locations, Devices, Work Arrangement, Work Pattern, Overrides. Required properties:

- authenticated tenant derived server-side;
- existing permission identifiers and Admin operation;
- idempotent request and transaction;
- dry-run/affected counts before apply for existing rows;
- section keys from existing constants;
- audit actor/time and optimistic concurrency;
- verified rows remain non-editable;
- no creation/deletion of assignments as a side effect;
- result counts per section plus failures; no partial silent success.

### Phase 6 — identity/statutory/compliance implementation (PENDING RULE APPROVAL)

1. Reconcile duplicated/overlapping identity paths (`Sensitive` UI versus identity entities).
2. Define the authoritative employee-country source and rule precedence.
3. Validate current entity relationships, unique indexes, tenant scope, effective dating and soft
   delete behavior.
4. Build read-only applicable-requirements evaluation first.
5. Add approved, sourced, idempotent country seed migrations in reviewable batches.
6. Then implement missing Identity CRUD using existing handler/repository conventions.
7. Include rule-driven Identity/Statutory completion only after denominator semantics are approved.

### Phase 7 — dashboard and percentage page (PENDING)

1. Replace per-employee N+1 calls with an authorized server-side aggregate/query.
2. Return section percentages for the selected employee and a separately named overall value.
3. Return tenant-wide aggregate only from a dashboard-specific contract.
4. Define empty/not-applicable explicitly; never present it as silently completed.
5. Remove current hardcoded dashboard summary only under a separately approved change.

### Phase 8 — Employee bulk gap closure (PENDING)

1. Preserve current Employee base import behavior and tests.
2. Do not make Employee creation imply manager assignment, work assignment, policy mapping or device
   enrollment.
3. Add each new bulk module only after its manual CRUD/rules and permission pipeline are stable.
4. Use preview/confirm/durable-job/report patterns from the bulk reference.
5. Test dependency ordering, country resolution, duplicate identity values, retries, cancellation,
   tenant isolation, worker permission recheck and encrypted sensitive staging.

## 8. Required validation matrix before COMPLETE

For every tab/feature:

- create/read/update/delete or documented not-applicable operation;
- valid Admin, valid employee self-service and denied role;
- edit allowed true/false;
- verified true forces effective edit false;
- cross-tenant and tampered identifier rejection;
- inactive/soft-deleted parent and child rows;
- duplicate/primary/unique constraints;
- file upload type/size/path and deletion behavior where applicable;
- completion at 0, partial and 100, including multiple rows;
- country/state/tenant rule precedence and effective dates where applicable;
- API response contract matches Angular interface;
- database state and audit fields verified after HTTP call;
- EmployeeBulk regression suite remains green.

Real-time acceptance requires an isolated database, authenticated HTTP calls through the actual API,
and reconciliation of response, persisted rows and effective permissions. Production execution,
migration or seed application is not implied by local passing tests.

## 9. Current gaps and conflicts summary

| Finding | Severity | Status |
| --- | --- | --- |
| `get-all-percentage` returns only Education/Bank/Contact | High | Confirmed current limitation |
| Angular reads `res.sections`; backend standard success places list in `data` | High | Requires live contract confirmation |
| Dashboard template does not directly call `get-all-percentage` | Medium | Confirmed by static analysis; inspect browser network for layout/other branch evidence |
| Top dashboard employee summary endpoint is hardcoded | High | Confirmed current behavior |
| Identity Angular service targets `Sensitive`; backend update is inactive | High | Confirmed contract gap |
| Percentage rules count verification/editability and some boolean truth values as completeness | High | Preserve until characterization + approval |
| Page-level “overall” is based on returned records/page in reviewed path | High | Confirm scope with live response |
| Controller authorization declarations are inconsistent | High | Integration test required; do not infer global pipeline absence |
| Work/Device tabs are not in current completion response | Medium | Product decision required before inclusion |
| Universal country rules are not evidenced or safely seedable from screenshots | Critical | Legal/product data required |
| Bulk location/work arrangement/policy/device assignment remains deferred in reference | High | PENDING; no silent coupling |

## 10. Evidence reviewed

- Angular profile routes, action menu, Employee services, Verification Settings and Admin Dashboard.
- Employee controllers, query/command handlers, repositories, DTOs and completion helper.
- Work Location, Device Enrollment, Work Arrangement, Work Pattern and Override API families.
- Identity/statutory/compliance domain entities and DbContext registrations.
- User screenshots of current profile tabs and database tables/data.
- `docs/AI_ASSISTED_BULK_IMPORT_REFERENCE.md` through its 2026-09-12 entries.

## 11. Status

- Current-state analysis/documentation: **COMPLETE**.
- Phase 0 behavior-lock tests: **COMPLETE — 14/14 passed, zero skips**.
- Phase 1 static permission/route/DTO audit: **COMPLETE**; every concrete `EmployeeCmd` request is
  mapped to a server-owned leaf module and carries the existing permission DTO contract.
- Code/schema/API/UI/seed changes: **PENDING USER APPROVAL**.
- Related Work/Device + EmployeeBulk regressions: **181/181 passed, zero skips**.
- Employee isolated PostgreSQL regression subset: **17/17 passed, zero skips**.
- Live unauthenticated API denial probes: **COMPLETE**; protected families returned 401 after valid
  request binding. Full authenticated legacy-tab CRUD remains PENDING because no test-user credential
  fixture is configured for the running development API.
- Production migration/deployment: **NOT STARTED / NOT CLAIMED**.
