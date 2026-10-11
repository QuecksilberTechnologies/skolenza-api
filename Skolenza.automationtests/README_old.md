# Skolenza automated API + UI tests

This project runs API tests directly and opens Playwright-managed Chromium for UI tests. It uses NUnit, so every test appears in Visual Studio **Test Explorer**.

## What is already covered

Four-country Render replacement evidence (81 local locked cases before/after,
4 Render rollback probes, complete table reconciliation):
[2026-10-01 database report](../docs/testing/database/tenant-cleanup-geography-regulatory-seed/2026-10-01.md#authorized-four-country-local-to-render-replacement-follow-up).

- API: Swagger document is available.
- API: public `ClientInfo/detect-device` response contains the automation browser identity.
- API: authenticated navigation rejects a request without a token.
- API: a permission-aware Tenant Email Configuration CRUD flow (create, read, list, update, delete, and cleanup).
- Unit: Tenant device configuration permission behavior rejects wrong-module and denied-operation requests before a handler/queue can run, and confirms transport is resolved from stored configuration rather than a Tenant request field.
- Unit: the recent Device and Employee API surface is locked down end-to-end at the controller contract level: route and HTTP verb, authentication, Device DDL sections and values, employee permission-module bindings, and credential-response redaction. This covers tenant-device configuration, gateway/runtime commands, location and attendance setup, employee enrollment/work setup, and Host card inventory.
- Unit: Host administration regression tests cover all Host controllers plus the Host-facing Tenant, SMTP, and device endpoints. They confirm every non-onboarding action is authenticated, an already verified Tenant cannot trigger SMTP resend, that conflict is returned as HTTP 409, and Host users are intentionally denied Tenant runtime-configuration screens after device assignment while initial device bootstrap remains available.
- UI: the Angular login route loads in Chromium.

The UI test is deliberately skipped until a frontend address is supplied. That keeps `Run All` safe when the frontend is not running or is located outside this repository.

## One-time setup

1. Open `Skolenza.sln` in Visual Studio and allow NuGet restore to finish.
2. Build the `skolenza.automationtests` project once.
3. Install the browser revision matched to the project once:

```powershell
pwsh .\skolenza.automationtests\bin\Debug\net10.0\playwright.ps1 install chromium
```

## First run in Visual Studio

1. Start `skolenza.api` using the **http** launch profile. Swagger should open at `http://localhost:5170/swagger`.
2. In **Test Explorer**, run the tests in the `API` category. All three API tests should pass.
3. Start the Angular frontend from its own repository/application.
4. Change `WebBaseUrl` in `automationsettings.json` to the frontend address, for example `http://localhost:4200`.
5. Run the `UI` category in Test Explorer. A headless Chromium browser performs the login-page smoke test.

To watch the browser, set `Headless` to `false` in `automationsettings.json`, then rerun the UI test.

## Commands

### Durable bulk master import and designation department scope

`Category=BulkImport` covers Excel/CSV/paste parsing, column mapping, row errors,
department-specific designation names, existing roles, endpoint authentication and
the existing Department/Designation/Role permission behaviors.

`Category=BulkImportDatabase` executes actual designation repository and database
constraint checks, real persisted permission checks, confirmation idempotency,
scheduled execution, parallel workers, interrupted-batch recovery, partial-failure
retry, cancellation and the hosted background worker. Set `SKOLENZA_BULK_TEST_CONNECTION` to an isolated database named
`skolenza_bulk_test`, restored from the workforce backup, then apply
`database-scripts/EnforceDesignationDepartmentScope.sql` followed by
`database-scripts/AddDurableMasterBulkImport.sql` to that isolated database.
Never point these tests at production. Missing configuration is skipped explicitly;
designation tests roll back their changes; durable tests commit to verify recovery
and then remove only their own uniquely named records/jobs. The restored fixture
needs an active tenant employee/role with existing grants for all three masters.

```powershell
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj --filter "Category=BulkImport|Category=BulkImportDatabase" --logger "trx;LogFileName=bulk-import.trx" --results-directory artifacts/bulk-import
```

Department, Designation, Role and EmployeeType support saved previews, confirmation, durable
background execution, history, retry, cancellation, templates and result reports.
AI is disabled by user decision. Angular integration remains a separate frontend
task. The single API/UI handoff and progress reference is
`docs/AI_ASSISTED_BULK_IMPORT_REFERENCE.md`.

EmployeeType is tenant-owned. Also apply `AddTenantEmployeeTypes.sql` and
`SeedTenantEmployeeTypeModule.sql`, or run `database-scripts/ApplyBulkImportMigrations.ps1`
with the intended API environment. EmployeeTypeDatabaseTests cover ownership,
duplicate names, hidden legacy/foreign rows, onboarding and database assignment
constraints. Durable tests include EmployeeType import and repeatable migration
execution for both Development and Production selections, always overriding the
connection to the isolated test database. These runner checks require PowerShell 7
and psql; set SKOLENZA_TEST_PSQL_PATH if PostgreSQL is installed elsewhere.

```powershell
# API tests only
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj --filter "Category=API"

# UI tests only
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj --filter "Category=UI"

# Entire suite
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj

# Tenant Email Configuration CRUD only
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj --filter "Category=TenantEmailConfig"

# Tenant device configuration permission/transport behavior unit tests
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj --filter "FullyQualifiedName~TenantDeviceConfigurationPermissionBehaviorTests"

# Recent device, employee enrollment, work setup, card inventory and DDL API contract tests
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj --filter "Category=RecentDeviceEmployee"

# Host administration API scenarios: auth boundary, resend-verification state,
# HTTP 409 conflict envelope, and the Host/Tenant device ownership rule
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj --filter "Category=HostApi"

# Tenant role-type response mapping, including Client=4 and unknown fallback
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj --filter "Category=RoleTypeMapping"
```

## Host API regression scenarios

The Host regression tests also compile the actual repository queries with the PostgreSQL EF provider.
Connection/command interceptors prevent database access and supply empty results: these checks validate
SQL translation, not production schema compatibility or populated response data. Coverage includes
Tenant lists (verification unset/true/false, search, active filter and paging), Tenant detail/update loading,
Host users/roles, device catalogue, installed devices and device connectivity lists.

The Tenant list regression reproduces `The LINQ expression 'employee' could not be translated` before
the fix. The filtered credential Include now compares TenantId through `credential.Employee.TenantId`,
preserving the tenant match without referencing the outer Include lambda. Deploy/restart the updated
API to apply this repository fix; no seed reset or schema migration is required.

| Scenario | Expected result |
| --- | --- |
| Host opens a Host administration page or calls its endpoint | The action has a concrete HTTP route and requires an authenticated session. |
| Anonymous registration / token verification | Only `POST /api/Tenant/create-tenant` and `POST /api/Tenant/verify` remain anonymous. |
| Host selects **Resend verification** for a Tenant already shown as verified | No token or SMTP call is made; the API returns the standard `409 Conflict` response. |
| Host issues the initial bootstrap URL for unassigned inventory | The permission behavior permits the request to reach the handler. |
| Host reads Tenant Device Configuration | List/detail reads require the scope-2 `TENANT_DEVICE_CONFIG` module and a persisted Host grant. A missing grant or a different module returns `403`. Runtime mutation ownership remains Tenant-only. |

### Authenticated Host device checks

`Category=HostLive` logs in and calls the real device/configuration list endpoints, checking HTTP 200,
the success envelope, array data and pagination. Supply `SKOLENZA_HOST_LOGIN_ID`,
`SKOLENZA_HOST_LOGIN_PASSWORD`, `SKOLENZA_HOST_DEVICE_MODULE_ID`,
`SKOLENZA_HOST_CONFIGURATION_MODULE_ID`, and `SKOLENZA_HOST_VIEW_OPERATION_ID` through the
environment. Obtain IDs from my-menu; configure the target with `SKOLENZA_TEST_API_BASE_URL`.
Missing settings cause an explicit skip, not a pass. No records are created or deleted by these list checks.

`Category=HostDatabaseRead` additionally reads actual rows and maps them through the application mapping
profile. Opt in with `SKOLENZA_HOST_DB_SETTINGS` pointing to the intended API settings JSON containing
`ConnectionStrings:DefaultConnection`. This detects missing columns that translation-only tests cannot find.

On 2026-09-09 the populated read reproduced PostgreSQL `42703` for
`PendingHttpsIngressTokenExpiresDateTime`. Apply `database-scripts/AddPendingHttpsGatewayReplacement.sql`
transactionally before the corresponding API release. This adds the pending token columns, constraint
and index without resetting seed data. The production device list returned HTTP 200 with one row after
the migration; the configuration permission fix additionally requires the updated API deployment.
Both authenticated HostLive tests passed against the updated local API using the configured database;
the populated database check mapped one device and one configuration row successfully.

## Configuration and secrets

`automationsettings.json` contains only local addresses and browser mode. Environment variables override it when needed:

```powershell
$env:SKOLENZA_TEST_API_BASE_URL = "http://localhost:5170"
$env:SKOLENZA_TEST_WEB_BASE_URL = "http://localhost:4200"
$env:SKOLENZA_TEST_HEADLESS = "false"
```

The Tenant Email Configuration CRUD test intentionally needs a dedicated Tenant Admin test account. Set these only in the terminal session that runs the test; do not save them in a file:

```powershell
$env:SKOLENZA_TEST_LOGIN_ID = "tenant-admin@example.test"
$env:SKOLENZA_TEST_LOGIN_PASSWORD = "your-test-password"
dotnet test .\skolenza.automationtests\skolenza.automationtests.csproj --filter "Category=TenantEmailConfig"
```

Before this test can run, apply `database-scripts/complete seed data/Skolenza_New_Production_Module_Operation_Seed.sql` to the intended **non-production** database, enable `TENANT_EMAIL_CONFIG` plus its CRUD operations for that tenant through the normal plan-entitlement synchronization, and assign Add, View, Update, and Delete to the test account's role. The test creates an **inactive** SMTP configuration and always deletes it, so it cannot replace the tenant's active mail configuration.

Do not place user passwords, JWTs, or production credentials in `automationsettings.json`.

## Artifacts when a UI test fails

On a failed UI test, Playwright saves a screenshot and trace ZIP under the test project's build output `artifacts` directory. The trace can be opened with:

```powershell
pwsh .\skolenza.automationtests\bin\Debug\net10.0\playwright.ps1 show-trace <trace-file.zip>
```

## Bulk validation checklist: read before every bulk-import session

Continuing contract: [AI_ASSISTED_BULK_IMPORT_REFERENCE.md](../docs/AI_ASSISTED_BULK_IMPORT_REFERENCE.md).
Read this checklist plus that reference before changes or deployment. Preserve
existing permission pipelines, constants, enums, mappings and handler/repository
patterns. Do not claim a target deployment or live HTTP acceptance pass from an
isolated database or controller-contract test.

### Source headers and filenames

| Source header for designation names | Current behavior |
| --- | --- |
| DesignationName / designation name / Designation_Name | Auto-matches after case/space/punctuation normalization |
| DesName / design name / DesignationType | Requires explicit manual mapping to DesignationName |
| Several possible designation-name columns | User selects the correct source; do not guess or merge columns |

Example ColumnMappingJson for source headers DesName and Dept:

```json
{"DesignationName":"DesName","DepartmentName":"Dept"}
```

For another alias, replace DesName with the exact source header. This is column
mapping, not database schema renaming. If DesignationType actually means a category
instead of a designation name, the user must select the correct column based on
its values. Semantic meaning is not automatically validated by AI; AI is disabled.
Unmapped required fields produce preview errors and CanCommit=false. Arbitrary
unused source columns are ignored and should be visibly marked as unmapped in UI.

The uploaded filename can be any name ending in .xlsx or .csv; it does not decide
the master type. The selected /api/Department, /Designation, /Role or /EmployeeType
route does. .cvs is not a supported extension. Raw files are not retained; normalized
preview rows/mapping/results are persisted in BulkImportJob. Multiple-sheet XLSX
requires the exact SheetName; no automatic semantic sheet selection exists.

### Validation and acceptance matrix

| Check | Required outcome |
| --- | --- |
| File versus pasted data | Exactly one source; empty input rejected |
| CSV | Valid UTF-8, quoted commas/newlines preserved, consistent row widths |
| XLSX | Valid bounded workbook, chosen sheet; reject formulas, error cells, merged cells and external worksheet links |
| Limits | 5 MiB file, 25 MiB expanded workbook, 5,000 rows, 64 columns, 4,000 characters/cell, 1,000 ZIP entries |
| Headers/mapping | Nonempty unique headers; supported targets only; one source column cannot populate several targets; required fields mapped |
| Required values | DepartmentName; DesignationName + DepartmentName; RoleName + existing numeric RoleType; EmployeeType TypeName |
| Text lengths | Department/Designation names 255; RoleName 100; Description 500 and Remark 200; EmployeeType fields each 255 |
| IsActive | true/false only; missing/blank defaults true |
| Source duplicates | Trim/case-insensitive identity duplicates invalidate all involved rows |
| Existing active match | Skip existing record; preserve values; no overwrite/reactivation |
| Inactive/ambiguous match | Preview error; no silent merge/reactivation |
| Designation ownership | Same name allowed in different departments; parent must be active and belong to this tenant |
| Role type | Existing Admin=1, Employee=2, Manager=3 only; conflicting existing type rejected; no role grants/employee assignment |
| EmployeeType ownership | Tenant-only options/reads/import; hide shared legacy/foreign rows; existing references migrated; cross-tenant assignment rejected |
| Authentication/permissions | Existing module/action grant required; client cannot choose TenantId/actor; View cannot confirm/create |
| Job ownership | Other tenant or another actor cannot read, confirm, cancel, retry or download this job |
| Confirmation | Only valid saved draft; no replacement rows; repeated Confirm does not create another job |
| RequestId | Same source/sheet/mapping replay returns saved draft; changed input with same ID returns conflict |
| Schedule | Null means eligible now; future UTC means not-before time; past time rejected |
| Concurrent changes | Worker rechecks DB and permissions per batch; unique constraints protect races; changed parent/error rows reported |
| Crash/restart | Master inserts and progress commit together; uncommitted batch rolls back; committed rows are not recreated |
| Retry | Only Failed/CompletedWithErrors; created rows retained; failed/pending rows revalidated |
| Cancel | Stops pending work at batch boundary; committed rows remain; no undo |
| Reports | Quoted CSV, formula protection, row numbers/IDs/errors; downloads are files, not JSON success envelopes |
| Infrastructure failure | Rollback/log/retry next poll; no attempt ceiling or automatic low-traffic detection implemented |

These are the implemented validation contracts, not a guarantee that every possible
conflict can be predicted at preview time. Another user can change data before the
worker runs; revalidation, database constraints and row error reporting handle that.
Structural validity also cannot prove a manually mapped column has the user's intended
business meaning. UI must show mapped sample values and require confirmation.

### Repeatable tests and target-deployment acceptance

- BulkImportPreviewTests: parser and mapping cases, duplicates, required values,
  master matching and the explicit designation alias examples across XLSX/CSV/paste.
- BulkImportPermissionTests: routes, authentication, permission pipeline and action restrictions.
- DesignationDepartmentDatabaseTests / EmployeeTypeDatabaseTests: real isolated-DB
  ownership, duplicate and migration/reference constraints.
- DurableBulkImportDatabaseTests: saved jobs, worker, replay, scheduling, concurrency,
  recovery, retry/cancel/history and migration runner in both environment selections.

```powershell
# Focused aliases/canonical headers: no DB required.
dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-restore --filter 'FullyQualifiedName~Custom_designation_headers|FullyQualifiedName~Canonical_designation_headers' --logger 'trx;LogFileName=bulk-header-mapping.trx' --results-directory artifacts/bulk-import
# Full backend suite: set SKOLENZA_BULK_TEST_CONNECTION to the isolated fixture first.
dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-restore --filter 'Category=BulkImport|Category=BulkImportDatabase' --logger 'trx;LogFileName=bulk-import.trx' --results-directory artifacts/bulk-import
```

Before a requested target deployment: identify the selected environment/connection
without logging credentials, verify backup and existing data conflicts, drain old
API/worker, apply the four scripts via ApplyBulkImportMigrations.ps1, start the new
API, verify schema/worker logs and existing entitlements/grants. Use the intended
authorized test tenant for authenticated XLSX/CSV/paste → preview → confirm → poll →
report checks. Include alias mapping, duplicates, cross-tenant denial and retry.
Verify actual persisted rows/counts against the job report and clean only explicitly
identified test records. Record environment, test cases, results, failure/skip details
and cleanup in the continuing reference. Never run the destructive isolated-DB
automation suite against production.

Current target migration/restart/live HTTP acceptance: PENDING. The user requested
confirmation of this behavior and document names before giving the next instruction.
The subsequent 2026-09-10 instruction grants migration/restart and completion approval
and forbids rerunning already tested cases. Target acceptance is now WIP: the intended
API URL/service host and authorized test tenant/account must be identified. Development
and Production configuration-only migration checks both succeeded and resolve to the
same remote database. No migration, restart or live acceptance has been performed;
existing passed test suites were not rerun. See the reference's target acceptance follow-up.

Latest focused run (2026-09-10): 18 alias/canonical-header tests passed, zero failed
or skipped. Result: artifacts/bulk-import/bulk-header-mapping.trx. Previous complete
backend run: 88 bulk + 119 regression tests passed; no actual-target deployment or
live HTTP validation was performed during this documentation/header review.

Production execution follow-up (2026-09-10): target identified as
https://skolenza-api.onrender.com. Backup archive created and fully read successfully;
Render suspension verified; all four Production migrations committed; post-migration
schema/ownership checks passed. Deployment `dep-dahcpf7qj5pc73a6mu1g` for commit
`d2754cd984e8edf2570d310e556fda68a8eb7716` succeeded and is Live. Actual Swagger
exposes all 32 bulk routes and runtime logs show queue polling. Authenticated bulk acceptance and
DB-result comparison remain PENDING on an authorized tenant login/session. Earlier
passed suites were not rerun. Backup location/hash and execution evidence are in
the main reference's Production execution section; this supersedes the earlier
statement that no target migration was performed.

Authenticated development acceptance (2026-09-10): tenant 8 passed nine live
Department/Designation/Role XLSX, CSV and pasted-CSV flows plus seven HTTP validation
checks. Nine completed jobs and six sample records matched downloaded reports in
read-only database reconciliation. EmployeeType remains BLOCKED on missing tenant
entitlement/module 79 and Add/View grants; its successful import is not a pass.
The existing passed test suites were not rerun. See `../docs/bulk-upload/README.md`
for sample XLSX/CSV files, actual response/report evidence and the remaining
EmployeeType-only command. No passwords or tokens are stored in those artifacts.

### Employee phase: 2026-09-11

77 distinct focused Employee-pattern/import/invitation tests passed. Zero remaining
failures/skips. The inventory and exact source TRX files are listed in
`../docs/bulk-upload/results/employee-automated-summary.json`. Changed locking and
capacity paths were retested; unrelated previously passed master suites were not.

New test categories: `EmployeeBulkCode`, `EmployeeBulkCodeDatabase`, `EmployeeImport`,
`EmployeeImportDatabase`, `EmployeeImportInvitations`. Database cases require
`SKOLENZA_BULK_TEST_CONNECTION` pointing to the disposable database named exactly
`skolenza_bulk_test`. They must never run against the target/production database.
The fixture requires existing tenant/admin/master data, a valid subscription and
all five bulk SQL migrations. Fixtures restore their test changes.

Focused command example (select only new/affected cases when continuing):

```powershell
dotnet test skolenza.automationtests/skolenza.automationtests.csproj `
  --artifacts-path artifacts/employee-code-tests `
  --filter 'TestCategory=EmployeeImportInvitations' `
  --logger 'trx;LogFileName=employee-invitations.trx' `
  --results-directory artifacts/bulk-import
```

Separate artifact output avoids locking a developer's running API executable.
For later runs, do not rerun all categories by default. Use the saved case inventory
to select unresolved cases or tests directly affected by new code changes.

Verified: original joining dates and Excel date systems; preserved suffixes and
counter reservations; preview/confirm staleness; pattern and account transactions;
tenant isolation; active references; Admin capacity count; role/image/contact
creation; duplicate rejection; invitation claim/retry/idempotency and public
response privacy. No real SMTP email was sent by these tests.

Authenticated HTTP/deployed acceptance remains PENDING. Automatic approval review
rejected a separate isolated API process launch with `blocked by policy`; no API
was started by that command. Do not report repository/route tests as live HTTP
passes. UI handoff: `../docs/bulk-upload/EMPLOYEE_IMPORT_UI.md`.

### Release correction, 11 September 2026

FINAL: legacy fix `2c91b6eb` is deployed and live recoding/restore plus final DB
verification passed. Employee import create/replay and EmployeeType CRUD/menu
acceptance are COMPLETE. Add-existing-pattern rejection is live-verified;
missing-pattern creation/XLSX remain automated coverage. No SMTP emails sent.
The pending paragraphs below describe earlier checkpoints.

Later live acceptance passed Employee create/replay/report/DB and EmployeeType CRUD
and menu checks. Legacy Admin creation-year recognition required a further formatter
fix: `employee-legacy-date.trx` contains 3 passing focused cases (2 new, 1 affected
missing-date regression). This fix still needs deployment/live recoding acceptance.

Release publish reproduced the EmployeeType handler/repository response namespace
mismatch in commit `3387971`. The handler now aliases the existing repository DTO.
Release publish passes. Two new `List_preserves_repository_response_and_page_metadata`
cases pass (0 failed/skipped); evidence is
`artifacts/bulk-import/employee-type-response-regression.trx`.
Only these new cases ran; the existing 77 Employee cases were not repeated.
EmployeeType live XLSX/CSV/paste bulk and DB/report verification also passed;
manual CRUD update/delete and live Employee acceptance remain pending deployment.
# Host tenant verification resend review — 2026-09-11

## Supplied failure logs and target configuration — confirmed findings

## Brevo account evidence — 2026-09-11

- User-authenticated Brevo account opened successfully; Transactional → Email →
  Logs shows 49 events for 04/09/2026–11/09/2026.
- Latest recipient `axionvibe@gmail.com` has `Sent` and `Delivered` at 11/09/2026
  22:09. Earlier attempts for `mca.deepesh@gmail.com` show Delivered at 09/09/2026
  21:13, plus Sent/Clicked/Open events. This proves Brevo accepted and delivered
  multiple welcome messages for this account; it does not prove every API attempt
  succeeds.
- Historical entries for `mca.deepesh@gmail.com` and `axionvibe@gmail.com` also show
  `Error` events. Therefore the observed UI success response and error toast can be
  separate overlapping requests/attempts, not one response with two states. UI must
  correlate toast/request ID and avoid firing resend more than once per click.
- Brevo usage showed 299 of 300 emails remaining after the latest activity. No
  additional resend was triggered during this verification, avoiding duplicate mail.

- User-supplied Render log reports `System.TimeoutException` in
  `MailKit.Net.SocketUtils.ConnectAsync` / `SmtpClient.ConnectAsync`, at
  `EmailService.cs:128`, for WELCOME_EMAIL / tenant 9. This failure is during
  connection establishment, before SMTP authentication or message submission.
- Read-only target DB inspection: tenant 9 has no TenantEmailConfig. Active Host
  default is DefaultEmailConfig ID 2, smtp-relay.brevo.com, port 587. Another active
  row (ID 1) is not default and is not selected by the repository. No secrets exported.
  The earlier tenant-config preference hypothesis therefore does not explain this
  tenant's current configuration.
- Render documents that Free web services block outbound ports 25/465/587:
  https://render.com/docs/free . Current instance plan still needs verification;
  the connection timeout alone does not prove the platform restriction caused it.
- Brevo documents port 2525 as the alternative when 587 is blocked:
  https://help.brevo.com/hc/en-us/articles/10905415650322-Which-SMTP-port-should-I-use-Port-587-465-or-2525 .
  Proposed configuration correction: select active default ID 2 through existing
  Host configuration update flow and use 2525 with existing required STARTTLS.
  Do not change credentials or disable TLS. Change has NOT been applied.
- New screenshot shows one successful API envelope alongside a failed request and
  error toast. It does not prove inbox delivery, a UI bug, or that the failed and
  successful attempts used identical deployment/configuration. Correlate each request.
- Authenticated Host session is still unavailable (browser inventory has no tabs).
  Remaining: apply/verify configuration through existing permission flow, perform
  one authenticated resend, correlate SMTP acceptance and recipient delivery.
  No blanket automatic resend retry was added; uncertain delivery can duplicate mail.

**WIP: authenticated live resend/root-cause verification remains blocked on Host
login and Render logs. This is separate from Employee bulk invitations.**

- Reviewed TenantController route, ResendTenantVerificationCommandHandler,
  TenantManagementPermissionBehavior and EmailService before testing.
- Added 6 regression cases in `Unit/HostApiRegressionTests.cs`: accepted delivery,
  rejected delivery, empty token (no send), missing template, inactive template,
  and unavailable tenant/Host SMTP configuration. All 6 passed, 0 failed/skipped.
  Handler tests verify recipient/template/link, 30-minute token lifetime and that
  resend does not mark the tenant or onboarding credential verified.
- The first 3 cases use a fake email service; the other 3 exercise the real email
  service with fake repositories and no network. These are not SMTP delivery tests.
- Exact supplied Render POST route, without credentials, returned HTTP 401.
  No authenticated resend/email was attempted; screenshot HTTP 500 root cause is
  not yet confirmed. No production code/configuration changes were made.
- Review observations to correlate with logs: registration forces Host SMTP;
  resend prefers tenant SMTP. An apparently complete tenant configuration does
  not fall back on SMTP authentication/delivery failure. Template/configuration/
  transport failures return false and the handler converts false to generic 500.
  These observations are not proof of which failure occurred on Render.
- Next: use authenticated Host session for one resend, correlate response with
  server logs, identify actual exception/configuration, then add a cause-specific
  regression and verify any justified fix after deployment. Do not claim delivered
  mail from mocked success or repeat send after an uncertain outcome without checking logs.

Focused commands (each new group ran once; unrelated passed suites not rerun):

```powershell
dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-restore --filter "FullyQualifiedName~Resend_delivery_result_is_truthful"
dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-restore --filter "FullyQualifiedName~Resend_email_service_rejects"
```
# Employee profile permission status mapping — 2026-09-11

- Central `EmployeeTenantPermissionBehavior` now validates the authenticated
  Tenant context first. A valid authenticated user whose Employee profile request
  has no/incorrect permission data receives `403 Forbidden`; invalid/expired
  authentication still receives `401 Unauthorized`.
- This covers Employee Overview, Bank, Contact, Device, Work, Location,
  Arrangement, Pattern and other `EmployeeCmd` profile request families through
  their shared pipeline. Target employee data-scope denials remain 403.
- Added and passed regression test:
  `Tenant_permission_denial_is_forbidden_while_invalid_permission_context_is_unauthorized`.
- No separate permission pipeline or endpoint-specific bypass was introduced.

## 2026-09-12 TenantLocation Host regression

Read-only target DB verification: module 78 is HOST_TENANT_LOCATION_LIST,
ModuleScope=2, IsActive=true. TenantLocationPermissionBehavior incorrectly required
TENANT_LOCATIONS after Host runtime authorization. Local fix selects the Host
location module for Host callers and keeps TENANT_LOCATIONS for tenant callers.
Existing persisted authorization is retained. Four binding regression cases added
in EmployeeCodePatternPermissionTests. Target deployment/authenticated acceptance
is still pending; no target data was modified for this investigation.
Validation: focused EmployeeCodePatternPermissionTests passed 15/15, including four new Host/Tenant location binding cases. Output: artifacts/location-permission-test.log.

2026-09-12 follow-up: six Host_location_list_pipeline cases exercise Handle and persisted Host permission invocation using request ModuleId=78/OperationId=4. Allowed, denied, invalid-session, stale-role, tenant-module and unrelated-host-module cases passed. Combined focused run: 21 passed, 0 failed, 0 skipped. Live authenticated acceptance remains pending.

2026-09-12 TenantDevice/create: reproduced omitted OperationId in actual CreateTenantDeviceCommandHandler. Focused regression passed 1/1, proving rejection before permission lookup/device write. Read-only DB: module 35 TENANT_DEVICES maps Assign=11, View=4, Update=2, Remove=12, Active=8, Inactive=9; no Add/Create mapping. UI must supply its granted assignment action ID from MyMenu. No live create executed; successful insertion is not verified. Evidence: artifacts/device-create-regression.log.

## Host bulk mapper — 2026-09-12

`dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-restore --filter Category=HostBulkImport`

11 passed, zero failed/skipped. Covers protected field injection, card leading zeros,
ISO dates/decimal parsing, invalid enums, explicit header mapping and duplicate headers.
This result does not establish API, persistence or worker completion; those remain pending.

## Host bulk completed local validation — 2026-09-12

The mapper-only note above is superseded: 14 mapper + 12 Host permission + 10
PostgreSQL/local HTTP tests passed (36 distinct Host tests), with zero skips.
75 shared bulk regression tests also passed. Card/device insertion and reports,
encrypted snapshots, cross-owner/tenant denial, revoked permission/retry,
invalid mappings/lengths and cancellation before/after a committed batch are covered.
HTTP tests use actual controllers and handlers with signed test JWT and a fixture
Host context; they are not production login/deployment tests.

```powershell
$env:SKOLENZA_BULK_TEST_CONNECTION='Host=127.0.0.1;Port=55439;Database=skolenza_bulk_test;Username=postgres'
dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-restore --filter 'Category=HostBulkImport|Category=HostBulkPermission|Category=HostBulkDatabase'
```

The database fixture refuses non-local hosts or databases not named skolenza_bulk_test.
Apply AddHostBulkImport.sql and SeedHostBulkImportModules.sql to the isolated clone
before running. Tests use temporary Host grants and remove their own records.
Evidence logs: artifacts/host-bulk-workflow-tests.log,
artifacts/host-bulk-http-regression-tests.log, artifacts/host-bulk-final-edge-tests.log.

## Module metadata and operation seed — 2026-09-14

The complete production seed contains no Dashboard seed entries. Its final
normalization fills empty Module `Remark`, `ImageIconWeb` and `ImageIconMobile`
values while preserving existing values. The same seed consolidates the legacy
Create alias into canonical Add and migrates persisted operation references before
removing redundant aliases.

```powershell
dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-restore --filter "Category=ModuleOperationSeed"
dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-build --no-restore --filter "Category=ModuleOperationSeedDatabase"
```

Local result: source contract 1 passed, 0 failed/skipped. Database reconciliation
0 passed, 0 failed, 1 skipped because the disposable PostgreSQL fixture was not
available. Combined contract and affected Host seed regressions passed 4/4 with
zero failures/skips. Full seed first-run/rerun remains required and must not be
reported as passed. Scenario:
`docs/testing/module/module-operation-seed/2026-09-14.md`.
Release/API examples: docs/bulk-upload/HOST_CARD_DEVICE_IMPORT.md.

## Refresh and authentication status regression

RefreshTokenDatabaseRegressionTests reproduces the actual Tenant refresh null
ParentModuleId materialization failure and validates fixed Tenant/Host rotation,
including invalid/reused token rejection. AuthenticationStatusRegressionTests
checks 403 permission denial, 401 stale/invalid authentication, and 400 missing
action IDs using the API error middleware. Eleven focused cases passed across
the final runs, including three existing EmployeeMutationPermission tests.
See docs/AUTH_REFRESH_TOKEN_FIX.md for evidence, fixture setup and deployment status.

```powershell
dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-restore --filter 'Category=RefreshDatabase|Category=AuthenticationStatus|Category=EmployeeMutationPermission'
```

RefreshDatabase requires SKOLENZA_BULK_TEST_CONNECTION pointing to the isolated
local skolenza_bulk_test; never point this fixture at production.
# Opt-in refresh-token-v2 — 2026-09-13

**Latest V2 correction run: 35/35 PASS, 0 failed/skipped.** Atomic V2 rotation fixes the previous Host/Tenant concurrency failures; oversized IP now returns 400 before DB writes. PostgreSQL rollback, persistence, HTTP boundaries and old/V2 interoperability verified. Evidence: `artifacts/refresh-v2-fixed-tests.log` and `docs/AUTH_REFRESH_TOKEN_V2.md`. Earlier failures below are historical. Deployment and UI acceptance remain separate pending steps.

Continuing-session decision and UI instructions: [AUTH_REFRESH_TOKEN_V2.md](../docs/AUTH_REFRESH_TOKEN_V2.md).
Branch: `codex/lightweight-refresh-api`. Legacy refresh stays unchanged pending explicit UI acceptance and user approval.

Focused tests (the DB categories require `SKOLENZA_BULK_TEST_CONNECTION` pointing only to the isolated local `skolenza_bulk_test` database):

```powershell
dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-restore --filter 'TestCategory=RefreshV2|TestCategory=RefreshV2Database|TestCategory=RefreshDatabase'
```

Validation: 18/18 PASS initially (14 V2 unit, 2 V2 PostgreSQL, 2 existing legacy PostgreSQL regression cases), followed by 2/2 additional Host/Tenant loopback HTTP interoperability cases. Total 20 distinct PASS, no skipped cases. Old → V2 → old replacement-token compatibility and HTTP 400/401 verified. Deployment/UI acceptance remains pending; see the V2 reference for evidence and limits.
## Host catalogue bulk imports (2026-09-13)

Apply `AddHostBulkImport.sql` and `SeedHostBulkImportModules.sql` to the isolated
`skolenza_bulk_test` fixture, then run:

```powershell
$env:SKOLENZA_BULK_TEST_CONNECTION='Host=127.0.0.1;Port=55439;Database=skolenza_bulk_test;Username=postgres'
dotnet test --filter "FullyQualifiedName~HostBulkImportTableMapperTests|FullyQualifiedName~HostBulkImportPermissionTests|FullyQualifiedName~HostBulkImportDatabaseTests"
```

The catalogue database case validates Module → Child Module → Operation → Mapping
creation and replay-as-existing. Contract tests verify all four controllers expose
the shared eight-route lifecycle.

## Scenario report requirement — 2026-09-13

For every tested scenario, maintain a dated report under
`docs/testing/<module>/<scenario>/<YYYY-MM-DD>.md` using the
[scenario template](../docs/testing/SCENARIO_TEMPLATE.md). Include the actual
inputs, API steps, expected/actual results, command and test names, environment,
commit, evidence, skipped/blocked checks, persistence and cleanup. Preserve the
distinction between local tests and deployed acceptance. Do not rerun passing
tests solely for documentation.

- [Scenario index](../docs/testing/README.md)
- [Employee China/USA identity report](../docs/testing/employee/country-identity/2026-09-13.md):
  previous focused run recorded 18 passed, 0 failed, 5 DB-dependent skipped.
  Local date-projection regression passed; deployed identity acceptance remains
  pending. No new test run was performed while creating this report.
- [Bulk existing-module operation cleanup](../docs/testing/bulk/module-operation-cleanup/2026-09-13.md):
  complete seed first-run/rerun passed on isolated PostgreSQL; focused
  `BulkModuleOperationSeed`, `HostBulkPermission` and `HostBulkImport` run passed
  36/36 with zero failures/skips. Target DB reconciliation is recorded there.
- [Employee get-all assigned roles](../docs/testing/employee/get-all-assigned-roles/2026-09-13.md):
  JSON response/empty-array contract passed locally. The distinct active
  same-Tenant role database case is implemented and skipped unless the isolated
  `skolenza_bulk_test` connection is configured; deployed acceptance is pending.
- [Tenant creation without implicit policy types](../docs/testing/tenant/creation-policy-independence/2026-10-01.md):
  public and Host tenant-creation transaction variants passed 6/6 locally after
  removing the obsolete Insurance/Leave policy-type auto-seed path; deployed
  tenant/database acceptance remains pending.
- [Tenant registration actionable errors](../docs/testing/tenant/registration-actionable-errors/2026-10-01.md):
  the existing public and Host transaction regression now covers 14 success,
  persistence, conflict, cancellation and post-commit email cases; the final
  Development-database rollback probe passed 2/2 after four orphan tenant
  encryption keys were removed. Angular registration tests passed 44/44 and the
  production build passed; deployed registration acceptance remains pending.
- [Role Client type display](../docs/testing/role/client-role-type-display/2026-10-01.md):
  centralized Role/login mappings and the authenticated permission-ID-free
  role-type options contract passed 9/9. The protected tenant registration gate
  passed 14/14, and the API-backed Angular Add/Edit/filter integration passed
  69/69 focused tests. Authenticated and deployed HTTP verification remain pending.
- [Employee contact relation and location cascade](../docs/testing/employee/contact-relation-location/2026-10-01.md):
  enum-backed token-only relations (including Owner), the initial-contact
  transaction contract and nullable Locality persistence passed 45 backend/profile
  tests (including isolated HTTP 401/401/200) and 9 applicable locality tests.
  Contact DB probes passed 2/2; protected tenant DB rollback passed 2/2.
  UAE seed and migration applied locally; seed rerun inserted zero duplicates.
  Affected Angular consumers passed 66/66 and the production build passed.
  Full running-product authenticated/browser and deployed acceptance remain pending.
