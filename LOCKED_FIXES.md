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
| `LOCK-POLICY-LEAVE-011` | Leave Type targeting across policy versions, rules and applicability | LOCKED | Policy contract/schema 40/40; Angular 7/7 and production build; Local/Render schema and 8-type seed verified | DB APPLIED; API/UI DEPLOYMENT PENDING; S3 CREDENTIAL BLOCKED | [2026-10-08](docs/testing/policy/leave-type-targeting/2026-10-08_old.md) |
| `LOCK-POLICY-DRAFT-EDIT-012` | Policy list Draft row opens its exact version in Edit | LOCKED | Angular 8/8; production build; localhost Draft-v2 browser verification | UI DEPLOYMENT PENDING | [2026-10-08](docs/testing/policy/draft-version-edit/2026-10-08_old.md) |
| `LOCK-POLICY-ACCRUAL-013` | Leave annual entitlement and derived accrual-cycle amount | LOCKED | Backend 47/47; Angular 27/27; Release and production builds; Local/Render metadata verified | DB APPLIED; API/UI DEPLOYMENT PENDING | [2026-10-08](docs/testing/policy/annual-entitlement-accrual/2026-10-08_old.md) |
| `LOCK-POLICY-IDENTITY-014` | Tenant policy-name uniqueness and version guidance | LOCKED | Backend 48/48; Release build; Local/Render unique index verified | DB APPLIED; API DEPLOYMENT PENDING | [2026-10-09](docs/testing/policy/policy-name-uniqueness/2026-10-09_old.md) |
| `LOCK-POLICY-DRAFT-SAVE-015` | Atomic replacement of Draft policy rules and targets | LOCKED | Backend 49/49; Release build | API DEPLOYMENT PENDING | [2026-10-09](docs/testing/policy/draft-replacement-save/2026-10-09_old.md) |
| `LOCK-EMP-PROFILE-COMPLETION-016` | Mandatory-aware Employee profile completion consistency | LOCKED | Employee/contact 52/52; Bank 1/1; locality 9/9; tenant 14/14 + DB rollback 2/2; Role 20/20; Release build | API DEPLOYMENT PENDING | [2026-10-10](docs/testing/employee/profile-completion-consistency/2026-10-10_old.md) |

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

## Adding the next lock

Append the next entry; do not rewrite or erase historical locked evidence. If a
new authorized requirement intentionally conflicts with a lock, mark the old
entry `SUPERSEDED` with a link to the user's decision and the replacement lock.
Keep the earlier report available.

## Skolenza documentation lifecycle

- Files ending in _old.md are read-only AxionPro reference material. Do not update them for Skolenza implementation work.
- Record every new decision, implementation status and test result only in current Skolenza Markdown files without the _old suffix.
- If legacy behavior is reused, cite the _old.md source and write the accepted Skolenza decision in a current document.



