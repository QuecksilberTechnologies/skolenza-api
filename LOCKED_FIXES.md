# Locked Fix Regression Protocol

This file is the canonical registry for user-approved fixes whose behavior must
remain stable while later work is performed. It applies to every contributor,
account, automated agent and session working on this project.

## Mandatory first-read rule

1. Read this entire file before inspecting, changing or testing application
   code, database state, configuration or API behavior.
2. Read the repository `AGENTS_old.md` and any feature-specific reference named by
   it before starting work.
3. Before fixing another feature, identify every locked item that the proposed
   work can affect through shared handlers, repositories, entities, database
   tables, middleware, permissions, response contracts or UI consumers.
4. Run the affected locked items' **pre-change regression gate** before making
   the new change. If a gate fails, stop the new feature work, record the actual
   failure and tell the user. Do not hide the failure or relabel it as unrelated.
5. After the authorized change, rerun the same locked gates and the new
   feature's tests. A new fix is not locked until both sets pass and its evidence
   is recorded.

These regression runs protect established behavior and therefore are not
documentation-only reruns.

## Angular authorization boundary

- Do not edit, format, generate, build, test, install dependencies in, or
  otherwise change the Angular workspace unless the user explicitly authorizes
  Angular work in the current request.
- Backend work, an API error, a related feature name or an existing UI consumer
  does not imply permission to touch Angular.
- If an authorized backend change requires an Angular contract change, prepare
  the backend result and explain the exact required UI change. Wait for explicit
  Angular authorization before applying it.
- When Angular work is authorized, read the Angular repository's `AGENTS_old.md`
  before touching that workspace and run every applicable UI lock gate.

## Meaning of a locked fix

A `LOCKED` entry establishes its listed observable behavior as a regression
baseline. Later work must not silently change that behavior, API contract,
transaction outcome, permission path, persistence rule or user-facing error.

- Only an explicit user instruction can change, unlock or retire locked
  behavior.
- A refactor is not permission to alter a locked result.
- An expanded test suite may add coverage, but it must preserve the original
  locked assertions unless the user approves a behavior change.
- Skipped, blocked and not-run checks are never passes.
- Local verification is not deployed acceptance.
- Never store credentials, tokens, real identity numbers or unredacted personal
  documents in this registry or its evidence reports.

## Required workflow for every small fix

### 1. Define the requested scope

- Restate the exact defect and intended result.
- List the files, APIs, tables and consumers likely to be affected.
- Ask the user if intended behavior is unclear; do not invent a flow.

### 2. Check existing locks before changing code

- Select applicable entries from the registry below.
- Run each selected entry's pre-change gate exactly as documented.
- Record passed, failed, skipped and blocked counts honestly.
- If no lock applies, state why in the new scenario report.

### 3. Implement only the authorized fix

- Preserve existing project patterns, permission flow, regions, comments,
  constants, enums and mappings.
- Do not combine an unrelated cleanup, refactor or UI change with the fix.
- Do not weaken an existing assertion just to make a regression pass.

### 4. Verify and lock the result

- Rerun all applicable pre-change gates as post-change gates.
- Add meaningful cases to an existing relevant test when that is the established
  test location. Create a new suite only when no suitable suite exists.
- Create the required scenario report under
  `docs/testing/<module>/<scenario>/<YYYY-MM-DD>.md` and link it from the test
  indexes required by `AGENTS_old.md`.
- Add or update one registry entry below. Mark it `LOCKED` only after required
  local checks pass. Record deployed verification separately.

## Lock entry requirements

Every entry must include:

- stable lock ID, date and status;
- user-approved behavior that must remain unchanged;
- protected APIs, code areas and persistence objects;
- exact pre-change and post-change regression commands;
- expected assertions or minimum result;
- evidence report;
- local and deployed verification status;
- any approved exception or remaining acceptance item.

## Lock registry

### Skolenza regression selector correction — 2026-10-29

The inherited Employee/Tenant test selectors below are retained as historical
references. Their current Skolenza equivalents use `StaffMember` and
`Institution` names. For the affected Bank, contact/profile and registration
gates, the following current selectors replace the obsolete selectors; other
required gate commands remain in force. Zero matched tests never constitute a
pass. No locked product behavior or test assertion is changed by this selector
correction. Local verification:
Bank 1/1, contact/profile 52/52, locality 9/9, institution creation/update
15/15, role 26/26 and staff/academic/rollback 41/41. See
[key-rotation investigation](docs/testing/student/portal-email-key-rotation/2026-10-29.md).

```powershell
dotnet test .\Skolenza.automationtests\Skolenza.automationtests.csproj -c Release --no-build --filter "TestCategory=StaffMemberBankEncryption&TestCategory!=StaffMemberBankEncryptionDatabase" --logger "console;verbosity=minimal"
dotnet test .\Skolenza.automationtests\Skolenza.automationtests.csproj -c Release --no-build --filter "TestCategory=StaffMemberContactRelation|FullyQualifiedName~StaffMemberProfileCharacterizationTests" --logger "console;verbosity=minimal"
dotnet test .\Skolenza.automationtests\Skolenza.automationtests.csproj -c Release --no-build --filter "FullyQualifiedName~HostApiRegressionTests.Institution_creation_awaits_dependencies_and_preserves_transaction_outcome|FullyQualifiedName~HostApiRegressionTests.Host_institution_update_synchronizes_the_canonical_location" --logger "console;verbosity=minimal"
$env:SKOLENZA_HOST_DB_SETTINGS=(Resolve-Path '.\Skolenza.api\appsettings.Development.json').Path
dotnet test .\Skolenza.automationtests\Skolenza.automationtests.csproj -c Release --no-build --filter "FullyQualifiedName~HostApiRegressionTests.Institution_creation_real_database_rollback_probe" --logger "console;verbosity=minimal"
```

| Lock ID | Area | Status | Local baseline | Deployed status | Evidence |
| --- | --- | --- | --- | --- | --- |
| `LOCK-TENANT-REG-001` | Tenant registration transaction and actionable errors | LOCKED | Backend 14/14; PostgreSQL rollback 2/2; Angular 44/44 and production build | PENDING | [2026-10-01](docs/testing/tenant/registration-actionable-errors/2026-10-01_old.md) |
| `LOCK-ROLE-TYPE-002` | Original Admin/Employee/Manager/Client display labels | SUPERSEDED by `LOCK-ROLE-PERSONA-007` | Historical backend/UI baseline preserved | SUPERSEDED | [2026-10-01](docs/testing/role/client-role-type-display/2026-10-01_old.md) |
| `LOCK-EMP-CONTACT-003` | Employee contact relations, initial row and location cascade | LOCKED | Backend 52/52 current combined gate + contact DB 2/2; locality 9/9; protected tenant 14/14 + DB rollback 2/2; Angular 66/66; Role 20/20 current backend + 69/69 UI; production build | PENDING | [2026-10-01](docs/testing/employee/contact-relation-location/2026-10-01_old.md) |
| `LOCK-EMP-EDU-004` | Employee Education create date and score-type mapping | LOCKED | Mapping 1/1; protected Employee profile/contact 52/52; Release build | PENDING | [2026-10-03](docs/testing/employee/education-create-date-mapping/2026-10-03_old.md) |
| `LOCK-EMP-BANK-005` | Employee Bank sensitive fields encrypted at rest | LOCKED | Unit 1/1; Local DB 1/1; Render DB 1/1; protected backend gates pass; Release build | API DEPLOYMENT PENDING | [2026-10-03](docs/testing/employee/bank-sensitive-field-encryption/2026-10-03_old.md) |
| `LOCK-POLICY-ASSIGN-006` | Applicability-safe employee assignment picker, bulk and export | LOCKED | Policy contract/schema 32/32; Release build | PENDING | [2026-10-03](docs/testing/policy/assignment-applicability-mapping/2026-10-03_old.md) |
| `LOCK-ROLE-PERSONA-007` | Professional Tenant role names, role-type personas and remarks | LOCKED | Backend 15/15; protected tenant registration 14/14; Local/Render data verified | API DEPLOYMENT PENDING | [2026-10-04](docs/testing/role/professional-access-personas/2026-10-04_old.md) |
| `LOCK-TENANT-LOCATION-008` | Initial TenantLocation creation and Host synchronization | LOCKED | Creation/update 15/15; PostgreSQL rollback 2/2; Role 20/20 | API DEPLOYMENT PENDING | [2026-10-04](docs/testing/tenant/initial-location-lifecycle/2026-10-04_old.md) |
| `LOCK-POLICY-DOWNLOAD-009` | Policy Assignment Download module-operation mapping | LOCKED | Policy contract/schema 33/33; Release build; Local/Render mapping verified | DATA APPLIED; API DEPLOYMENT NOT REQUIRED | [2026-10-07](docs/testing/policy/assignment-download-operation/2026-10-07_old.md) |
| `LOCK-POLICY-PUBLISH-010` | Policy document, approval fingerprint, clone and archive integrity | LOCKED | Policy contract/schema 37/37; Release build; Angular production build; Local/Render schema verified | API/UI DEPLOYMENT PENDING | [2026-10-07](docs/testing/policy/publication-integrity/2026-10-07_old.md) |
| `LOCK-POLICY-LEAVE-011` | Leave Type targeting across policy versions, rules and applicability | LOCKED | Policy contract/schema 40/40; Angular 7/7 and production build; Local/Render schema and 8-type seed verified | DB APPLIED; API/UI DEPLOYMENT PENDING; LOCAL S3 PASS, RENDER STORAGE ACCEPTANCE PENDING | [2026-10-08](docs/testing/policy/leave-type-targeting/2026-10-08_old.md) |
| `LOCK-POLICY-DRAFT-EDIT-012` | Policy list Draft row opens its exact version in Edit | LOCKED | Angular 8/8; production build; localhost Draft-v2 browser verification | UI DEPLOYMENT PENDING | [2026-10-08](docs/testing/policy/draft-version-edit/2026-10-08_old.md) |
| `LOCK-POLICY-ACCRUAL-013` | Leave annual entitlement and derived accrual-cycle amount | LOCKED | Backend 47/47; Angular 27/27; Release and production builds; Local/Render metadata verified | DB APPLIED; API/UI DEPLOYMENT PENDING | [2026-10-08](docs/testing/policy/annual-entitlement-accrual/2026-10-08_old.md) |
| `LOCK-POLICY-IDENTITY-014` | Tenant policy-name uniqueness and version guidance | LOCKED | Backend 48/48; Release build; Local/Render unique index verified | DB APPLIED; API DEPLOYMENT PENDING | [2026-10-09](docs/testing/policy/policy-name-uniqueness/2026-10-09_old.md) |
| `LOCK-POLICY-DRAFT-SAVE-015` | Atomic replacement of Draft policy rules and targets | LOCKED | Backend 49/49; Release build | API DEPLOYMENT PENDING | [2026-10-09](docs/testing/policy/draft-replacement-save/2026-10-09_old.md) |
| `LOCK-EMP-PROFILE-COMPLETION-016` | Mandatory-aware Employee profile completion consistency | LOCKED | Employee/contact 52/52; Bank 1/1; locality 9/9; tenant 14/14 + DB rollback 2/2; Role 20/20; Release build | API DEPLOYMENT PENDING | [2026-10-10](docs/testing/employee/profile-completion-consistency/2026-10-10_old.md) |
| `LOCK-STAFF-PROFESSIONAL-MODEL-017` | Runtime-valid professional master mapping | LOCKED | Staff professional 4/4; API startup/EF model/Swagger/runtime probes | PENDING | [2026-10-12](docs/testing/staff/professional-structure/2026-10-12.md) |
| `LOCK-STAFF-ACADEMIC-018` | Tenant-owned academic hierarchy and effective staff teaching responsibility | LOCKED | Staff combined 12/12; tenant rollback 2/2; Release API and Angular production builds; authenticated local CRUD/conflict/isolation | DB/API LIVE; AUTHENTICATED UI PENDING | [2026-10-13](docs/testing/staff/academic-structure/2026-10-13.md) |
| `LOCK-HOST-STAFF-CATEGORY-CATALOGUE-021` | Host-owned Staff Category catalogue and permission-scoped institution setup | LOCKED | Staff gates 14/14; Angular catalogue/interceptor 67/67; backend Release and Angular production builds | Render DB seed PASS; API/UI deployment and authenticated browser acceptance PENDING | [2026-10-14](docs/testing/staff/host-staff-category-catalogue/2026-10-14.md) |
| `LOCK-CURRICULUM-API-PAGINATION-022` | Permission-scoped curriculum pagination and independent Topics | LOCKED | New paging 9/9; affected Staff and tenant rollback combined 33/33; Release build and local HTTP probes | API deployment and UI integration PENDING | [2026-10-17](docs/testing/staff/curriculum-pagination/2026-10-17.md) |
| `LOCK-PROFESSIONAL-API-PAGINATION-023` | All five institution Professional Structure tab lists use central paging | LOCKED | Combined Staff/Curriculum/rollback 38/38; Debug and Release builds; local HTTP paging/filter/error probes | API deployment and UI integration PENDING | [2026-10-18](docs/testing/staff/professional-pagination/2026-10-18.md) |
| `LOCK-ACADEMIC-API-PAGINATION-024` | All six Academic master tab lists reuse central paging | LOCKED | Combined suite 41/41; Debug/Release builds; all 17 structure/curriculum list variants verified over local HTTP | API deployment and UI integration PENDING | [2026-10-18](docs/testing/staff/academic-pagination/2026-10-18.md) |
| `WIP-STUDENT-PERMISSION-025` | Student admission detail and payment reconciliation permissions | WIP — authenticated HTTP acceptance pending | Student 84/84; protected Staff/Academic/Curriculum gate 41/41 before and after; Release build | API deployment and authenticated acceptance PENDING | [2026-10-29](docs/testing/student/permission-behavior/2026-10-29.md) |
| `WIP-STUDENT-ADMISSION-CONCURRENCY-026` | Student admission and related write ordering | WIP — full approval and authenticated HTTP acceptance pending | Student 93/93 with disposable DB; protected gate 41/41; Release build | API deployment and authenticated acceptance PENDING | [2026-10-29](docs/testing/student/admission-concurrency/2026-10-29.md) |

## LOCK-TENANT-REG-001: Tenant registration transaction and actionable errors

### Locked behavior

- Public and Host tenant registration follow the established transaction flow.
- A failed creation does not leave a partially committed tenant graph.
- Duplicate email is rejected before a transaction begins.
- Known database conflicts and unexpected failures return a safe, actionable
  stage-specific message and stable error code. Raw exception, SQL and database
  details remain server-log-only.
- Request cancellation rolls back and returns a clear cancellation result.
- Verification-email failure after commit does not roll back the created tenant;
  the result reports that the email was not sent.
- Tenant registration does not implicitly create obsolete Insurance or Leave
  policy types.
- The Angular registration screen, when explicitly authorized for UI work,
  displays the API message plus distinct validation details and uses clear
  fallbacks when no detail is available.

### Protected areas

- `skolenza.application/Features/RegistrationCmd/Handlers/CreateTenantCommandHandler.cs`
- `skolenza.application/Constants/AppConstants.cs`
- `skolenza.api/Controllers/Tenant/TenantController.cs`
- `skolenza.automationtests/Unit/HostApiRegressionTests.cs`
- Tenant, tenant subscription, security key, location, employee, role,
  permission and tenant-profile persistence used by registration.
- Angular registration response contract, component and existing component
  specification. The Angular authorization boundary above still applies.

### Required backend gate

Run before and after any change that can affect this lock:

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "FullyQualifiedName~HostApiRegressionTests.Tenant_creation_awaits_dependencies_and_preserves_transaction_outcome" --logger "console;verbosity=minimal"
```

Expected locked baseline: all named public and Host cases pass. At lock time the
result was 14 passed, 0 failed and 0 skipped.

For database, entity, repository, transaction or registration-persistence
changes, also run the opt-in rollback probe against the explicitly approved
Development settings:

```powershell
$env:SKOLENZA_HOST_DB_SETTINGS=(Resolve-Path '.\skolenza.api\appsettings.Development.json').Path
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "FullyQualifiedName~HostApiRegressionTests.Tenant_creation_real_database_rollback_probe" --logger "console;verbosity=minimal"
```

Expected locked baseline: 2 passed, 0 failed and 0 skipped, with both transactions
rolled back and no test tenant persisted. If the environment variable or
database is unavailable, record the probe as BLOCKED or SKIPPED and do not claim
database regression acceptance.

### Required Angular gate

Run only after the user explicitly authorizes Angular work:

```powershell
bun run test -- --include src/app/features/authentication/registration/registration.spec.ts
bun run build -- --configuration production
```

Expected locked baseline: the focused registration specification passes (44
tests at lock time) and the production bundle builds successfully.

### Known data-integrity prerequisite

Tenant cleanup must not leave orphan tenant-encryption-key rows. The lock-time
failure was caused by four orphan `TenantEncryptionKeys` rows colliding with
reused tenant identities; the local orphans were removed and the final orphan
count was zero. Do not delete or alter tenant data for a regression run without
the user's explicit authorization.

### Acceptance state

- Local backend mocked gate: PASS.
- Local PostgreSQL rollback gate: PASS.
- Local Angular focused tests and production build: PASS under the authorization
  that created this lock.
- Deployed end-to-end registration: PENDING and must not be reported as passed
  until independently executed and reconciled.

## LOCK-ROLE-TYPE-002: Tenant role-type response mapping and API-backed DDL

Status: **SUPERSEDED on 2026-10-04 by `LOCK-ROLE-PERSONA-007`** with explicit
user authorization to replace the four original display labels. Numeric values,
permission behavior and the API-backed DDL architecture remain protected.

### Locked behavior

- Persisted Tenant role type `1` displays as `Super Admin`.
- Persisted Tenant role type `2` displays as `Employee`.
- Persisted Tenant role type `3` displays as `Manager`.
- Persisted Tenant role type `4` displays as `Client`, never `Unknown`.
- An unsupported numeric role type uses the safe `Unknown` fallback.
- Role GET and login role responses resolve names through the same centralized
  constants mapping.
- `GET /api/Role/type-options` is bearer-authenticated and returns the supported
  1–4 option catalogue from the same constants. It accepts no ModuleId or
  OperationId and deliberately bypasses the Role module-operation pipeline.
- Existing Role create/update/list/delete, permission and bulk-import requests
  retain their established permission behavior.
- Angular Role Add, Edit and filter controls use the API-backed shared signal;
  no hardcoded `ROLE_TYPES` list may replace it. The Angular permission
  interceptor must not attach permission IDs to this lookup.

### Protected areas

- `skolenza.application/Constants/AppConstants.cs`, Tenant Role Types region.
- `skolenza.application/Mappings/MappingProfile.cs`, mappings from `Role` to
  `GetRoleResponseDTO` and `NewLoginRoleDTO`.
- `skolenza.application/Features/RoleCmd/Handlers/GetRoleTypeOptionsQueryHandler.cs`.
- `skolenza.application/Features/RoleCmd/RolePermissionBehavior.cs`, the narrow
  `GetRoleTypeOptionsQuery` exception only.
- `skolenza.api/Controllers/Role/RoleController.cs`, `GET type-options`.
- `skolenza.automationtests/Unit/RoleTypeMappingTests.cs`.
- `GET /api/Role/get` role-type fields and login role-type display fields.
- Angular `roles-api.ts`, `role-dialog`, `roles-list`, role interfaces and the
  `/Role/type-options` interceptor exclusion.

### Required backend gate

Run before and after any change that can affect this lock:

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "TestCategory=RoleTypeMapping" --logger "console;verbosity=minimal"
```

Expected locked baseline: 9 passed, 0 failed and 0 skipped. The assertions cover
types 1, 2, 3, 4 and an unsupported value across the central resolver and response
mappings, the four-option query output, the no-permission pipeline exception and
the authenticated controller route. A regular Role query still rejects missing
permission IDs, proving the exception does not weaken existing Role operations.

Because `AppConstants.cs` is also protected by `LOCK-TENANT-REG-001`, changes to
that file must additionally run that lock's backend gate.

### Required Angular gate

Run before and after changes to the protected Role option consumers or interceptor:

```powershell
C:\latestSkolenzaUI\skolenza-app\node_modules\.bin\ng.exe test --project skolenza --include C:\latestSkolenzaUI\skolenza-app\src\app\features\roles\role-dialog\role-dialog.spec.ts --include C:\latestSkolenzaUI\skolenza-app\src\app\core\services\roles-api.spec.ts --include C:\latestSkolenzaUI\skolenza-app\src\app\core\interceptors\module-operation-routes.spec.ts --watch=false
```

Expected locked baseline: 3 files and 69 tests pass.

### Acceptance state

- Local backend gate: PASS, 9/9.
- Shared constants tenant-registration gate: PASS, 14/14.
- Angular Role option gate: PASS, 69/69.
- Angular production build: PASS.
- Authenticated local Role GET JSON: PENDING.
- Authenticated local `GET /api/Role/type-options`: PENDING.
- Deployed Role GET: PENDING.
- Deployed Role type options and UI acceptance: PENDING.
- Angular Role create/edit dropdown: known hardcoded 1–3 list, unchanged and
  outside this backend lock.

## LOCK-EMP-CONTACT-003: Employee contact relations, initial row and location cascade

### Locked behavior

- Employee contact relations come from backend `EmergencyContactRelation`; no
  Angular `RELATIONS` catalogue may replace it.
- Values 1–16 and 99 remain stable. `Owner` is value 16 and `Other` is 99.
- `GET /api/Employee/Contact/relation-options` is bearer-authenticated, accepts
  no ModuleId/OperationId or employee ID. As explicitly requested by the user,
  only bearer-token verification applies; no tenant/user database or permission
  query runs. This follows the existing Role-options boundary. Other Employee
  requests retain the established permission pipeline. This corrects the initial
  draft's extra tenant-context check to match the same user request, before final acceptance.
- Normal Employee creation adds exactly one editable, active, unverified,
  non-primary EmployeeContact to the same aggregate and transaction. It copies
  only full employee name and CountryId; Relation and the remaining user-entered
  contact/address fields are null.
- Existing Contact rows remain editable and employees may create more rows
  through existing Contact CRUD authorization.
- Manual Contact Add/Edit loads Country -> State -> District -> Locality, clears
  downstream values when a parent changes, and persists nullable LocalityId.
- Contact reads return nullable LocalityId/LocalityName. The additive migration
  never rewrites or deletes existing EmployeeContact rows.
- Contact edit accepts international plus-prefixed numbers while preserving
  legacy ten-digit numbers and ownership/editable/verified guards.
- The approved public UAE seed is additive/idempotent and preserves non-AE rows.
  Missing source district membership stays explicitly Unassigned; no postal
  codes or administrative membership are invented.

### Protected areas

- `skolenza.application/Common/Enums/OperationType.cs`,
  `EmergencyContactRelation`.
- Relation DTO/query/handler, `ContactController.GetRelationOptions` and the
  narrow exception in `EmployeeTenantPermissionBehavior`.
- `EmployeeContactInfoMapperHelper.CreateInitialContact` and
  `CreateBaseEmployeeInfoCommandHandler` aggregate wiring.
- EmployeeContact entity/EF model/repository, Contact create/update/get DTOs and
  `database-scripts/AddEmployeeContactDefaultAndLocality.sql`.
- `EmployeeContactRelationAndCreationTests` and Employee profile route/permission
  characterization.
- Angular EmployeeContactsAPI, LookupStore relation/location resources, Contact
  form, Employee Manage dialog, relation pipe/display consumers and the
  `/Employee/Contact/relation-options` interceptor exclusion.

### Required backend gates

Run before and after any change that can affect this lock:

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "TestCategory=EmployeeContactRelation|FullyQualifiedName~EmployeeProfileCharacterizationTests" --logger "console;verbosity=minimal"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "FullyQualifiedName~LocalityRefactorTests&FullyQualifiedName!~Four_country_postal_seed_is_idempotent_and_populates_locality_postal_code" --logger "console;verbosity=minimal"
```

Expected current baseline: 52/52 Employee contact/profile and 9/9 applicable
locality tests pass, with zero failed or skipped. The excluded historical
postal-seed test is BLOCKED by its absent
`database-scripts/SeedFourCountryPostalLocalities.sql` fixture and must never be
reported as passed until that fixture is restored or the test is explicitly
retired.

Because Employee persistence and shared constants/interceptor paths overlap
existing locks, also run their applicable gates:

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "FullyQualifiedName~HostApiRegressionTests.Tenant_creation_awaits_dependencies_and_preserves_transaction_outcome" --logger "console;verbosity=minimal"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "TestCategory=RoleTypeMapping" --logger "console;verbosity=minimal"
```

Expected baselines: Tenant 14/14 and Role 20/20 pass. For entity/repository/schema
changes, run the LOCK-TENANT-REG-001 real-database rollback probe; expected 2/2
pass with no retained test tenant.

For Contact persistence and UAE geography, also run (after applying the additive
schema script and UAE seed to the approved local target):

```powershell
$env:SKOLENZA_CONTACT_DB_SETTINGS=(Resolve-Path '.\skolenza.api\appsettings.Development.json').Path
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "TestCategory=EmployeeContactDatabase" --logger "console;verbosity=minimal"
```

Expected: 2 passed, no skipped; initial contact, edit/add/read-back, UAE lookup
hierarchy and rollback cleanup are verified. The DB probe substitutes auth/email;
it does not establish running-product authenticated HTTP acceptance.

### Required Angular gates

Run only with explicit Angular authorization:

```powershell
npx ng test --watch=false --include=src/app/core/services/employee-contacts-api.spec.ts --include=src/app/shared/pipes/get-relation-name-pipe.spec.ts --include=src/app/features/user-menu/employee-profile/employee-contact-info/employee-contact-form/employee-contact-form.spec.ts --include=src/app/core/interceptors/module-operation-routes.spec.ts --include=src/app/shared/components/employee/employee-manage-dialog/employee-manage-dialog.spec.ts --include=src/app/features/user-menu/employee-profile/employee-contact-info/employee-contact-info.spec.ts --include=src/app/features/user-menu/employee-profile/employee-basic-info/employee-basic-info.spec.ts --include=src/app/features/employees/avatar-popup/avatar-popup.spec.ts
npx ng test --project skolenza --include src/app/features/roles/role-dialog/role-dialog.spec.ts --include src/app/core/services/roles-api.spec.ts --include src/app/core/interceptors/module-operation-routes.spec.ts --watch=false
npx ng build --configuration production --progress=false
```

Expected baselines: affected Employee UI 8 files/66 tests, protected Role UI 3
files/69 tests, and the production build all pass. Changed TS/HTML files must
also pass `oxfmt --check` and ESLint.

### Acceptance state

- Local backend Employee contact/profile gate: PASS, 52/52, including three
  isolated HTTP bearer cases (401/401/200), with zero persistence dependencies.
- Local locality contract gate: PASS, 9/9 applicable tests; one historical
  fixture-dependent case remains BLOCKED and excluded as documented.
- Protected tenant mocked gate: PASS, 14/14 before and after.
- Protected tenant PostgreSQL rollback probe: PASS, 2/2, no retained test tenant.
- Protected Role backend/interceptor gate: PASS, 9/9 backend and 69/69 Angular.
- Affected Angular gate: PASS, 66/66; format and ESLint PASS.
- Angular production build: PASS.
- EmployeeContact schema migration and UAE seed: APPLIED locally; second seed
  run inserted zero rows and non-AE row fingerprints stayed unchanged.
- Real Employee-create/contact edit/add/read-back and UAE repository cascade:
  PASS, 2/2 database probes; test graph rolled back with no retained account.
- Running-product authenticated employee/browser acceptance: PENDING.
- Deployed API/UI acceptance: PENDING.

## LOCK-EMP-EDU-004: Employee Education create date and score-type mapping

### Locked behavior

- `POST /api/Employee/Education/create` retains its existing authenticated Employee permission pipeline and multipart FormData contract.
- Nullable request `DateTime` start/end values map to `EmployeeEducation` nullable `DateOnly` values without changing the submitted calendar date.
- The existing numeric-string score-type contract maps to the nullable integer persistence column. A missing, non-numeric or non-positive score type returns validation failure rather than an unexpected mapping exception.
- Encoded EmployeeId handling, transaction boundaries, optional document upload, projection and repository behavior remain unchanged.

### Protected areas

- `skolenza.application/Mappings/MappingProfile.cs`, `CreateEducationRequestDTO -> EmployeeEducation` mapping.
- `skolenza.application/Features/EmployeeCmd/EducationInfo/Handlers/CreateEducationInfoCommandHandler.cs`.
- `skolenza.automationtests/Unit/EmployeeProfileCharacterizationTests.cs`, Education create mapping case.
- `POST /api/Employee/Education/create` FormData contract.

### Required backend gate

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "FullyQualifiedName~Education_create_mapping_accepts_the_form_contract" --logger "console;verbosity=minimal"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "TestCategory=EmployeeContactRelation|FullyQualifiedName~EmployeeProfileCharacterizationTests" --logger "console;verbosity=minimal"
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
```

Expected baseline: focused mapping 1/1 and protected Employee profile/contact 52/52 pass with zero failed/skipped; Release build succeeds with zero errors.

### Acceptance state

- Local focused mapping: PASS, 1/1.
- Local protected Employee profile/contact gate: PASS, 52/52.
- Local Release build: PASS, zero errors; existing warnings remain.
- Authenticated running-product create/read-back and persistence reconciliation: PENDING.
- Deployed API/UI acceptance: PENDING.

## LOCK-EMP-BANK-005: Employee Bank sensitive fields encrypted at rest

### Locked behavior

- Bank Create and Update encrypt `AccountNumber`, `IFSCCode` and non-empty `UPIId` with the existing `IEncryptionService` and authenticated Tenant encryption key before persistence.
- Bank Get decrypts these values only inside the authenticated Tenant request before returning them over HTTPS, preserving the existing Angular mask/reveal and Edit contract.
- Delete, verification and edit-status operations preserve ciphertext and retain the existing permission and soft-delete flows.
- `EmployeeBankDetail` column capacities remain `AccountNumber varchar(128)`, `IFSCCode varchar(128)` and `UPIId varchar(512)` in EF, the schema reference and Local/Render PostgreSQL.
- Existing valid plaintext rows are migrated once; rerunning the migration validates ciphertext and never double-encrypts it.

### Protected areas

- Bank Create/Get/Update handlers under `skolenza.application/Features/EmployeeCmd/BankInfo/Handlers`.
- `ProjectionHelper.ToGetBankResponseDTOs`.
- `EmployeeBankDetail` EF mapping and `database-scripts/EncryptEmployeeBankSensitiveFields.sql`.
- `EmployeeBankEncryptionTests` and the Bank endpoints documented in `docs/UIdeveloperDoc/EMPLOYEE_PROFILE_CRUD_old.md`.

### Required backend gates

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "TestCategory=EmployeeBankEncryption&TestCategory!=EmployeeBankEncryptionDatabase" --logger "console;verbosity=minimal"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "TestCategory=EmployeeContactRelation|FullyQualifiedName~EmployeeProfileCharacterizationTests" --logger "console;verbosity=minimal"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "FullyQualifiedName~LocalityRefactorTests&FullyQualifiedName!~Four_country_postal_seed_is_idempotent_and_populates_locality_postal_code" --logger "console;verbosity=minimal"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "FullyQualifiedName~HostApiRegressionTests.Tenant_creation_awaits_dependencies_and_preserves_transaction_outcome" --logger "console;verbosity=minimal"
$env:SKOLENZA_HOST_DB_SETTINGS=(Resolve-Path '.\skolenza.api\appsettings.Development.json').Path
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "FullyQualifiedName~HostApiRegressionTests.Tenant_creation_real_database_rollback_probe" --logger "console;verbosity=minimal"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "TestCategory=RoleTypeMapping" --logger "console;verbosity=minimal"
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
```

For an explicitly approved database target, set `SKOLENZA_BANK_DB_SETTINGS` to its settings file and run `TestCategory=EmployeeBankEncryptionDatabase`. The test widens the schema, migrates valid plaintext, validates existing ciphertext and commits atomically.

Expected baseline: Bank unit 1/1, Employee profile/contact 52/52, locality 9/9, Tenant 14/14 and Role 20/20 pass with zero failed/skipped; Release build succeeds with zero errors. The protected Tenant rollback probe remains 2/2 for persistence changes.

### Acceptance state

- Bank unit/decryption projection: PASS, 1/1.
- Local PostgreSQL schema/migration: PASS, 1/1; zero existing rows.
- Render PostgreSQL schema/migration: PASS, 1/1; two existing rows encrypted and verified; idempotency rerun PASS.
- Protected backend gates and Release build: PASS.
- Corrected API deployment and authenticated HTTP CRUD/read-back: PENDING. The database migration alone is not deployed API acceptance.

## LOCK-POLICY-ASSIGN-006: Applicability-safe employee policy assignment

### Locked behavior

- Candidate rows are active tenant employees included by the selected Published version's
  effective Applicability; Exclude wins an equal specificity/priority tie.
- Rows expose encoded employee ID plus employee code/type/department/designation.
- Direct and durable bulk assignment repeat the same applicability decision at write time.
- Existing durable Policy Assignment import remains the single 100+ row workflow.
- Mapping export uses the existing dynamic Policy Assignments permission pipeline.

### Protected areas and gate

- TenantPolicy assignment/candidate/export routes, GenericPolicy handlers/repository/permission
  behavior, Policy Assignment bulk validation and the UI handoff.

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "FullyQualifiedName~GenericPolicyApiContractTests|FullyQualifiedName~PolicyFrameworkSchemaTests|TestCategory=GenericPolicyRuleMetadata" --logger "console;verbosity=minimal"
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
```

Expected baseline: 32 passed, 0 failed/skipped; Release build zero errors. Local contract
acceptance passes. Deployed authenticated candidate/bulk/export acceptance remains pending.

## LOCK-ROLE-PERSONA-007: Professional Institution role-type personas

### Locked behavior

- Persisted RoleType values 1 through 4 remain stable; Student and Parent / Guardian
  are additive values 5 and 6.
- Their user-facing names are Institution Administrator, Staff Member, Staff Manager,
  External User, Student and Parent / Guardian.
- Each option returns its centralized, non-empty description from `AppConstants.cs`.
- Institution registration creates only the four operational defaults: Institution
  Administrator, Staff Member, Staff Manager and External User. Student and Parent /
  Guardian stay available in the central catalogue without being auto-created before
  their modules and permission model exist.
- The four registration-created operational roles and institution-created custom roles
  currently persist `IsSystemDefault=false`. Initial-user and entitlement synchronization
  resolve the Institution Administrator role using the same false-flag convention.
- Default Role remarks are resolved by stable RoleType. New tenant roles persist the specific remark, while a legacy shared generated remark is upgraded in API responses without replacing a user-authored remark.
- Role/list/login mappings resolve the same professional display names; unsupported values remain Unknown.
- Existing Role IDs, UserRole foreign keys and persisted RoleType values remain unchanged.
- API options, create/update validation, bulk validation and registration defaults
  consume the single `InstitutionRoleTypes` catalogue. Angular DDL consumers use the API.
- `GET /api/Role/type-options` keeps bearer authentication and its existing narrow no-ModuleId/OperationId lookup behavior. Other Role requests retain the permission pipeline.

### Protected areas and gate

- Tenant Role Types region in `AppConstants.cs`, Role mapping profile, options handler, permission behavior and controller route.
- Persisted numeric RoleType values and registration Role names.

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "TestCategory=RoleTypeMapping" --logger "console;verbosity=minimal"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "FullyQualifiedName~HostApiRegressionTests.Tenant_creation_awaits_dependencies_and_preserves_transaction_outcome" --logger "console;verbosity=minimal"
```

Expected: Role 26/26 and institution registration 14/14 pass, with no failures or
skips. Current evidence: [2026-10-12](docs/testing/role/institution-persona-catalogue/2026-10-12.md).
Database migration and deployed API/UI acceptance are recorded separately.

## LOCK-TENANT-LOCATION-008: Initial TenantLocation lifecycle

### Locked behavior

- Self registration creates one minimum valid initial `TenantLocation` in the same transaction as the `Tenant`.
- The self-registration location belongs to the new Tenant and uses the requested Tenant country. Required technical fields use centralized defaults: code `PRIMARY`, the company name, Head Office type and UTC time zone. Optional address and subordinate geography remain empty.
- Host onboarding creates the initial location from the supplied `InitialLocation` values in the same transaction as the Tenant.
- The Host aggregate update synchronizes the UI-selected location. An omitted location country inherits the final Tenant country; changing country clears incompatible state, district and locality values unless replacements are supplied.
- The simple Host update synchronizes the canonical active location (Head Office first, then the oldest row). It creates a minimum valid row for a legacy Tenant that has none, changes its country with the Tenant, clears subordinate geography on a country change and updates only a generated company-based location name.
- Tenant creation rollback includes `TenantLocation`; no partial Tenant or location remains when a later onboarding stage fails.
- Existing authorization and permission middleware remain authoritative for Host routes. Self registration remains anonymous as before.

### Protected areas and gate

- Tenant registration handler, Host tenant update handlers, Tenant configuration repository canonical-location lookup, centralized initial-location constants and Tenant controller contracts.

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --filter "FullyQualifiedName~HostApiRegressionTests.Tenant_creation_awaits_dependencies_and_preserves_transaction_outcome|FullyQualifiedName~HostApiRegressionTests.Host_tenant_update_synchronizes_the_canonical_location" --logger "console;verbosity=minimal"
$env:SKOLENZA_HOST_DB_SETTINGS="<authorized local settings file>"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "FullyQualifiedName~HostApiRegressionTests.Tenant_creation_real_database_rollback_probe" --logger "console;verbosity=minimal"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "TestCategory=RoleTypeMapping" --logger "console;verbosity=minimal"
```

Expected: Tenant creation and Host update 15/15, PostgreSQL rollback 2/2 and Role mapping 15/15 pass, with zero failures or skips. Evidence: [2026-10-04](docs/testing/tenant/initial-location-lifecycle/2026-10-04_old.md). Corrected API deployment and live authenticated reconciliation remain pending.

## LOCK-POLICY-DOWNLOAD-009: Policy Assignment Download mapping

### Locked behavior

- `TENANT_POLICY_ASSIGNMENTS` reuses the established `Download` operation.
- Exactly one active and operational catalogue mapping exists with priority `60`.
- Both focused and consolidated production seeds contain the idempotent mapping row.
- Role grants continue through the existing permission pipeline; this catalogue mapping does not bypass it.

### Protected areas and gate

- `Module`, `Operation`, and `ModuleOperationMapping` catalogue rows for Policy Assignments.
- `SeedTenantPolicyModules.sql`, consolidated production module-operation seed, and policy schema contracts.

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "FullyQualifiedName~GenericPolicyApiContractTests|FullyQualifiedName~PolicyFrameworkSchemaTests|TestCategory=GenericPolicyRuleMetadata" --logger "console;verbosity=minimal"
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
```

Expected: policy contract/schema 33/33 and Release build pass with zero errors. Local and Render each retain exactly one active and operational mapping. Evidence: [2026-10-07](docs/testing/policy/assignment-download-operation/2026-10-07_old.md).

## LOCK-POLICY-PUBLISH-010: Policy publication integrity

### Locked behavior

- The Policy list action is labelled `Manage version`; the route remains the exact version view.
- Documents can be added or removed only while a version is Draft or Rejected.
- Submit and Publish require at least one active employee-visible document whose stable type code is `POLICY_DOCUMENT`.
- Approval stores a SHA-256 content fingerprint covering policy/version fields, rules, applicability, Attendance configuration and active document checksum metadata. Publish recomputes it and rejects changed or unapproved content.
- Published and Archived versions remain immutable.
- Clone creates the next Draft and copies rules, applicability and Attendance configuration. It does not copy documents, assignments, acknowledgements, exceptions or approval history.
- Archive preserves assignment rows. Assignment reads expose `IsEffective=false` and `StateReason=VERSION_ARCHIVED` for active rows belonging to an Archived version.

### Protected areas and gate

- Generic Policy entities, EF mapping, repository lifecycle/document/assignment methods, Policy DTOs and `AddPolicyPublicationIntegrity.sql`.
- Angular Policy Definitions action copy and Policy Version document/assignment state.

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "FullyQualifiedName~GenericPolicyApiContractTests|FullyQualifiedName~PolicyFrameworkSchemaTests|TestCategory=GenericPolicyRuleMetadata" --logger "console;verbosity=minimal"
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
```

Expected: 37 passed, zero failed/skipped; Release build zero errors. Angular changed-file format and production build must pass when Angular work is authorized. Local and Render schemas must each have the two checksum columns, two check constraints and zero null approval-history checksums. Deployed authenticated lifecycle and storage verification remains pending. Evidence: [2026-10-07](docs/testing/policy/publication-integrity/2026-10-07_old.md).

## LOCK-POLICY-LEAVE-011: Leave Type targeting

### Locked behavior

- A Leave-category policy version selects active tenant Leave Types through `PolicyVersionLeaveType`.
- Every Leave rule and applicability row targets one or more selected Leave Types through normalized mappings.
- Non-Leave policies reject Leave Type targets; tenant ownership and active-state validation are mandatory.
- Leave-category rule types allow multiple rows so each leave component can have independent entitlement, accrual, carry-forward, limit and approval behavior.
- Clone preserves all Leave Type mappings, Draft replacement rewrites them transactionally, and approval fingerprints include them.
- Resolve accepts optional `LeaveTypeId` and returns only the covered version, matching applicability and targeted rules.

### Protected areas and gate

- Generic Policy entities, DTOs, repository, EF configuration and lookup handler.
- `AddPolicyLeaveTypeTargeting.sql`, `CreateGenericTenantPolicyFramework.sql` and Leave rule metadata.
- Angular Policy Definition model, contract, schema and form controls.

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "FullyQualifiedName~GenericPolicyApiContractTests|FullyQualifiedName~PolicyFrameworkSchemaTests|TestCategory=GenericPolicyRuleMetadata" --logger "console;verbosity=minimal"
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
```

Expected: at least 40 passed with zero failed/skipped and Release build zero errors. When Angular is authorized, the focused Policy Definition tests and production build must pass. Local and Render must each expose all three mapping tables, all three unique constraints, eight active seeded Leave Types and seven active Leave-category mappings with `AllowMultiple=true`. Evidence: [2026-10-08](docs/testing/policy/leave-type-targeting/2026-10-08_old.md).

## LOCK-POLICY-DRAFT-EDIT-012: Exact Draft version edit

### Locked behavior

- Edit uses the policy list row's `currentVersionId` and the existing version-specific GET route.
- A newer Draft opens even when an older Published version is marked current.
- Published version immutability remains unchanged.

### Protected areas and gate

- Angular Policy Definitions list, Policy Definition form/store, and Tenant Policy API client.

```powershell
bun run test -- --include src/app/features/tenant-policies/policy-definitions/policy-definition-form/policy-definition-form.spec.ts
bun run build -- --configuration production
```

Expected: at least 8 passed with zero failed/skipped and production build succeeds. Evidence:
[2026-10-08](docs/testing/policy/draft-version-edit/2026-10-08_old.md).

## LOCK-POLICY-ACCRUAL-013: Leave annual entitlement calculation

### Locked behavior

- Each Leave Accrual rule stores a required positive `annualEntitlement` as its authoritative yearly total.
- MONTHLY, QUARTERLY and YEARLY derive `amountPerCycle` using 12, 4 and 1 cycle respectively,
  rounded to six decimal places away from zero.
- Angular recalculates the amount when entitlement or frequency changes and renders it read-only.
- Create and Draft-update APIs independently recompute the amount and reject a conflicting value.
- The consuming leave ledger must credit the remaining balance in the final cycle so rounding never
  changes the exact annual entitlement.
- Leave rule targeting, Published immutability and the existing permission pipeline remain unchanged.

### Protected areas and gate

- Accrual setting rows in `AddGenericPolicyRuleMetadata.sql`.
- `PolicyAccrualCalculationHelper` and Generic Policy save validation.
- Angular Policy Definition rule settings and shared numeric input read-only support.

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "FullyQualifiedName~GenericPolicyApiContractTests|FullyQualifiedName~PolicyFrameworkSchemaTests|TestCategory=GenericPolicyRuleMetadata" --logger "console;verbosity=minimal"
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
bun run test -- --include src/app/features/tenant-policies/policy-definitions/policy-definition-form/policy-definition-form.spec.ts --include src/app/features/tenant-policies/policy-definitions/policy-definition-form/rule-settings.spec.ts
bun run build -- --configuration production
```

Expected: backend at least 47 passed with zero failed/skipped; Angular at least 27 passed with zero
failed/skipped; both builds succeed. Local and Render each expose one active required
`annualEntitlement` Accrual definition before `amountPerCycle`.

Evidence: [2026-10-08](docs/testing/policy/annual-entitlement-accrual/2026-10-08_old.md).

## LOCK-POLICY-IDENTITY-014: Tenant policy-name uniqueness

### Locked behavior

- One Policy identity owns its versions; `(PolicyId, VersionNumber)` remains unique.
- Active, non-deleted Policy names are unique per tenant after trimming and case normalization.
- Create and Draft update reject a duplicate name with an actionable Conflict that directs the
  user to open the existing Policy and create its next version.
- Policy Definition bulk preview reports a different-code/same-name conflict before confirmation.
- The database migration refuses to create its unique index while historical duplicates exist;
  it never guesses which Policy identities should be merged.

### Protected areas and gate

- Generic Policy create/update repository methods, Policy Definition bulk preview, and
  `EnforceTenantPolicyUniqueName.sql`.

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "FullyQualifiedName~GenericPolicyApiContractTests|FullyQualifiedName~PolicyFrameworkSchemaTests|TestCategory=GenericPolicyRuleMetadata" --logger "console;verbosity=minimal"
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
```

Expected: at least 48 passed with zero failures/skips and Release build with zero errors. Local
Local and Render PostgreSQL expose `UX_Policy_Tenant_NormalizedName`. The corrected Render API
deployment remains pending. Evidence:
[2026-10-09](docs/testing/policy/policy-name-uniqueness/2026-10-09_old.md).

## LOCK-POLICY-DRAFT-SAVE-015: Atomic Draft replacement save

### Locked behavior

- Updating a Draft or Rejected policy version removes its previous rules, applicability rows and
  version Leave Type mappings before inserting the replacement graph.
- The delete flush and replacement inserts remain inside one database transaction, so a later
  validation or persistence failure rolls the whole edit back.
- Version-scoped unique keys such as `(PolicyVersionId, RuleOrder)` and
  `(PolicyVersionId, LeaveTypeId)` cannot collide with rows from the graph being replaced.
- Published and Archived immutability, Leave Type targeting, annual entitlement calculation and
  the existing permission pipeline remain unchanged.

### Protected areas and gate

- `GenericPolicyRepository.UpdateDraftAsync` replacement ordering.
- Generic Policy contract, schema and rule metadata tests.

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "FullyQualifiedName~GenericPolicyApiContractTests|FullyQualifiedName~PolicyFrameworkSchemaTests|TestCategory=GenericPolicyRuleMetadata" --logger "console;verbosity=minimal"
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
```

Expected: at least 49 passed with zero failed/skipped and Release build with zero errors.
Evidence: [2026-10-09](docs/testing/policy/draft-replacement-save/2026-10-09_old.md).
Local source/build verification passes. The authenticated Render save remains pending until the
corrected API build is deployed.

## LOCK-EMP-PROFILE-COMPLETION-016: Mandatory-aware profile completion consistency

### Locked behavior

- Employee list completion uses the same rounded mean of profile/configuration sections as `GET /api/Employee/get-all-percentage`; it is not Overview-only.
- An empty supported section is zero percent.
- A Bank section with saved rows but no Primary account is zero percent. Once a Primary exists, every saved Bank row contributes to the section average, and the Primary row requires its cancelled-cheque upload and file metadata.
- Identity applicability comes from active `CountryIdentityRule` rows for the employee's persisted country; country names and document names are not hardcoded.
- A mandatory Identity rule participates even without a saved row. An optional rule participates after an identity is saved. Every participating Identity row requires both a non-blank value and an uploaded document.
- Verification/edit flags remain workflow metadata and never add completion percentage.
- Existing Employee authentication, data-access checks, permission pipeline, encoded IDs, CRUD contracts and Bank encryption remain unchanged.

### Protected areas and gate

- `EmployeeProfileCompletionCalculator`, Identity projection and profile-status handler.
- Employee list/profile completion query in `BaseEmployeeRepository`.
- Bank list completion in `EmployeeBankRepository`.
- `EmployeeProfileCharacterizationTests` and related protected Employee/Bank gates.

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-restore --filter "TestCategory=EmployeeContactRelation|FullyQualifiedName~EmployeeProfileCharacterizationTests" --logger "console;verbosity=minimal"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "TestCategory=EmployeeBankEncryption&TestCategory!=EmployeeBankEncryptionDatabase" --logger "console;verbosity=minimal"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "FullyQualifiedName~LocalityRefactorTests&FullyQualifiedName!~Four_country_postal_seed_is_idempotent_and_populates_locality_postal_code" --logger "console;verbosity=minimal"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "FullyQualifiedName~HostApiRegressionTests.Tenant_creation_awaits_dependencies_and_preserves_transaction_outcome" --logger "console;verbosity=minimal"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --filter "TestCategory=RoleTypeMapping" --logger "console;verbosity=minimal"
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
```

Expected: Employee/contact 52/52, Bank 1/1, locality 9/9, tenant 14/14, PostgreSQL rollback 2/2 and Role 20/20 pass with zero failures/skips; Release build succeeds with zero errors. The historical Employee/contact count was 46 before the legacy Insurance test case was removed in committed change `a09acbe3`; the immediately preceding active baseline was 45/45 and seven completion cases were added by this lock.

### Acceptance state

- Local focused and protected tests: PASS with the counts above.
- Local PostgreSQL tenant rollback probe: PASS, 2/2; transactions rolled back.
- Local Release build: PASS, zero errors; existing dependency/compiler warnings remain.
- Database data/schema changes: NOT REQUIRED and not performed.
- Authenticated running-product and deployed API/browser reconciliation: PENDING.

Evidence: [2026-10-10](docs/testing/employee/profile-completion-consistency/2026-10-10_old.md).

## LOCK-STAFF-PROFESSIONAL-MODEL-017: Runtime-valid professional master mapping

### Locked behavior

- `InstitutionMasterEntity` is the root of the TPC hierarchy and owns the inherited primary-key
  and common-property configuration.
- `StaffJobFamilyMaster`, `StaffCapabilityMaster`, `StaffDutyDefinitionMaster` and
  `StaffPositionMaster` map to their own `skolenza` tables; derived types must not configure the
  inherited key again.
- A successful compile is not acceptance for persistence changes. The EF model must be constructed,
  the API must start against local PostgreSQL, and an HTTP probe must reach Swagger before the
  behavior is reported as runtime-valid.

### Protected areas and gate

- `StaffProfessionalStructure` inheritance and `SkolenzaDbContext` professional master mappings.
- Staff professional structure persistence-model regression test.

```powershell
dotnet test .\Skolenza.automationtests\Skolenza.automationtests.csproj -c Release --no-restore --filter "TestCategory=StaffProfessionalStructure" --logger "console;verbosity=minimal"
dotnet run --project .\Skolenza.api\Skolenza.api.csproj -c Release --no-build --launch-profile http
```

Expected: 4/4 tests pass; API logs `SKOLENZA APPLICATION STARTED SUCCESSFULLY`, executes a local
PostgreSQL command, Swagger HTML and OpenAPI JSON return HTTP 200, and an unauthenticated protected
professional-structure endpoint returns HTTP 401 rather than a startup/model exception. Evidence:
[2026-10-12](docs/testing/staff/professional-structure/2026-10-12.md).

## LOCK-STAFF-ACADEMIC-018: Academic structure and teaching assignments

### Locked behavior

- School, College and Coaching Center use the same institution-owned Session, Programme,
  Group, Section, Subject and Course Offering hierarchy.
- `TeachingAssignmentTypeMaster` is the authoritative teaching-role catalogue. UI consumers
  resolve it through the authenticated API and do not hardcode IDs or labels.
- Teaching and class-teacher responsibilities are effective-dated, tenant-isolated assignments.
  Overlapping duplicate teaching allocation and overlapping primary class-teacher responsibility
  are rejected with Conflict.
- Catalogue parents, campuses, Staff members, offerings and sections must belong to the same
  institution. Existing permission middleware remains authoritative.
- Staff Overview reads the persisted canonical assignments and their resolved catalogue names.

### Protected areas and gate

- Academic master and assignment entities, EF mappings, migration and rollback scripts.
- `StaffAcademicStructureRepository`, DTOs, handlers and controller.
- Angular academic structure API, full-page management UI, permission-route mapping and Staff
  Overview assignment display.

```powershell
dotnet test .\Skolenza.automationtests\Skolenza.automationtests.csproj -c Release --no-restore --filter "TestCategory=StaffAcademicStructure|TestCategory=StaffProfessionalStructure|TestCategory=StaffCategoryCatalogue" --logger "console;verbosity=minimal"
$env:SKOLENZA_HOST_DB_SETTINGS=(Resolve-Path '.\Skolenza.api\appsettings.Development.json').Path
dotnet test .\Skolenza.automationtests\Skolenza.automationtests.csproj -c Release --no-build --filter "FullyQualifiedName~HostApiRegressionTests.Institution_creation_real_database_rollback_probe" --logger "console;verbosity=minimal"
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
bun run build -- --configuration production
```

Expected: 12 focused Staff tests and 2 rollback probes pass with zero failures/skips; API Release
and Angular production builds succeed; API starts against PostgreSQL. Authenticated local CRUD,
read-back, HTTP 409 overlap and cross-institution denial remain part of the acceptance baseline.
Evidence: [2026-10-13](docs/testing/staff/academic-structure/2026-10-13.md). Render DB and API
deployment passed; authenticated deployed browser acceptance remains separate.

## Adding the next lock

Append the next entry; do not rewrite or erase historical locked evidence. If a
new authorized requirement intentionally conflicts with a lock, mark the old
entry `SUPERSEDED` with a link to the user's decision and the replacement lock.
Keep the earlier report available.

## Skolenza documentation lifecycle

- Files ending in _old.md are read-only AxionPro reference material. Do not update them for Skolenza implementation work.
- Record every new decision, implementation status and test result only in current Skolenza Markdown files without the _old suffix.
- If legacy behavior is reused, cite the _old.md source and write the accepted Skolenza decision in a current document.




## LOCK-STAFF-INSTITUTION-CATEGORY-HIERARCHY-019: Institution-owned professional hierarchy

### Locked behavior

- `StaffCategory` remains the central platform semantic catalogue; institutions manage working copies in `InstitutionStaffCategoryMaster`.
- Active `StaffCategoryJobFamily` mappings are the authoritative Category to Job Family hierarchy. A Position and StaffMember use an institution-owned category and retain the canonical type only for shared semantics.
- Position writes reject cross-tenant, inactive or unmapped Category/Job Family combinations. Position capability and duty mappings preserve required versus optional status.
- Deactivation is tenant-isolated and dependency-safe. Records remain available for reactivation; business history is not soft-deleted by the UI deactivate action.
- Staff create and official update accept `InstitutionStaffCategoryMasterId`; the API derives `StaffCategoryId` and validates the selected Position against the same institution category.

### Protected areas and gate

- Institution category, Category/Job Family mapping, Position and StaffMember entities and EF configuration.
- Professional structure repository, handlers, controller, migration/rollback and Angular Staff Professional Structure/create/edit flows.

```powershell
dotnet test .\skolenza.automationtests\Skolenza.automationtests.csproj -c Release --no-restore --filter "TestCategory=StaffAcademicStructure|TestCategory=StaffProfessionalStructure|TestCategory=StaffCategoryCatalogue" --logger "console;verbosity=minimal"
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
bunx ng build --no-progress
```

Expected: 13 focused Staff tests pass with zero failures/skips; backend Release and Angular production builds succeed. The idempotent migration must leave zero institution-owned active Position or categorized StaffMember rows without `InstitutionStaffCategoryMasterId` in both local and Render databases. Evidence: [2026-10-13](docs/testing/staff/professional-category-hierarchy/2026-10-13.md).

## LOCK-S3-STORAGE-PREFIX-020: External object-storage namespace

### Locked behavior

- Credentials remain outside tracked configuration and are resolved through the established .NET configuration pipeline.
- `AWSUploads:BasePath` is the central object-key namespace. Every consumer of `IFileStorageService` receives the same prefix without duplicating it in policy, staff, bank, education, identity or asset handlers.
- Existing caller-provided folder structure is preserved beneath the configured prefix. Stored keys and signed URLs use that complete key.
- Storage acceptance requires a real upload, signed download with byte comparison and cleanup delete; configuration or compilation alone is not acceptance.

### Protected areas and gate

- `FileStorageService` key construction and `AWSUploads` configuration.
- Existing `IFileStorageService` callers and their folder conventions.

```powershell
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
```

Expected: Release build succeeds with zero errors. The sanitized development probe uploads below `skolenza/`, downloads the same content through a signed URL and deletes the probe object. Credentials and signed URLs must never be committed or recorded in evidence. Evidence: [2026-10-13](docs/testing/storage/development-s3-prefix/2026-10-13.md).

## LOCK-HOST-STAFF-CATEGORY-CATALOGUE-021: Host-owned Staff Category catalogue

### Locked behavior

- Host administrators manage the central Staff Category catalogue through the existing Host permission pipeline and Host-only module scope (`ModuleScope=2`).
- Institution administrators consume active central categories and configure institution-owned category mappings, Job Families, Capabilities, Duties and Positions using the existing tenant-scoped professional-structure API and UI.
- List, create and update authorization resolves through existing module operations (`View`, `Add`, `Update`); no permission bypass or consumer-side hardcoded catalogue is introduced.
- The central catalogue seed is repeatable and preserves existing `StaffCategory` rows while enabling generated IDs for future API creates.
- Removing a central category from use is deactivation, not cross-tenant destructive deletion; institution configuration and history remain tenant-owned.

### Protected areas and gate

- Host catalogue seed and canonical production seed runner stage 7.
- Host Staff Category API/controller, handlers, repository, DTOs and authorization behavior.
- Angular Host-only route/menu, `StaffCategoriesAPI`, permission-operation interceptor mapping and Signal Forms catalogue page.
- Host module/operation mappings and Host-Super-Admin grants in local and Render `skolenza` databases.
- Existing institution professional structure and academic staff behavior covered by the combined Staff gates.

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj -c Release --no-build --no-restore --filter "TestCategory=StaffAcademicStructure|TestCategory=StaffProfessionalStructure|TestCategory=StaffCategoryCatalogue" --logger "console;verbosity=minimal"
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
bun run test -- --include src/app/features/host/institution-master-data/staff-category-catalogue.spec.ts --include src/app/core/interceptors/module-operation-routes.spec.ts --watch=false
bun run build -- --configuration production
```

Expected: 14 focused backend Staff tests and 67 Angular component/interceptor tests pass with zero failures or skips; Release API and Angular production builds succeed. Re-running `database-scripts/SeedHostInstitutionMasterData.sql` must keep counts at one Host parent, one child, three active operation mappings and three active Host grants in both local and Render databases.

Evidence: [2026-10-14 scenario](docs/testing/staff/host-staff-category-catalogue/2026-10-14.md).

### Acceptance state

- Local catalogue contract and protected Staff regression tests: PASS, 14/14.
- Angular catalogue and permission-route tests: PASS, 67/67.
- Backend Release build: PASS, zero errors; existing dependency vulnerability warnings remain.
- Angular production build: PASS.
- Local and Render `skolenza` database seed/repeat reconciliation: PASS.
- Local authenticated Host create/list and Institution dependent configuration: PASS.
- Cross-Institution isolation comparison: PENDING.
- Render API route/deployment and authenticated browser acceptance: PENDING; live OpenAPI did not yet expose the new route.

### 2026-10-17 onboarding acceptance addendum

The Host catalogue remains authoritative beyond its original five seed IDs.
An obsolete StaffMember range check was found to reject a valid Host-created
category; the additive migration removes only that range and retains the FK.
The staff insertion response also now safely handles the interval before its
primary Position exists, and resolves the response identity after assignment.

Protected pre-change Staff/real-DB gates passed 17/17; profile/contact/role gates
passed 78/78. Category post-change gates passed 23/23 and the combined affected
suite passed 102/102 with zero failures/skips. New real-DB tests protect extended
category validity/unknown-category rejection and pre-assignment Position reads.
Authenticated local onboarding, named allocation and 0/75/100% progress passed.

The expanded final run passed 108/109 with one old opt-in UAE contact probe
failing at an absent UAE staff-template prerequisite before handler execution.
This broader result is unaccepted, not relabelled as a clean gate. Welcome email,
first login, teacher grants, current UI and Render deployment remain unaccepted.
No Angular work was performed. See the
[current scenario](docs/testing/staff/host-category-onboarding/2026-10-17.md).


## LOCK-CURRICULUM-API-PAGINATION-022: Curriculum collection paging

### Locked behavior

- Frameworks, Units, independently requested Topics, Lesson Plans, Delivery Logs and Syllabus Progress GET collections default to page 1 / 10 records and reuse the existing permission pipeline and root array/totals response envelope.
- Nonpositive paging inputs normalize to the central defaults. Filtered totals, deterministic ordering, last/empty pages and overflow-safe offsets are preserved by `QueryPagination.ToPageAsync`.
- Paged Unit headers omit nested Topics. `/api/Staff/curriculum-delivery/units/{unitId}/topics` pages its institution-owned catalogue independently. Unit save responses retain their existing nested Topics.
- Plan topic selections and Delivery coverage remain complete per returned record. Syllabus progress uses all approved coverage for the paged units, not just the current Delivery page. No staff profile-completion metadata is added.
- List paging must not truncate write/review record responses or weaken parent ownership, soft-delete scope or existing permission checks.

### Regression gate and evidence

```powershell
$env:SKOLENZA_HOST_DB_SETTINGS=(Resolve-Path '.\Skolenza.api\appsettings.Development.json').Path
dotnet test .\Skolenza.automationtests\Skolenza.automationtests.csproj -c Release --no-restore --filter 'TestCategory=StaffAcademicStructure|TestCategory=StaffProfessionalStructure|TestCategory=StaffCategoryCatalogue|TestCategory=StaffCurriculumDelivery|FullyQualifiedName~HostApiRegressionTests.Institution_creation_real_database_rollback_probe' --logger 'console;verbosity=minimal'
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
```

Expected local baseline: 33/33 tests, zero failures/skips; Release build zero errors.
Verify authenticated local page slices, root totals, independent Topics, 401/403/404/400
responses and Swagger. [Scenario evidence](docs/testing/staff/curriculum-pagination/2026-10-17.md).
No Angular changes/build were authorized or performed. UI integration and Render
deployment are pending acceptance, not implied by this local API lock.

## LOCK-PROFESSIONAL-API-PAGINATION-023: Five professional catalogue lists

### Locked behavior

- Institution Job Families, Staff Categories, Capabilities, Duties and Positions
  reuse `QueryPagination.ToPageAsync` and `PaginationDefaults`; omitted or
  nonpositive values resolve to page 1 / 10 records. No duplicate paging helper
  or profile-completion metadata is introduced.
- Tenant, soft-delete, active-only and optional Position category filters apply
  before count/paging. DisplayOrder, Name and Id give a deterministic order.
- Root `data` remains an array with pageNumber, pageSize, totalRecords and
  totalPages metadata. Categories retain complete family mappings/dependency
  counts; Positions retain complete capability/duty requirements and flags.
- Existing save/profile/seed reads and write responses are not limited to page 1.
  The established StaffMember institution permission pipeline remains unchanged.

### Regression gate and evidence

```powershell
$env:SKOLENZA_HOST_DB_SETTINGS=(Resolve-Path '.\Skolenza.api\appsettings.Development.json').Path
dotnet test .\Skolenza.automationtests\Skolenza.automationtests.csproj -c Release --no-restore --filter 'TestCategory=StaffAcademicStructure|TestCategory=StaffProfessionalStructure|TestCategory=StaffCategoryCatalogue|TestCategory=StaffCurriculumDelivery|FullyQualifiedName~HostApiRegressionTests.Institution_creation_real_database_rollback_probe' --logger 'console;verbosity=minimal'
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
dotnet build .\Skolenza.sln -c Debug --no-restore --nologo
```

Expected local baseline: 38/38 tests, zero failures/skips; both builds zero errors.
Authenticated HTTP pages, active/category filters, 401/403/400 errors and OpenAPI
paging parameters are recorded in the [2026-10-18 scenario](docs/testing/staff/professional-pagination/2026-10-18.md).
No Angular work or schema migration was authorized/performed for this API-only
change. UI integration and Render deployment remain unaccepted.


## LOCK-ACADEMIC-API-PAGINATION-024: Academic catalogue paging

### Locked behavior

- Session, Programme, Group, Section, Subject and CourseOffering GET lists use
  the existing central QueryPagination helper and PaginationDefaults. Default
  page/size is 1/10; nonpositive values normalize through that same helper.
- Existing tenant, soft-delete and activeOnly predicates apply before SQL count
  and page retrieval. Name plus unique Id provides stable ordering.
- The existing root data array gains standard pageNumber, pageSize,
  totalRecords and totalPages metadata; no profile-completion flags are added.
- Date/type/reference IDs and names, capacity, level, curriculum version and
  required periods remain complete. Parent name resolution and single-record
  save/profile/seed flows must not be truncated to page 1.
- The existing StaffMember institution permission pipeline, entity mapping,
  teaching allocation and lookup contracts remain unchanged.

### Regression gate and evidence

```powershell
$env:SKOLENZA_HOST_DB_SETTINGS=(Resolve-Path '.\Skolenza.api\appsettings.Development.json').Path
dotnet test .\Skolenza.automationtests\Skolenza.automationtests.csproj -c Release --no-restore --filter 'TestCategory=StaffAcademicStructure|TestCategory=StaffProfessionalStructure|TestCategory=StaffCategoryCatalogue|TestCategory=StaffCurriculumDelivery|FullyQualifiedName~HostApiRegressionTests.Institution_creation_real_database_rollback_probe' --logger 'console;verbosity=minimal'
dotnet build .\Skolenza.sln -c Release --no-restore --nologo
dotnet build .\Skolenza.sln -c Debug --no-restore --nologo
```

Expected local baseline: 41/41, zero failures/skips; Debug/Release zero errors.
All 17 structure/curriculum list URL variants were verified over local HTTP,
including Academic active/custom-size/invalid-input/authentication cases.
[Scenario evidence](docs/testing/staff/academic-pagination/2026-10-18.md) and
[consolidated endpoint index](docs/UIdeveloperDoc/STAFF_PAGINATION_API_INDEX.md).
No Angular work or database migration was authorized/performed. UI integration
and Render deployment remain pending acceptance.

## WIP-STUDENT-PERMISSION-025: Student permission behavior (2026-10-29)

Status: **WIP — not locked until authenticated HTTP acceptance is complete.**

- Admission detail receives dynamic module and operation IDs from the query and
  forwards the existing permission DTO through the Student MediatR behavior.
- The behavior requires `STUDENT_ADMISSIONS` View for detail reads and
  `STUDENT_FEES` Update for Cashfree reconciliation, which can post payment and
  receipt changes. The existing role-permission pipeline remains authoritative.
- Student suite passed 84/84; protected Staff/Academic/Curriculum gate passed
  41/41 before and after, including Local PostgreSQL rollback probes. Release
  build passed with 0 errors and 22 warnings.
- Authenticated Local HTTP, real role grants, Cashfree sandbox and deployed
  acceptance remain pending. See [scenario evidence](docs/testing/student/permission-behavior/2026-10-29.md)
  for exact commands, scope and test limitations.

## WIP-STUDENT-ADMISSION-CONCURRENCY-026: Admission write ordering (2026-10-29)

Status: **WIP — not locked until full approval and authenticated HTTP acceptance.**

- Institution-scoped admission transitions, edits, admission Guardian links,
  admission document version/review persistence and fee quote creation use the
  admission row lock within a transaction before checking current state.
- The disposable Local PostgreSQL suite passed nine cases, including eight
  competing writes and one uncontended repository flow. The full Student suite
  passed 93/93 with that disposable database; the protected gate passed 41/41
  and Release build had 0 errors and 22 warnings.
- The database cases are opt-in and skip without a guarded disposable database.
  Full approval/enrollment/invitation rollback and authenticated, provider and
  deployed acceptance remain pending. Exact commands and limits are in the
  [concurrency scenario](docs/testing/student/admission-concurrency/2026-10-29.md).

## WIP-STUDENT-APPROVAL-027: Approval atomicity and Guardian dates (2026-10-29)

Status: **WIP — real invitation and authenticated/deployed acceptance pending.**
Approval requires an effective, unexpired primary Guardian. A disposable Local
PostgreSQL fixture verifies Student/enrollment/Guardian creation and rollback on
mocked invitation failure, plus rejection of future Guardian links. The full
Student suite passed 96/96 with disposable DB. The exact scope and remaining
checks are in [scenario evidence](docs/testing/student/admission-approval-atomicity/2026-10-29.md).
Protected gate passed 41/41 before and after; Release and Debug builds passed
with 22 and 583 warnings respectively. Local now has an invitation template but
no verified portal domain. Render had neither at its last read-only check.
Real approval remains blocked until the relevant school domain is DNS-verified.

## WIP-STUDENT-INVITATION-028: Local invitation setup (2026-10-29)

Status: **WIP — real domain, authenticated HTTP and email delivery pending.**
The Local host invitation template was seeded idempotently without changing
existing templates. A disposable database test verified approval with the real
queue repository and synthetic verified domain; Student tests passed 97/97.
The protected Staff gate passed 41/41 before and after; Release build passed.
No Render data was modified. See [scenario evidence](docs/testing/student/guardian-invitation-local/2026-10-29.md).

## WIP-STUDENT-EMAIL-PROTECTION-029: Portal email queue protection (2026-10-29)

Status: **WIP — real DNS, SMTP and key rotation acceptance remain pending.**
New invitation and reset messages protect queue placeholders using authenticated
institution-key encryption. The worker decrypts new messages while preserving
legacy queue compatibility. Local invitation and reset templates are active;
no Render changes were made. Student tests passed 102/102 with a disposable DB;
the final combined protected/security gate passed 50/50 and Release build succeeded.
Historical key rotation and any legacy plaintext queue data elsewhere require
review. See [scenario evidence](docs/testing/student/portal-email-encryption/2026-10-29.md).

## WIP-STUDENT-KEY-ROTATION-030: Shared-key retention investigation (2026-10-29)

2026-10-30 characterization addendum: four synthetic tests verify that current Student AES-GCM envelopes need their original key, legacy Bank CBC has no authentication, numeric IDs currently use a global salt, and per-process key caches do not invalidate each other. Protected baseline 104/104 and 41/41 before, 108/108 including the characterization tests and 41/41 after. No rotation implementation, actual key change or database migration was made. See [scenario](docs/testing/student/portal-email-key-rotation/2026-10-30.md).

Status: **WIP — Local shared-key design approved; implementation and acceptance pending.**
A disposable-database Student suite failed 1/106 because the shared key table
has a non-conditional unique InstitutionId constraint. The experimental
inactive-key fallback was removed. After revert, the protected/security gate
passed 50/50 and Release build succeeded. No Local or Render key schema or data
was changed. Current Skolenza Bank 1/1 and profile/contact 52/52 selectors
passed and are documented in the selector-correction addendum above. No key
rotation implementation has been accepted. See
[investigation](docs/testing/student/portal-email-key-rotation/2026-10-29.md).

## WIP-STUDENT-RESET-031: Outstanding password-reset tokens (2026-10-29)

Status: **WIP — forced-interleaving, authenticated HTTP and deployed acceptance pending.**
Password reset completion now consumes other outstanding password-reset tokens
for the same institution and account in the password-change transaction. The
disposable Local database reproduced the old failure and the Student suite passed
102/102 after the change. Protected staff/curriculum and Local rollback passed
41/41; Bank/contact/profile/role passed 79/79. Parallel repository attempts
and same-institution cross-account token isolation passed in a disposable DB;
forced interleavings and serialization-conflict HTTP mapping remain pending. The sequential Release build
passed. No Local application data, Render data or Angular was changed.
[Scenario evidence](docs/testing/student/password-reset-token-revocation/2026-10-29.md).

2026-10-30 addendum: a forced PostgreSQL account-lock race previously produced
200/500. The repository now maps the specific serialization failure, including
its EF wrapper, through the existing conflict pipeline. Disposable DB and
in-process middleware tests verified 200/409 with a safe response; Student
102/102, Staff/Curriculum with Local rollback 41/41 and Bank/profile/role 79/79
passed. Release build had 0 errors and 22 warnings. Full running-product HTTP
and deployed verification remain pending. [Evidence](docs/testing/student/password-reset-token-revocation/2026-10-30.md).

## WIP-STUDENT-LOGIN-032: Portal failed-login lockout (2026-10-30)

Status: **WIP — actual HTTP and deployed acceptance pending.** The portal login
handler records a failure when an active verified account has no lockout end,
as well as when a previous lockout has expired. A disposable PostgreSQL test
verified the configured five-failure lockout. Student 106/106, protected
Staff/Curriculum 41/41 and Bank/contact/profile/role 79/79 passed. No Angular,
Local application data or Render data changed. [Evidence](docs/testing/student/portal-login-lockout/2026-10-30.md).

2026-10-30 addendum: an isolated Local HTTP harness with synthetic domain and
real portal controller, handlers, password hasher, JWT signer and policy passed
Guardian login, profile, refresh/replay, logout/revocation and invalid-host
checks. Student tests passed 106/106, protected Staff/Curriculum 41/41 and
Bank/profile/role 79/79; Release build 0 errors/22 warnings. Full production
startup, live DNS, SMTP, Staff permission and deployment acceptance remain
pending. [Evidence](docs/testing/student/portal-auth-http/2026-10-30.md).

## WIP-STUDENT-RENDER-TEMPLATES-033: Portal template parity (2026-10-30)

Status: **WIP — live DNS, delivery, authenticated HTTP and deployment pending.**
Student-related schema comparison found 1,198 matching catalog entries in Local
and Render. Following a fresh Render backup, verified full Local restore and
two-pass idempotency dry run, the existing invitation and password-reset template
seeds were applied to Render. Both templates are active and their content matches
Local. A disposable restore of the fresh Render backup passed Student 106/106;
protected Staff/Curriculum 41/41 and Bank/profile/role 79/79 passed before and
after. No role grants, domain verification or messages were created. See
[scenario evidence](docs/testing/student/render-template-schema-sync/2026-10-30.md).
# WIP-STUDENT-FEE-034: Fee payment database concurrency and timestamps

Status: **WIP — authenticated HTTP, provider and deployed acceptance pending**. Local PostgreSQL forced concurrency verifies same-key deduplication and prevents competing payments from exceeding an installment balance. Payment UTC timestamps use the existing timestamp-without-time-zone column and PostgreSQL microsecond precision. Existing Staff/Curriculum gate passed 41/41 and Bank/Profile/Role 79/79 before and after; see [fee payment evidence](docs/testing/student/fee-payment-database/2026-10-30.md). This does not authorize changing any earlier locked behavior or claim complete Student acceptance.

## WIP-STUDENT-EXIT-035: Student exit, Guardian and identifier persistence

Status: **WIP — authenticated HTTP and deployed acceptance pending**. Local PostgreSQL forced concurrency verified one exit/history and one primary Guardian, and denied Guardian linking after a competing exit. Synthetic identifier persistence, masking and verification reset passed after UTC timestamp mapping correction. Student suite 106/106, protected Staff/Curriculum 41/41 and Bank/Profile/Role 79/79 passed. See [scenario evidence](docs/testing/student/exit-guardian-identifier/2026-10-30.md).

## WIP-STUDENT-GUARDIAN-CHECKOUT-036: Guardian authorization and fee checkout

Status: **WIP — real provider, running-product and deployed acceptance pending.** Guardian checkout rechecks the active linked account, Student, approved admission and installment while database rows are locked in the same transaction as payment-attempt preparation. A revoke committed first prevents a new attempt and replay; an attempt committed first may finish its provider call and later requests are denied. Both forced interleavings passed with a fake provider and disposable PostgreSQL; Student 114/114, affected Staff/Curriculum 41/41 and Bank/Profile/Role plus portal fee 87/87 passed. Run the same Student disposable-database suite and protected gates before and after related changes; exact commands and scope are in [scenario evidence](docs/testing/student/guardian-fee-portal/2026-10-30.md). This entry does not claim provider or Render acceptance.
