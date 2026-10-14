# Skolenza bulk import: implementation reference and UI handoff

## 2026-09-24 Employee invitation email smoke

- Employee import completion does not automatically send welcome email. Invitation delivery remains the explicit authenticated `bulk/send-invitations` action.
- Focused invitation/template tests: 8 passed, 0 failed, 1 isolated-DB test skipped.
- Configured DB readiness for Tenant 8 passed: one active Tenant SMTP configuration, one active Tenant `WELCOME_EMAIL`, one active fallback `WELCOME_EMAIL`, and one active default SMTP configuration.
- A real external message was not sent because an approved disposable recipient was not supplied. Actual mailbox delivery remains NOT RUN, not a pass.
- Evidence: `docs/testing/bulk/employee-invitation-email/2026-09-24.md`.

## 2026-09-23 Holiday rename and Icon — COMPLETE (local code and Development DB)

- Renamed the API root to `/api/Holiday`, the entity/table/DTO/repository/features/tests to `Holiday`, and the module identity to `TENANT_POLICY_HOLIDAY` / `Holiday`.
- Added nullable `Icon varchar(100)` to create, update, response, import and export contracts.
- Applied the guarded DB migration after a data-only backup. All 31 Tenant 8 / Jabalpur 2026 rows remain; the old table is absent.
- Retained module Id 118, `/app/holidays`, immutable page key, six operation mappings and 10 active plan mappings. The seed and migration reruns were idempotent.
- See `docs/testing/holiday-calendar/rename-to-holiday/2026-09-23.md` and `docs/UIdeveloperDoc/HOLIDAY.md` for validation and UI handoff.

## 2026-09-22 Holiday Calendar import/export — WIP

- 2026-09-22 duplicate-date change: Holiday Calendar create/update/import now
  reject any other non-soft-deleted row at the same tenant/location/date,
  including inactive rows and differently named holidays. Import no longer
  skips existing rows. A guarded partial unique index protects concurrent
  inserts. See the new scenario report under
  `docs/testing/holiday-calendar/unique-date/2026-09-22.md`.

- Follow-up live data smoke: user selected 2026, tenant 8 Jabalpur location.
  The configured target DB now has 31 sourced holiday rows across all 12
  months (29 MP government-listed and 2 optional); the transactional insert
  rerun added 0. Local unauthenticated API GET/export/multipart import returned
  401, and 14 focused tests passed. Authenticated import/export and employer
  holiday approval remain pending. Input CSV, exact DB results and limitations
  are in `docs/testing/holiday-calendar/live-2026-jabalpur/2026-09-22.md`.

- Requested scope: add Import and Export to the existing
  `TENANT_POLICY_HOLIDAY` module and holiday API.
- Implemented locally: bounded synchronous CSV/XLSX import using the shared
  table reader; tenant-location ownership and all-row validation; existing
  date conflict rejection; tenant-scoped CSV export with matching headers.
- This is a holiday-specific synchronous flow, not one of the 14 durable
  `BulkImportMaster` targets. It has no preview/confirm/job/report sequence.
- Seed applied twice to the configured target DB: exactly one Import and one
  Export mapping, with priorities 50 and 60. The isolated PostgreSQL seed
  fixture first-run/rerun requirement has not been executed; release acceptance
  therefore remains WIP, not COMPLETE.
- Local API build and rollback-only CSV import/export DB test passed. Authenticated
  HTTP import/export and deployed acceptance remain PENDING. See
  `docs/testing/holiday-calendar/import-export/2026-09-22.md` and
  `docs/UIdeveloperDoc/HOLIDAY.md`.

Updated: 2026-09-11. Read this file before continuing bulk-import work in any session.
Maintain user decisions, implementation sequence and COMPLETE / WIP / PENDING status.
Follow AGENTS.md, Employee handler/repository conventions, regions, comments,
endpoint documentation, existing permission pipelines, constants, enums and mappings.
Ask before implementing unclear business rules. COMPLETE below means the stated
backend scope, not all future bulk modules or production deployment.

## 2026-09-19 all-target deployed smoke verification

- **COMPLETE:** all 14 durable targets passed deployed template, preview,
  confirm/background completion with zero failed rows, and report download:
  Department, Designation, Role, EmployeeType, Employee, DeviceMaster,
  TenantCard, HostModule, HostSubModule, HostOperation, HostModuleOperation,
  PolicyType, PolicyDefinition, and PolicyAssignment.
- **COMPLETE:** meaningful Employee flow verified end to end: Aarav Mehta →
  Hybrid Full-Time → primary TenantLocation → published India Hybrid Attendance
  and Location Compliance policy → bulk PolicyAssignment → policy resolution.
- **COMPLETE:** Policy applicability includes both EmployeeType and
  TenantLocation; rules contain realistic Web/Mobile/Biometric attendance controls.
- **AUTOMATED:** filtered Bulk/Import suite: 141 passed, 0 failed, 39 skipped.
  Skipped environment-dependent tests remain skipped, not acceptance passes.
- **GAP:** no catalogue-wide business export endpoint exists. Current CSV export
  surfaces are the input template and completed-job report.
- Evidence: `docs/testing/bulk/all-live-smoke/2026-09-19.md`.

## 2026-09-16 tenant policy live acceptance update

- **COMPLETE:** target DB prerequisites, generic policy schema, durable bulk
  base, Policy masters 12–14 constraint, and tenant policy module seed applied.
- **COMPLETE:** authenticated Policy Type upload → preview → confirm → worker →
  report/DB reconciliation on Render (one row, Completed, one created).
- **COMPLETE:** Policy Definition and Policy Assignment live bulk persistence
  and target DB reconciliation.
- **COMPLETE:** Draft cancellation and Completed-job retry rejection.
- **WIP:** terminal-job cancellation conflict is fixed locally and awaits deploy;
  successful Retry still needs a genuine worker failed-row job.
- Evidence: `docs/testing/policy/target-db-migration/2026-09-16.md` and
  `docs/testing/policy/bulk-import/2026-09-16.md`, plus
  `docs/testing/policy/live-business-flow/2026-09-16.md`.

## 2026-09-15 tenant master hierarchy and entitlement repair

- **COMPLETE on Render development DB:** `DEPARTMENT`, `DESIGNATION`, `ROLE` and
  `EMPLOYEE_TYPE` are active leaf modules under `TENANT_DEPARTMENT`,
  `TENANT_DESIGNATION`, `TENANT_ROLE` and `EMP_MGMT` respectively.
- Each leaf has one active Add, Update, Delete, View, Import and canonical bulk
  Export mapping. The duplicate non-bulk EmployeeType Export mapping was removed.
- Active-plan tenant `8` received seven missing module snapshots, 30 enabled
  operation snapshots and 30 missing-only Tenant Admin permissions. Tenant `9`
  uses plan `18`, which does not contain these modules, and was not modified.
- Post-commit database assertions found zero missing hierarchy rows, plan mappings,
  tenant module snapshots, required enabled operations or Tenant Admin grants,
  and zero duplicate required operation names.

## 2026-09-14 module metadata and operation catalogue decision

- The only authoritative broad module/operation seed is now
  `database-scripts/production-seed/04-access-and-host/001-modules-operations-two-host-admins.sql`;
  the root-level duplicate was removed by explicit user request.
- The complete seed keeps one canonical `TENANT_DASHBOARD` navigation-parent
  entry. It does not delete Dashboard rows from the database.
- A final normalization preserves existing values and fills empty Module `Remark`,
  `ImageIconWeb` and `ImageIconMobile` values, followed by a database assertion.
- The user explicitly superseded the earlier immutable-operation-name decision for
  the duplicate Add/Create pair only. `OperationType=1` is consolidated to one
  canonical Add record and Create references are migrated before removal. All
  other operation names remain covered by the immutable-name rule.
- Source contract validation passed 1/1. Isolated PostgreSQL first-run/rerun is
  BLOCKED/NOT RUN because the local fixture at port 55439 was unavailable; the
  database test was skipped and this seed change is not production-ready.

## 2026-09-13 issue audit

- **COMPLETE — bulk permission catalogue simplification:** user accepted the UI
  developer's existing-module operation model. Separate Tenant/Host bulk Module
  rows, including the legacy `BULKUPLOAD` parent, were removed. Import/Export map
  directly to the eleven existing functional modules. Existing grants were
  migrated where applicable; entitlement/mapping/plan/role references were
  cleaned before Module deletion. The target DB has zero bulk-named modules,
  zero orphan TenantEnabledModule/TenantEnabledOperation rows, 22 active
  Import/Export mappings and zero duplicates. Full seed first-run/rerun passed
  on isolated PostgreSQL; focused result 36/36 passed. Scenario evidence:
  `docs/testing/bulk/module-operation-cleanup/2026-09-13.md`. Older sections that
  describe bulk child modules are historical and superseded by this decision.

- **COMPLETE locally — inactive durable-master retry:** target DB evidence showed the reported Role rows were inactive (`IsActive=false`) but not soft-deleted, with multiple older soft-deleted versions. An incoming `IsActive=true` row now previews the current inactive, non-deleted Department, Designation, Role or EmployeeType as `willReactivate`, and confirmation reactivates only that row without overwriting its other fields. Truly soft-deleted rows remain excluded and therefore import as new records. Reports expose `reactivatedCount`; worker-time conflicts become row errors. Focused preview/database suite: 50 passed, 18 isolated-DB tests explicitly skipped because `SKOLENZA_BULK_TEST_CONNECTION` was not configured.
- Employee profile verification payload fix is independent of import: the completion response now supplies nullable `tabInfoType` and `canUpdateVerificationStatus`, preventing assignment-summary rows from being sent to `/api/Employee/update-bulk`.

> **Mandatory test-before-handoff rule — user-confirmed 2026-09-13:** never hand
> over a bulk SQL file, migration, seed, API change or release as ready/complete
> based only on code review or compilation. Run the relevant automated tests and,
> for SQL, execute it against the isolated PostgreSQL fixture on both first run and
> rerun to prove FK ordering and idempotency. Record exact pass/fail/skip counts.
> A skipped, unavailable, manually inspected or unexecuted test is NOT a pass.
> If the required environment is unavailable, label the work UNTESTED/BLOCKED and
> do not tell the user it is production-ready.

> **Current SQL regression — 2026-09-13:** a module/operation seed failed with
> PostgreSQL 23503 while deleting Module Id 80 because child Module rows still
> referenced it through `FK_Module_ParentModule`. That script is NOT accepted or
> production-ready until child hierarchy/mappings are handled in FK-safe order and
> clean-fixture first-run plus rerun both pass.

> **Immutable PageName decision — user-confirmed 2026-09-13:** while changing
> Module, child-module, Operation or ModuleOperationMapping seed data, never change
> an existing Module `PageName`. Operations, mappings, hierarchy, display metadata
> and other explicitly approved seed values may be changed, but `PageName` remains
> the stable UI component-registration identity. New module rows must receive their
> approved PageName once; later seed reruns must preserve it.
>
> **Immutable OperationName decision — user-confirmed 2026-09-13:** never rename
> or overwrite an existing `Operation.OperationName` through Module/Operation seed
> work. Resolve and reuse the canonical operation by its existing identity/type.
> A missing operation may be inserted with its approved initial name, but later
> reruns must preserve that stored name and must not create a naming alias/duplicate.

> **UI developer handoff rule — user-confirmed 2026-09-13:** whenever the user
> requests a new API or endpoint, its implementation is not ready for handoff
> until a corresponding document exists under `docs/UIdeveloperDoc`. That document
> must describe the feature and UI behavior, authentication/permission discovery,
> every endpoint and HTTP method, mandatory/optional fields, request JSON or
> multipart FormData, Excel/CSV headers and examples where applicable, representative
> success/error response JSON, enum/status meanings, polling/retry/cancel behavior,
> destination/persistence behavior, and tested versus pending deployment status.
> Numeric ModuleId/OperationId examples must be labelled illustrative; UI must
> resolve current IDs through the existing authenticated menu/permission flow.

Navigation: [Calling flow](#user-and-api-calling-flow) · [Endpoint contract](#endpoints-8-per-master-32-bulk-routes-total) · [All request/response examples](#copyable-request-and-response-examples-for-every-bulk-action) · [EmployeeType examples](#employeetype-manual-creation-and-bulk-examples) · [Local/production setup](#local-and-production-deployment-same-api-project) · [Progress](#implementation-sequence-and-status).

## Highlighted deferred gaps — user decision 2026-09-11

> **DEFERRED — do later, not part of the completed base-import release.**
> User explicitly asked to retain these gaps in documentation for later work.
> Do not mark them COMPLETE or begin implementation/testing without a new request.

Original 15-gap review: **13 addressed within bulk-import scope; 2 design
limitations remain.** This does not claim every separate manual onboarding flow
was changed. FINAL release acceptance refers to the approved base-import scope.

| Remaining item | Current behavior / evidence | Status |
| --- | --- | --- |
| Original gap 9: same email across multiple tenants | Login email remains globally unique; same-email multi-tenant accounts are not supported. Business/identity rules must be agreed before changing this. | DEFERRED — design + implementation |
| Original gap 10: employees without official email | OfficialEmail remains mandatory; email-less onboarding is not supported. Alternative login/onboarding rules must be agreed first. | DEFERRED — design + implementation |
| Actual welcome-invitation email delivery | Invitation tracking, separate dispatch/retry and automated coverage exist; no real invitation email was sent during live acceptance. | DEFERRED — live delivery test, NOT a pass |
| Reporting-manager bulk mapping | Future assignment scope, including missing manager/self-reporting/cycle rules. | DEFERRED — next bulk scope |
| Location/work arrangement and policy bulk assignments | Existing operational flows require dependency/rule review before bulk support. | DEFERRED — next bulk scope |
| Attendance-device bulk enrollment | Employee account creation does not imply device enrollment. | DEFERRED — next bulk scope |

### Device deployment follow-on request recorded 2026-09-13

The user requested future support for: bulk enrolling existing Employee IDs onto
devices, optionally deploying successfully created Employee bulk-import rows to
selected devices, and automatically disabling/reactivating enrolled device users
when the HRMS Employee active status changes. These are **PENDING requirements**,
not implemented behavior and not authorization to invent target-device/location,
credential, offline retry or reactivation rules. The proposed API/job inventory and
unresolved decisions are documented in
`docs/UIdeveloperDoc/DeviceRelatedAPI/DEVICE_RELATED_API_COMPLETE_HANDOFF.md`,
section 22. Implementation must remain a separate durable post-commit workflow so
device availability cannot roll back Employee creation.

UI work belongs to the separate UI developer and is excluded from this backend gap
count. Existing passed tests need not be repeated merely to revisit this list.

## Approved decisions and current scope

- Department, Designation, Role and EmployeeType now support CSV/XLSX/paste, manual mapping,
  saved preview, explicit confirmation, durable background execution, job history,
  retry, cancellation, templates and result reports.
- **AI is disabled by user decision.** No provider credentials/calls are required.
  Natural-language extraction, PDF/images and AI mapping are deferred.
- Each Designation belongs to a Department. The user confirmed that Manager can
  exist independently in IT, HR and Sales. Identity is tenant + department +
  trimmed, case-insensitive designation name. Department and Role names are unique
  per tenant among non-deleted records.
- Import creates missing masters and skips matching active records. It does not
  update descriptions, reactivate records, assign employees, grant permissions or
  replace the defaults created during tenant onboarding.
- Department must finish before Designation referencing it is previewed. Role is
  independent. Missing parents require correction; they are not silently created.
  IT and Information Technology are not automatically treated as the same name.
- Tenant/actor come from authenticated context. Existing module permission
  behaviors authorize HTTP commands. The worker rechecks the same persisted
  permission function per batch. No Host bypass is introduced.
- Angular bulk screens are not implemented in this backend task. This document
  supplies their API contract and integration sequence.
- EmployeeType manual CRUD and deletion dependency rules are maintained in
  `docs/EmployeeTypeRules.md`. EmployeeTypeBasicMenu has no soft-delete columns;
  any mapping row blocks EmployeeType soft deletion. No dependency cascade or hard delete is permitted.

## User and API calling flow

1. Open the master and obtain ModuleId/OperationId through the existing tenant
   menu/permission flow. Optionally download its CSV template.
2. Select CSV/XLSX or paste an Excel table. Choose the worksheet when needed.
   Map source headers to supported fields. Canonical headers match automatically;
   arbitrary names such as Dept require explicit mapping.
3. POST preview. Render global errors, source row numbers, mapped values, existing
   matches and row errors. Preview persists a draft; no master records are created.
4. Enable Confirm only when data.canCommit is true. Any invalid row prevents the
   entire draft from being confirmed. Correct the input/mapping and submit a new
   preview with a new RequestId; there is no in-place draft/row-edit endpoint.
5. Show final counts and execution choices: **Run now in background** or
   **Schedule for later**. Wait for explicit user confirmation.
6. POST confirm with the saved JobId. HTTP 200 means accepted/already confirmed,
   not that all rows have been inserted.
7. Poll job detail every 2-5 seconds until terminal status; back off for network
   errors. Closing the page leaves server work running. Restore through history.
8. Show Created / Existing / Failed counts and errors, download report, retry
   eligible failures or upload corrected data. Refresh the existing master list.
   Complete Department before moving to its Designations.

Both execution choices use the background worker. Run now is eligible at the
next poll; scheduled time is a not-before time, not a completion deadline.
There is **no automatic traffic measurement or low-traffic detector**. For off-peak
execution, let the user select a local time and convert it to UTC before sending.

## Endpoints: 8 per master, 32 bulk routes total

Bases: `/api/Department`, `/api/Designation`, `/api/Role`, `/api/EmployeeType`.
EmployeeType additionally has `/add`, `/get`, `/option`, `/update`, and `/delete`
(37 current-phase routes including those five manual CRUD routes).
All routes require authentication and ModuleId + OperationId. Use real tenant-menu
IDs; operation IDs are not necessarily equal to operation-type enum values.
Mutations need an active Add or Import operation and its existing grant. Reads
also accept a granted View operation. Wrong-module and denied requests are rejected
by existing permission behaviors before the handler executes.

| Method | Suffix | Input | Successful result |
| --- | --- | --- | --- |
| POST | /bulk/preview | multipart form | ApiResponse with saved preview |
| POST | /bulk/confirm | JSON job request | ApiResponse with queued/current job |
| GET | /bulk/jobs/{jobId} | permission query | ApiResponse with job and complete row results |
| GET | /bulk/jobs | permission query + pageNumber/pageSize | ApiResponse with newest-first summary array |
| POST | /bulk/retry | JSON job request | ApiResponse with requeued job |
| POST | /bulk/cancel | JSON job request | ApiResponse with current/cancelled job |
| GET | /bulk/template | permission query | Raw CSV header template |
| GET | /bulk/jobs/{jobId}/report | permission query | Raw CSV row-result report |

Job access is scoped to **the same tenant and uploading actor**, not every HR user
in the tenant. A foreign/missing job returns 404. Detail/report use the route JobId.
History defaults to pageNumber=1/pageSize=20; pageSize must be 1-100. Its data is
an array with preview=null in each summary; no total-count pagination metadata
is supplied. Request subsequent pages until one is shorter than pageSize.

## Upload and mapping contract

| Preview form field | Meaning |
| --- | --- |
| ModuleId, OperationId | Required existing permission identifiers |
| File | One .csv or .xlsx; supply File OR PastedText |
| PastedText | Header plus comma/tab-delimited rows; alternative to File |
| RequestId | Optional client UUID; recommended for retrying an unchanged upload |
| SheetName | Exact XLSX sheet name; required for multiple-sheet workbooks |
| ColumnMappingJson | JSON string mapping canonical target field to source header |

The same RequestId with the same source bytes/text, sheet and mapping returns its
saved preview. Changed input with that ID returns 409. Use a new UUID when correcting
anything. Confirmation accepts no replacement rows. Plain JSON row-array ingestion
is not implemented; JSON is used for actions and the column mapping.

| Master | Required columns | Optional columns |
| --- | --- | --- |
| Department | DepartmentName | Description, Remark, IsActive |
| Designation | DesignationName, DepartmentName | Description, IsActive |
| Role | RoleName, RoleType | Remark, IsActive |
| EmployeeType | TypeName | Description, Remark, IsActive |

IsActive accepts true/false; blank/absent means true. Department/Designation names
allow 255 characters, RoleName 100, Description 500 and Remark 200. For EmployeeType,
TypeName, Description and Remark each allow 255 characters. RoleType follows
existing ConstantValues: Admin=1, Employee=2, Manager=3. It is a type identifier,
not a grant of permissions. Arbitrary new RoleTypes cannot be imported.

Audit IDs, TenantId, primary keys and system-default flags are server-assigned;
do not expose them as mapped fields. Optional text may remain empty/null. This
master-import contract is separate from the earlier SQL module-seed request.

Limits: 5 MiB file, 25 MiB expanded XLSX, 5,000 data rows, 64 columns, 4,000
characters per cell, 1,000 ZIP entries. CSV must be valid UTF-8. Headers must exist
and be unique; row widths must agree. Formulas, merged cells, external worksheet
relationships and Excel error cells are rejected. Export formula results as
values first. Legacy .xls, macro workbooks, PDF and images are unsupported.

Names are trimmed/case-insensitive. Duplicate source identities invalidate all
involved rows. Inactive/ambiguous existing records are errors. Existing active
records are skipped even when optional descriptions differ; an existing Role with
a different RoleType is an error. Designation needs one active same-tenant parent.

Department sample:

```csv
DepartmentName,Description,Remark,IsActive
IT,Information technology,Initial import,true
HR,Human resources,Initial import,true
```

After those Departments exist, Designation sample:

```csv
DesignationName,DepartmentName,Description,IsActive
Manager,IT,IT team manager,true
Manager,HR,HR team manager,true
IT-Manager,IT,Technical operations,true
```

Role sample:

```csv
RoleName,RoleType,Remark,IsActive
Support Executive,2,Imported employee role,true
Area Manager,3,Imported manager role,true
```

For source headers Job Title, Dept, Enabled, ColumnMappingJson is:

```json
{"DesignationName":"Job Title","DepartmentName":"Dept","IsActive":"Enabled"}
```

Unknown targets and reusing one source column for multiple targets are rejected.
Unmapped columns are ignored; make this visible in the UI. The saved preview includes
its mapping. The UI can load an older job and offer its mapping when the current
headers match. No separate mapping-profile endpoint or automatic cross-upload
mapping selection is implemented.

## Preview response and confirmation

Use the existing ApiResponse envelope: isSucceeded, message, data, errors and
optional metadata. Row-validation errors may be inside a successful preview;
inspect canCommit, not only HTTP status/isSucceeded. JSON enums are numeric.
Canonical dictionary keys retain their field spelling.

Illustrative preview (optional envelope metadata omitted):

```json
{
  "isSucceeded": true,
  "message": "Draft saved. Review and confirm to queue import. No master records have been created.",
  "errors": [],
  "data": {
    "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
    "master": 1,
    "sourceColumns": ["DepartmentName"],
    "columnMapping": {"DepartmentName":"DepartmentName"},
    "errors": [],
    "rows": [{
      "processed": false,
      "rowNumber": 2,
      "values": {"DepartmentName":"IT"},
      "status": 1,
      "existingId": null,
      "departmentId": null,
      "errors": []
    }],
    "readyCount": 1,
    "existingCount": 0,
    "invalidCount": 0,
    "isValid": true,
    "confirmationAvailable": true,
    "canCommit": true
  }
}
```

Master enum: Department=1, Designation=2, Role=3, EmployeeType=4. A parsed invalid preview can
have JobId with canCommit=false. Parsing/mapping syntax errors may instead return
400 without a saved draft. Missing required mappings appear as preview errors.
An existing-only valid preview can be confirmed and finishes with skipped rows.

POST `/api/Department/bulk/confirm` (replace illustrative permission IDs from menu):

```json
{
  "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
  "moduleId": 25,
  "operationId": 1,
  "scheduledAtUtc": null
}
```

For later execution use a future ISO-8601 UTC timestamp; a past time returns 400.
Repeating Confirm on a non-draft returns its current state without changing the
schedule or creating another job. Retry accepts the same shape. Cancel needs only
jobId, moduleId and operationId.

## Progress, retry, cancellation and download

Job fields: jobId, master, status, createdAtUtc, updatedAtUtc, scheduledAtUtc,
totalRows, processedRows, createdCount, existingCount, failedCount, error, preview.
Detail/actions include preview/results; history contains summaries. Times are UTC.

| Job status | Value | UI action |
| --- | --- | --- |
| Draft | 1 | Review; confirm when canCommit; cancel |
| Queued | 2 | Waiting for time/worker; poll or cancel |
| Running | 3 | Processing batches; poll or cancel |
| Completed | 4 | Stop polling; show report/results |
| CompletedWithErrors | 5 | Stop polling; show failures; retry/new upload |
| Failed | 6 | Stop polling; show job error; resolve cause then retry |
| Cancelled | 7 | Stop polling; show already committed records |

Row status: Ready=1, Existing=2, Invalid=3, Created=4, Failed=5. The processed flag
distinguishes a preview match from a processed row. Job existingCount counts only
processed matches; preview existingCount also includes initial matches. Use
processedRows/totalRows for progress; failed attempts count as processed.

Retry is allowed only for Failed/CompletedWithErrors. It requeues the saved data
and refreshes the current actor's permission identifiers. Created rows are never
inserted again. Failed/pending rows are revalidated; progress may decrease as failed
rows reset for retry. If a matched Department was replaced after preview, create
a new preview to approve the new parent. Changed values require a new upload.

Cancel takes effect at a batch boundary. A currently locked batch may finish first;
committed rows remain. Cancel is not undo. Cancelled jobs cannot be retried; a new
preview will match/skip already created records.

Template/report routes return text/csv files, not ApiResponse JSON. Download as
Blob and handle JSON error responses through existing frontend error handling.
Report columns: RowNumber, Status (text), RecordId, Values (JSON cell), Errors.
All rows are included with CSV quoting and spreadsheet formula protection. The
report is for review; it is not directly re-importable as a template.

## Frontend request example

Use existing authentication/interceptors/error handling. Let the browser set the
multipart Content-Type boundary. This is contract pseudocode, not a frontend change.

```typescript
const base = '/api/Department';
const requestId = crypto.randomUUID(); // keep for retrying unchanged input
const form = new FormData();
form.append('ModuleId', String(moduleId));
form.append('OperationId', String(addOrImportOperationId));
form.append('RequestId', requestId);
form.append('File', selectedFile); // OR PastedText; never both
form.append('ColumnMappingJson', JSON.stringify({ DepartmentName: 'Dept' }));
// form.append('SheetName', selectedSheetName); // when applicable
const preview = await firstValueFrom(http.post<ApiResponse<BulkPreview>>(
  `${base}/bulk/preview`, form));

// Render preview/errors. Wait for explicit user confirmation.
// Only confirm when preview.data.canCommit is true.
const job = await firstValueFrom(http.post<ApiResponse<BulkJob>>(
  `${base}/bulk/confirm`, {
    jobId: preview.data.jobId,
    moduleId,
    operationId: addOrImportOperationId,
    scheduledAtUtc: selectedLocalDate ? selectedLocalDate.toISOString() : null
  }));

const params = { moduleId, operationId: readOperationId };
// Poll until status is 4, 5, 6 or 7; unsubscribe on page close.
const progress = await firstValueFrom(http.get<ApiResponse<BulkJob>>(
  `${base}/bulk/jobs/${job.data.jobId}`, { params }));
const report = await firstValueFrom(http.get(
  `${base}/bulk/jobs/${job.data.jobId}/report`, {
    params,
    responseType: 'blob'
  }));
```

Build UI types from the DTO fields above. Refresh existing master `/get` and
`/option` endpoints with their current permissions and request contracts.

## Durable background configuration and deployment

Apply to the intended database, in order, before serving bulk routes:

1. `database-scripts/EnforceDesignationDepartmentScope.sql`
2. `database-scripts/AddDurableMasterBulkImport.sql`
3. `database-scripts/AddTenantEmployeeTypes.sql`
4. `database-scripts/SeedTenantEmployeeTypeModule.sql`

All four scripts are transactional and rerunnable. The first rejects missing/cross-tenant
Department references or duplicate scoped Designations. The second adds the durable
queue and live-name unique indexes for Department/Role. Conflicting existing data
requires explicit review; no silent merge/delete/reset occurs. Back up the intended
database and use `psql -v ON_ERROR_STOP=1 -f <script>` during deployment.

Deploy the updated API with its existing database connection configured securely.
The worker uses ConnectionStrings:DefaultConnection, the same database as the API.
No broker, separate scheduler or AI provider is required. Enable after migration:

```powershell
$env:BulkImport__WorkerEnabled = 'true'
$env:BulkImport__PollIntervalSeconds = '5'
$env:BulkImport__BatchSize = '50'
$env:BulkImport__BatchTimeoutSeconds = '60'
# Configure existing ConnectionStrings__DefaultConnection securely for this host.
```

| Setting | Default | Range / effect |
| --- | --- | --- |
| WorkerEnabled | true in API appsettings.json | Enabled for local/production through inherited API configuration; can override to false |
| PollIntervalSeconds | 5 | 1-300; one batch attempted per tick |
| BatchSize | 50 | 1-200 rows per transaction |
| BatchTimeoutSeconds | 60 | 5-300; interrupted batch rolls back |

Settings are host-wide, validated at startup; restart after configuration changes.
Disabled workers leave jobs queued. Use an always-on API/service host: a sleeping
IIS application or scale-to-zero host cannot process until it wakes. Multiple
instances are supported using PostgreSQL FOR UPDATE SKIP LOCKED. No per-tenant
worker settings are implemented.

The skolenza.BulkImportJob table stores tenant/actor, permission identifiers,
source hash, normalized preview JSON, status, schedule, cursor and audit timestamps.
Raw workbooks are not retained. Drafts/history persist; automatic retention/purge
is not implemented.

Each batch locks a due job, rechecks permissions/current masters and commits master
inserts plus job progress in one transaction. Row savepoints isolate expected
constraint/reference failures. A tenant/master advisory lock serializes competing
imports; unique indexes also protect concurrent manual creation. A crash rolls back
the uncommitted batch and a new worker scope resumes from committed progress.
Designation parent references are rechecked/locked before insertion.

Expected row failures yield CompletedWithErrors; permission loss yields Failed.
Infrastructure errors/timeouts roll back, are logged and retry on the next poll.
**There is no retry-attempt ceiling, dead-letter queue or persisted infrastructure
error detail.** Persistent database/configuration errors can leave a job queued or
running with unchanged updatedAtUtc; operators must inspect worker logs and fix
the cause. Automated alerting/traffic-aware scheduling are not implemented.
Worker configuration is supplied in this API project. Development and Production
use their corresponding API connection settings. The migration runner has been
executed against the isolated test database only; no external production deployment
or production database mutation is claimed.

## Errors and frontend acceptance

Existing error envelope: 400 invalid input/schedule; 401 invalid login; 403 wrong
module/action or denied permission; 404 inaccessible job; 409 changed-source
RequestId reuse or ineligible Retry. Oversized requests can return server/proxy
413. Unexpected errors use existing middleware. Confirm HTTP 200 is not an all-rows
success signal.

Frontend acceptance after integration must cover upload/paste/manual mapping,
multi-sheet selection, disabled confirmation for invalid rows, same Manager in
different Departments, confirmation network retry, scheduling with UTC conversion,
page refresh/close, partial failure/retry/cancel/report, stale parents and denied
permissions. Those authenticated HTTP and Angular end-to-end checks remain PENDING.
Backend route contracts, permission behaviors, real database and hosted-worker
execution are covered by automation below.

## Implementation sequence and status

| Sequence | Work | Status |
| --- | --- | --- |
| 1 | Save user rules and continuing reference | COMPLETE |
| 2 | Inspect Department/Designation/Role ownership and flows | COMPLETE |
| 3 | Department-scoped Designation validation and DB constraint | COMPLETE |
| 4 | CSV/XLSX/paste, manual mapping and scoped preview | COMPLETE |
| 5 | Durable draft, RequestId replay and explicit confirmation | COMPLETE |
| 6 | Run-now/scheduled worker, resume and permission recheck | COMPLETE |
| 7 | Job history/detail, Retry, Cancel, templates and reports | COMPLETE |
| 8 | Current backend automation and consolidated UI handoff | COMPLETE |
| 9 | Angular screens and authenticated end-to-end acceptance | UI DEVELOPER RESPONSIBILITY — backend handoff complete |
| 10 | AI assistance | DEFERRED — user explicitly disabled |
| 11 | Tenant-owned EmployeeType CRUD, bulk, migration, onboarding | COMPLETE |
| 11a | Environment-specific worker configuration, migration runner and release packaging | COMPLETE — tested with isolated database |
| 12 | Applicable location/policy definitions and assignment flows | PENDING — verify dependencies/rules |
| 13 | Employee bulk preview/import using existing onboarding rules | PENDING |
| 14 | Manager/role/policy assignments, invitations/device enrollment as applicable | PENDING — verify module-specific rules |

Next backend work, after this phase, is the applicable location/policy dependency
analysis. Existing RoleType constants remain authoritative. Preserve the initial
employee/super-admin created by host tenant creation and self-registration; verify
duplicate/login/invitation rules before Employee import. Do not treat future
proposals or unresolved rules as authorization to invent new behavior.

### EmployeeType: approved ownership and implemented migration

User confirmed that each tenant creates EmployeeTypes according to its needs.
The application does not prepopulate all six global types for every tenant.

- Existing EmployeeType rows referenced by employees/history/policies are copied
  for their owning tenants and those references are remapped transactionally.
- Employee and old/new type-history references use Employee.TenantId; allowance
  mappings use Designation.TenantId; leave/unstructured mappings use their TenantId.
  Associated legacy menu rows are copied with the referenced types. Unresolvable
  or cross-tenant references fail the migration instead of being guessed.
- Legacy null-tenant catalogue rows remain only for bootstrap/history compatibility;
  tenant list/options/preview never expose them. Tenant-owned type names are unique
  after trim/case normalization. Database triggers reject cross-tenant assignments
  and moving a type's ownership.
- Existing super-admin onboarding is preserved using only one tenant-owned
  Permanent type. CreateTenant resolves its new ID instead of assigning shared ID=1.
  Other types are explicitly created/imported by the tenant. No custom type silently
  acquires permission grants or legacy menu assignments.
- `/api/EmployeeType/get` now reads the database instead of a hard-coded list.
  `/option` returns active, non-deleted types for the current tenant. Both now need
  ModuleId/OperationId; update existing employee/tenant form lookup calls accordingly.
- `/add`, `/get`, and `/option` provide manual create/read behavior; `/update` and `/delete`
  complete the manual CRUD surface. Delete is a guarded soft delete; its dependency rules
  are maintained in `docs/EmployeeTypeRules.md`.
- Existing Employee create/update handlers verify active same-tenant EmployeeType.
  Permission authorization stays in the existing pipeline. Database checks also
  cover old policy/reference writers during tenant-specific migration.
- Module code: EMPLOYEE_TYPE, a direct leaf child of EMP_MGMT, with
  Add/Update/Delete/View/Import/Export operations. Bulk endpoints use the Import
  mapping. Plan coverage inherits from EMP_LIST. The seed does not directly insert role grants.
  Existing tenants use their established entitlement synchronization and role
  permission assignment flow. New onboarding uses the existing plan/module setup.
- RoleType is unchanged: Admin=1, Employee=2, Manager=3. There is no arbitrary
  RoleType bulk-creation endpoint.

## Automated validation record

Tests are in existing skolenza.automationtests/Unit: BulkImportPreviewTests,
BulkImportPermissionTests, DesignationDepartmentDatabaseTests and
DurableBulkImportDatabaseTests. Real database tests use an isolated workforce-backup
clone named skolenza_bulk_test, all four migrations/seeds, persisted permission checks and
existing AutoMapper. Coverage includes actual writes, separate worker scopes,
hosted-worker startup, parallel workers, confirmation replay, scheduled execution,
interrupted-transaction recovery, changed parents/permissions, retry and cancel.

```powershell
# Set SKOLENZA_BULK_TEST_CONNECTION to the isolated skolenza_bulk_test database.
dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-restore --disable-build-servers --filter 'Category=BulkImport|Category=BulkImportDatabase' --logger 'trx;LogFileName=bulk-import.trx' --results-directory artifacts/bulk-import
dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-build --no-restore --filter 'FullyQualifiedName~RecentDeviceEmployeeApiContractTests|FullyQualifiedName~TenantDeviceConfigurationPermissionBehaviorTests' --logger 'trx;LogFileName=bulk-regression.trx' --results-directory artifacts/bulk-import
dotnet build Skolenza.sln --no-restore --disable-build-servers
```

2026-09-10 final current-phase validation:

- **88 bulk tests passed, 0 failed, 0 skipped**, including tenant EmployeeType
  creation, isolation, duplicate handling, onboarding, cross-tenant DB rejection,
  durable import and environment-selected migration replay.
- **119 Device/Employee/permission regression tests passed**, no failures/skips.
- Release publish succeeded. All four SQL files and the migration runner are
  present in artifacts/publish/skolenza.api/database-scripts.
- The runner was also executed successfully from the published package using
  Production selection with the connection explicitly overridden to the isolated
  test database. Repeated migrations created no duplicate types/modules.
- Build/publish retain existing warnings. No external production database was
  modified and Angular UI end-to-end execution is owned by the UI integration phase.

Earlier baseline evidence (superseded by the expanded result above):

- Final expanded bulk suite: **71 passed, 0 failed, 0 skipped**, including template
  contracts, report escaping/formula protection and actor-scoped history.
- Device/Employee and permission regression suite: **119 passed, 0 failed, 0 skipped**.
- Both migrations successfully reapplied on the isolated database.
- Full solution build: **0 errors, 10 existing warnings** on the final incremental
  build (including existing dependency vulnerability warnings).
- Initial final-run attempt had 47 passes and 19 database failures because the
  isolated PostgreSQL service was stopped. Restarting it and rerunning produced
  a 66-pass result; five additional template/report/history checks then brought the
  suite to 71 passes. Infrastructure failures were not counted as passes.
- Results: artifacts/bulk-import/bulk-import.trx and bulk-regression.trx.
- Missing fixture configuration is explicitly skipped, never a pass. Durable tests
  clean their uniquely named committed jobs/records; designation tests roll back.
  No live production data was modified.

## Local and production deployment: same API project

WorkerEnabled=true is now supplied in skolenza.api/appsettings.json. Standard
ASP.NET Core configuration chooses appsettings.Development.json locally and
appsettings.Production.json in Production, with environment overrides taking
precedence. No separate background-service project or server is required.

The release includes these four SQL files and ApplyBulkImportMigrations.ps1 under
its database-scripts directory. The runner works from the repository and published
API output. PowerShell 7+ and PostgreSQL psql are required. It resolves the selected
API connection, including Development user secrets when running from the source
project and ConnectionStrings__DefaultConnection. For other custom configuration
providers, supply the same connection environment override to both runner and API.
It never prints passwords/connection strings; -ValidateOnly makes no DB connection.

Local sequence, from repository root:

```powershell
# Optional read-only configuration check:
pwsh -NoProfile -File database-scripts/ApplyBulkImportMigrations.ps1 -Environment Development -ValidateOnly
# Apply to the Development connection (or its explicit environment override):
pwsh -NoProfile -File database-scripts/ApplyBulkImportMigrations.ps1 -Environment Development -PsqlPath 'C:/Program Files/PostgreSQL/18/bin/psql.exe'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project skolenza.api --no-launch-profile
```

Production release sequence:

```powershell
dotnet publish skolenza.api/skolenza.api.csproj -c Release -o artifacts/publish/skolenza.api
# Transfer the published release through the existing deployment process.
# On its host, from the published API directory with the intended connection configured:
pwsh -NoProfile -File database-scripts/ApplyBulkImportMigrations.ps1 -Environment Production -PsqlPath 'C:/Program Files/PostgreSQL/18/bin/psql.exe'
# Configure the existing IIS/service host environment as Production, then restart it.
```

Drain/stop the old API and its worker before the EmployeeType ownership migration,
then migrate and start the new version. The old version must not serve unscoped
EmployeeType lists or write shared IDs after the schema/data change. Do not start
the new API against an unmigrated schema. Migrations deliberately run
as a deployment command, not silently from API startup. Each SQL file is atomic;
if one fails, later files are not run. Earlier successful files remain applied and
are safe to rerun after correcting the reported problem. Take the normal database
backup before migration. Do not substitute the broad production module seed, which
contains unrelated reset operations.

For existing tenants, Host uses the existing POST
/api/Tenant/sync-active-plan-entitlements with its encrypted TenantId and current
Host permission context. Then grant EMPLOYEE_TYPE through the existing
role-permission UI/API. This new seed creates module/operation/plan metadata only;
it does not bypass role authorization. Refresh login/menu/options after rollout,
particularly because old shared EmployeeType IDs have been remapped. A local or
production profile name in a test does not mean an external production DB was changed.

## Copyable request and response examples for every bulk action

The examples below show one-row Department imports. The same body/response shape
applies to Designation, Role and EmployeeType by changing the route base, master
value, input fields and actual module/operation IDs. The complete field contracts
and sample input for each master are above; EmployeeType examples follow below.
IDs and timestamps are illustrative, not deployment constants. Get/menu supplies
real permission IDs. Authentication is the existing Bearer/session mechanism.
Success messages for job actions are currently empty strings; show UI text based
on status instead of depending on a message string.

### 1. Preview: POST /api/Department/bulk/preview

Content-Type: multipart/form-data with browser-generated boundary. Example form:

```text
ModuleId: 25
OperationId: 1
RequestId: 974a3d15-a7a6-4969-a823-28709ec3a348
PastedText: DepartmentName
IT
```

PastedText contains a real newline. For Excel, replace PastedText with the File
binary field; optionally send SheetName and ColumnMappingJson. Never send both.
Response HTTP 200:

```json
{
  "isSucceeded": true,
  "message": "Draft saved. Review and confirm to queue import. No master records have been created.",
  "data": {
    "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
    "master": 1,
    "sourceColumns": [
      "DepartmentName"
    ],
    "columnMapping": {
      "DepartmentName": "DepartmentName"
    },
    "errors": [],
    "rows": [
      {
        "processed": false,
        "rowNumber": 2,
        "values": {
          "DepartmentName": "IT"
        },
        "status": 1,
        "existingId": null,
        "departmentId": null,
        "errors": []
      }
    ],
    "readyCount": 1,
    "existingCount": 0,
    "invalidCount": 0,
    "isValid": true,
    "confirmationAvailable": true,
    "canCommit": true
  },
  "errors": []
}
```

### 2. Confirm: POST /api/Department/bulk/confirm

Content-Type: application/json. Run-now request:

```json
{
  "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
  "moduleId": 25,
  "operationId": 1,
  "scheduledAtUtc": null
}
```

For scheduling, change scheduledAtUtc to a future UTC value. On confirmation
response the server sets status=2; no master row has necessarily been committed:

```json
{
  "isSucceeded": true,
  "message": "",
  "data": {
    "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
    "master": 1,
    "status": 2,
    "createdAtUtc": "2026-09-10T10:00:00Z",
    "updatedAtUtc": "2026-09-10T10:01:00Z",
    "scheduledAtUtc": "2026-09-10T10:01:00Z",
    "totalRows": 1,
    "processedRows": 0,
    "createdCount": 0,
    "existingCount": 0,
    "failedCount": 0,
    "error": null,
    "preview": {
      "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
      "master": 1,
      "sourceColumns": [
        "DepartmentName"
      ],
      "columnMapping": {
        "DepartmentName": "DepartmentName"
      },
      "errors": [],
      "rows": [
        {
          "processed": false,
          "rowNumber": 2,
          "values": {
            "DepartmentName": "IT"
          },
          "status": 1,
          "existingId": null,
          "departmentId": null,
          "errors": []
        }
      ],
      "readyCount": 1,
      "existingCount": 0,
      "invalidCount": 0,
      "isValid": true,
      "confirmationAvailable": false,
      "canCommit": false
    }
  },
  "errors": []
}
```

### 3. Progress/detail: GET /api/Department/bulk/jobs/{jobId}

```http
GET /api/Department/bulk/jobs/974a3d15-a7a6-4969-a823-28709ec3a348?moduleId=25&operationId=4
```

Example completed response, HTTP 200:

```json
{
  "isSucceeded": true,
  "message": "",
  "data": {
    "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
    "master": 1,
    "status": 4,
    "createdAtUtc": "2026-09-10T10:00:00Z",
    "updatedAtUtc": "2026-09-10T10:01:05Z",
    "scheduledAtUtc": "2026-09-10T10:01:00Z",
    "totalRows": 1,
    "processedRows": 1,
    "createdCount": 1,
    "existingCount": 0,
    "failedCount": 0,
    "error": null,
    "preview": {
      "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
      "master": 1,
      "sourceColumns": [
        "DepartmentName"
      ],
      "columnMapping": {
        "DepartmentName": "DepartmentName"
      },
      "errors": [],
      "rows": [
        {
          "processed": true,
          "rowNumber": 2,
          "values": {
            "DepartmentName": "IT"
          },
          "status": 4,
          "existingId": 501,
          "departmentId": null,
          "errors": []
        }
      ],
      "readyCount": 0,
      "existingCount": 0,
      "invalidCount": 0,
      "isValid": true,
      "confirmationAvailable": false,
      "canCommit": false
    }
  },
  "errors": []
}
```

### 4. History: GET /api/Department/bulk/jobs

```http
GET /api/Department/bulk/jobs?moduleId=25&operationId=4&pageNumber=1&pageSize=20
```

Example HTTP 200 response. An empty history returns data: []:

```json
{
  "isSucceeded": true,
  "message": "",
  "data": [
    {
      "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
      "master": 1,
      "status": 4,
      "createdAtUtc": "2026-09-10T10:00:00Z",
      "updatedAtUtc": "2026-09-10T10:01:05Z",
      "scheduledAtUtc": "2026-09-10T10:01:00Z",
      "totalRows": 1,
      "processedRows": 1,
      "createdCount": 1,
      "existingCount": 0,
      "failedCount": 0,
      "error": null,
      "preview": null
    }
  ],
  "errors": []
}
```

### 5. Retry: POST /api/Department/bulk/retry

Only status 5 or 6 is eligible. After fixing the cause, send:

```json
{
  "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
  "moduleId": 25,
  "operationId": 1,
  "scheduledAtUtc": null
}
```

Example response for a previously failed one-row job requeued now. If earlier rows
were already Created, their statuses/IDs and createdCount remain present:

```json
{
  "isSucceeded": true,
  "message": "",
  "data": {
    "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
    "master": 1,
    "status": 2,
    "createdAtUtc": "2026-09-10T10:00:00Z",
    "updatedAtUtc": "2026-09-10T10:01:00Z",
    "scheduledAtUtc": "2026-09-10T10:01:00Z",
    "totalRows": 1,
    "processedRows": 0,
    "createdCount": 0,
    "existingCount": 0,
    "failedCount": 0,
    "error": null,
    "preview": {
      "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
      "master": 1,
      "sourceColumns": [
        "DepartmentName"
      ],
      "columnMapping": {
        "DepartmentName": "DepartmentName"
      },
      "errors": [],
      "rows": [
        {
          "processed": false,
          "rowNumber": 2,
          "values": {
            "DepartmentName": "IT"
          },
          "status": 1,
          "existingId": null,
          "departmentId": null,
          "errors": []
        }
      ],
      "readyCount": 1,
      "existingCount": 0,
      "invalidCount": 0,
      "isValid": true,
      "confirmationAvailable": false,
      "canCommit": false
    }
  },
  "errors": []
}
```

### 6. Cancel: POST /api/Department/bulk/cancel

Request:

```json
{
  "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
  "moduleId": 25,
  "operationId": 1
}
```

Example response cancelling before the first batch. For a running job, committed
row counts can be non-zero and remain after cancellation:

```json
{
  "isSucceeded": true,
  "message": "",
  "data": {
    "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
    "master": 1,
    "status": 7,
    "createdAtUtc": "2026-09-10T10:00:00Z",
    "updatedAtUtc": "2026-09-10T10:01:00Z",
    "scheduledAtUtc": "2026-09-10T10:01:00Z",
    "totalRows": 1,
    "processedRows": 0,
    "createdCount": 0,
    "existingCount": 0,
    "failedCount": 0,
    "error": null,
    "preview": {
      "jobId": "974a3d15-a7a6-4969-a823-28709ec3a348",
      "master": 1,
      "sourceColumns": [
        "DepartmentName"
      ],
      "columnMapping": {
        "DepartmentName": "DepartmentName"
      },
      "errors": [],
      "rows": [
        {
          "processed": false,
          "rowNumber": 2,
          "values": {
            "DepartmentName": "IT"
          },
          "status": 1,
          "existingId": null,
          "departmentId": null,
          "errors": []
        }
      ],
      "readyCount": 1,
      "existingCount": 0,
      "invalidCount": 0,
      "isValid": true,
      "confirmationAvailable": false,
      "canCommit": false
    }
  },
  "errors": []
}
```

### 7. Template: GET /api/Department/bulk/template

```http
GET /api/Department/bulk/template?moduleId=25&operationId=4
```

HTTP 200, Content-Type text/csv; charset=utf-8. Download name Department-template.csv:

```csv
DepartmentName,Description,Remark,IsActive
```

Designation template: DesignationName,DepartmentName,Description,IsActive.
Role template: RoleName,RoleType,Remark,IsActive.
EmployeeType template: TypeName,Description,Remark,IsActive.
The download contains headers only; use existing lookup APIs for reference values.

### 8. Report: GET /api/Department/bulk/jobs/{jobId}/report

```http
GET /api/Department/bulk/jobs/974a3d15-a7a6-4969-a823-28709ec3a348/report?moduleId=25&operationId=4
```

HTTP 200, Content-Type text/csv; charset=utf-8, UTF-8 BOM; download name
Department-import-974a3d15-a7a6-4969-a823-28709ec3a348.csv. Example body:

```csv
RowNumber,Status,RecordId,Values,Errors
2,Created,501,"{""DepartmentName"":""IT""}",""
```

### Invalid preview and corrections

A preview with row errors still returns isSucceeded=true if the preview was
successfully parsed/saved. For a blank required name, that row has status=3,
errors=["DepartmentName is required."], invalidCount=1, isValid=false and
canCommit=false. Display global data.errors and each row.errors separately.
A required parent mismatch in Designation identifies the source row and requires
creating/selecting an active Department before a NEW preview. Do not submit Confirm
and hope that invalid rows will be ignored.

A request-level 400/401/403/404/409 is different: use existing central error handling.
Do not parse it as a job or advance the wizard. Refresh authentication for 401;
check permissions for 403; reload/reselect a job for 404; new RequestId or eligible
job status is needed for 409. A failed background job is returned by successful
GET with job status=6 and job.error, not necessarily by an HTTP error.

## EmployeeType manual creation and bulk examples

Use the actual module ID whose code is EMPLOYEE_TYPE. The examples use
moduleId=80 only as an illustration; never hard-code it. /get and /option now need
permission query parameters; UserEmployeeId/TenantId cannot select another tenant.

### POST /api/EmployeeType/add

Request:

```json
{
  "moduleId": 80,
  "operationId": 1,
  "typeName": "Seasonal",
  "description": "Seasonal workers",
  "remark": "Defined by tenant HR",
  "isActive": true
}
```
HTTP 200 response (the response DTO retains legacy BaseRequest fields; ignore its row-level paging/sort fields):

```json
{
  "isSucceeded": true,
  "message": "EmployeeType created.",
  "data": {
    "id": 601,
    "typeName": "Seasonal",
    "description": "Seasonal workers",
    "remark": "Defined by tenant HR",
    "isActive": true,
    "pageNumber": 0,
    "pageSize": 0,
    "sortBy": null,
    "sortOrder": "desc",
    "userEmployeeId": null
  },
  "errors": []
}
```

Duplicate active/non-deleted names in the same tenant return 409 (trim/case
insensitive); another tenant may use Seasonal independently. Inactive records
still reserve their names. This is Add, so View/Import alone cannot call /add;
bulk mutations accept granted Add or Import as described above.

### GET /api/EmployeeType/get

```http
GET /api/EmployeeType/get?moduleId=80&operationId=4&pageNumber=1&pageSize=20
```

HTTP 200 response includes inactive non-deleted records too. Use envelope paging:

```json
{
  "isSucceeded": true,
  "message": "Employee types fetched successfully.",
  "data": [
    {
      "id": 601,
      "typeName": "Seasonal",
      "description": "Seasonal workers",
      "remark": "Defined by tenant HR",
      "isActive": true,
      "pageNumber": 0,
      "pageSize": 0,
      "sortBy": null,
      "sortOrder": "desc",
      "userEmployeeId": null
    }
  ],
  "errors": [],
  "pageNumber": 1,
  "pageSize": 20,
  "totalRecords": 1,
  "totalPages": 1
}
```

### GET /api/EmployeeType/option

```http
GET /api/EmployeeType/option?moduleId=80&operationId=4
```

HTTP 200 response contains only active, non-deleted tenant types:

```json
{
  "isSucceeded": true,
  "message": "Employee types fetched successfully.",
  "data": [
    {
      "id": 601,
      "typeName": "Seasonal"
    }
  ],
  "errors": []
}
```

An empty options list is a successful data: []; show Create EmployeeType/import
when allowed, not a fake shared fallback list.

### POST /api/EmployeeType/bulk/preview

Multipart request:

```text
ModuleId: <EMPLOYEE_TYPE module ID from menu>
OperationId: <granted Add or Import operation ID>
RequestId: <new UUID retained for unchanged-request network retry>
File: employee-types.xlsx (binary)
SheetName: EmployeeTypes
ColumnMappingJson: {"TypeName":"Employment category","Description":"Notes"}
```

Alternatively paste/use a CSV:

```csv
TypeName,Description,Remark,IsActive
Seasonal,Seasonal workers,HR-defined,true
Apprentice,Training programme,HR-defined,true
```

The preview response has master=4, values keys TypeName/Description/Remark/IsActive,
and the same saved-preview fields illustrated above. Existing Seasonal is status=2
with its ID; new Apprentice is status=1 with existingId=null. Duplicate uploaded
rows or invalid fields are status=3. Confirm that JobId using
/api/EmployeeType/bulk/confirm. Progress, history, retry, cancel, template and report
use /api/EmployeeType with the exact shared suffixes/contracts above.

On Department/Designation/Role screens, existing lookup refresh calls remain as
before. On Employee forms, load tenant EmployeeType options and submit their current
IDs; shared IDs from before migration must not remain in browser caches.

## Header-alias clarification and next target validation

User requested behavior confirmation/documentation before the next deployment
instruction. No actual target migration or API restart was executed in this review.
The full continuing validation matrix is now saved in
[automation README](../skolenza.automationtests/README.md#bulk-validation-checklist-read-before-every-bulk-import-session).
Read it in subsequent sessions alongside this document.

DesignationName, designation name and Designation_Name auto-map after header
normalization. DesName, design name and DesignationType require explicit manual
mapping, for example {"DesignationName":"DesName","DepartmentName":"Dept"}.
A source called DesignationType is not assumed to mean a designation name; inspect
its values before mapping. AI/semantic guessing remains disabled. Missing required
mapping prevents confirmation. Mapped names are checked against the tenant's active
Department and existing scoped Designations; existing matches are skipped.

Filename is not a module selector: any .xlsx/.csv name is allowed, while the endpoint
selects the master. Raw uploads are not saved as renamed documents; BulkImportJob
stores normalized rows, mappings and results. This handoff remains named
AI_ASSISTED_BULK_IMPORT_REFERENCE.md and test instructions remain in
skolenza.automationtests/README.md.

The new alias/canonical-header tests exercise CSV, XLSX and pasted tables against
controlled master fixtures. They do not substitute for the next authenticated
actual-target database and deployed-API acceptance run. Preview cannot guarantee
that no later concurrent change occurs; the worker revalidates, constraints protect
identities and row/job results expose conflicts.

2026-09-10 header clarification validation: **18 focused tests passed, 0 failed,
0 skipped** (3 custom aliases + 3 canonical spellings, each across CSV/XLSX/paste).
Custom aliases failed without mapping and matched the existing scoped Manager record
after explicit mapping; canonical headers auto-matched. Artifact:
artifacts/bulk-import/bulk-header-mapping.trx. Prior full backend result remains
88 bulk + 119 regression passes; that full suite was not rerun for this test/doc-only
change. Actual target migration/restart and authenticated acceptance remain pending.

### Target acceptance follow-up (2026-09-10)

- User authorized completing the current four-master scope, migration/restart,
  remaining tests and documentation before moving to another bulk module.
- User instructed that already tested cases must not be rerun. Existing TRX
  artifacts are retained as evidence; no previously passed suite was rerun.
- Development and Production migration-runner `-ValidateOnly` checks succeeded.
  Both resolve to the same remote Render database, `workforcedb_34hi_duis`.
  These checks do not connect to the database or apply migrations.
- Target acceptance is WIP, blocked on identifying the intended API URL/service
  host and authorized test tenant/account. No local dotnet/API/IIS worker process
  or SKOLENZA live-test environment settings were found in this session.
- Migration/restart approval is already granted; do not request it again.
  Resolve the target host/environment before backup, draining its worker,
  migration and restart. Authenticated upload/preview/confirm/report and persisted
  row verification remain PENDING, not passed. No target database was changed.

### Production execution (2026-09-10, supersedes the blocker above)

- Identified live API: https://skolenza-api.onrender.com. Render Production service:
  `srv-d6n5vpi4d50c73d9urn0`, repository `QuecksilberTechnologies/Skolenza.be`,
  branch `main_branch`. User signed in to Render and authorized continuation.
- COMPLETE: PostgreSQL custom-format backup saved outside the repository at
  `C:/Users/qtech/AppData/Local/Skolenza/Backups/workforcedb-pre-bulk-20260910-205535.dump`
  (788002 bytes). Archive listing and full archive read succeeded; a database
  restore was not performed. SHA256:
  `E793375F4BD7A7A21BF300D6EC7C953A76E34ABB942DD4CDF93891F059D7D02C`.
- COMPLETE: read-only target preflight found zero missing/foreign Designation
  parents and zero duplicate identity groups for Designation, Department and Role.
- COMPLETE: Render displayed Suspended before migrations. All four scripts ran
  through the Production migration runner and committed successfully. EmployeeType
  migration updated two Employee references; no test bulk records were inserted.
- COMPLETE: post-migration reads confirmed BulkImportJob, EmployeeType.TenantId,
  zero cross-tenant Employee/EmployeeType references and zero bulk jobs.
- COMPLETE: service resumed and manual latest-commit deployment succeeded for
  `d2754cd984e8edf2570d310e556fda68a8eb7716`, deployment
  `dep-dahcpf7qj5pc73a6mu1g`. Render displayed `Deploy succeeded | Live`.
  Actual Swagger returned all 32 bulk routes (8 per master); runtime logs showed
  the worker polling BulkImportJob. No authenticated import was inferred from this.
  Render is a Free instance and can sleep when inactive; scheduled work requires
  the API to be awake. No paid-plan or billing change was made.
- PENDING: authenticated upload/preview/confirm/worker/report and DB-result checks.
  Active tenants are Quecksilber Technologies (8) and SkyFruit (9), each with one
  active login. Neither is assumed to be an authorized test tenant without an
  identified login/session. Render authentication does not authenticate a tenant.
- Previous 18 header, 88 bulk and 119 regression passes were not rerun.

### Authenticated development acceptance and UI samples (2026-09-10)

- User identified the Render API as the development target and supplied the tenant
  administrator account. Login resolved to tenant 8, Quecksilber Technologies,
  role 22. Credentials/tokens are not saved in the handoff.
- COMPLETE: `docs/bulk-upload` contains four upload-ready XLSX files, matching CSV
  files, a UI integration/testing README, reusable development acceptance scripts
  and actual sanitized preview/job/report evidence. All four sheets were inspected
  and visually reviewed; the first three XLSX files were accepted by the live API.
- COMPLETE: Department, Designation and Role each passed XLSX creation (two rows),
  CSV existing-record replay and pasted-CSV replay (two skipped, zero created).
  Nine flows passed, zero failed. Six master records remain with prefix
  `UIQA-20260910-` for UI review: Department IDs 6/7, Designation IDs 6/7,
  Role IDs 28/29. No employee assignments or role grants were created.
- COMPLETE: nine completed jobs reconciled to database rows and reports, including
  RecordId, name, description/remark, role type, parent Department, counts and
  tenant ownership. Evidence: `docs/bulk-upload/results/db-verification.json`.
- COMPLETE: seven new live HTTP validation checks passed. Missing alias mapping,
  duplicate rows, missing required name and unknown parent all blocked confirmation
  with HTTP 400. Anonymous read returned 401; View-only mutation and missing
  EmployeeType entitlement returned 403. Invalid drafts remain unconfirmed.
- BLOCKED: EmployeeType module 79 has no TenantEnabledModule entry for tenant 8
  and remains absent from current my-menu. Its successful import/DB acceptance is
  PENDING. Enable through existing Host entitlement sync and tenant role grants,
  then run only `run-live.ps1 -Masters EmployeeType`. Do not change permission
  logic, grant via direct SQL, or rerun completed masters to resolve this.
- Previous 18/88/119 passed suites were not rerun. This run does not repeat live
  concurrency/recovery/retry/cancel or cross-tenant authenticated acceptance.
- UI developer handoff: `docs/bulk-upload/README.md`. Angular implementation is
  separate and was not performed. Current master phase is not wholly accepted
  until EmployeeType's entitlement-dependent live checks pass.

### Employee code pattern decisions (2026-09-11; approved design, implementation PENDING)

- Reuse the tenant's existing employee-code pattern configuration for Employee
  import. Preserve supplied existing codes when they match the selected pattern
  and do not conflict with another employee. When codes do not match, require the
  user to select/configure the intended pattern and review proposed codes.
- User approved changing the tenant pattern even after the initial Tenant Admin
  employee exists. Approved execution order: update the tenant pattern, change
  the existing Tenant Admin employee code to that pattern, then insert remaining
  imported employees using the same pattern. This updates the Admin's employee
  code, not their login, role or employee identity.
- Show code changes in preview and obtain explicit approval before applying them.
  Year/month components should use the employee joining/onboarding date rather
  than import time. Running numbers remain a separate sequence concern.
- Still clarify before implementation: whether already-created employees other
  than the initial Admin must also be recoded; which date is authoritative when
  joining and software-onboarding dates differ; and how retained numeric suffixes
  affect the counter and the Admin's proposed code. Do not invent these rules.
- Current generator still uses DateTime.UtcNow for year/month and increments
  LastUsedNumber; this decision is not implemented or tested yet. No code, pattern
  or employee record was changed while recording this decision.

### Requested next scope (2026-09-11; implementation PENDING)

- User requested employee-code pattern insert/update endpoints alongside the
  existing Tenant GET endpoint, and an Employee insertion template. Reuse existing
  folders, repositories, mappings, constants and permission pipeline; no new code
  folders or assumed business rules are authorized.
- Employee template must cover the required onboarding/base-account information
  and optional available profile data, including contact number and main address.
  Employees should complete remaining information after their own login.
- Inspection: Employee already contains MobileNumber and emergency-contact fields.
  Main address fields are in the existing EmployeeContact model/flow, not Employee.
  Confirm the intended primary contact/address mapping before implementing it;
  do not add address columns to Employee by assumption.
- Pattern-update scope for existing non-admin employees and date/sequence rules
  from the preceding decision section still require clarification. New endpoint
  implementation/tests and the final Employee template are not marked complete.

### Confirmed Employee import rules (2026-09-11; supersedes open questions above)

- A tenant pattern change must update employee codes for all existing employees,
  including the initial Tenant Admin, after the user approves the preview.
- Use original joining date for year/month components, not import time or the
  software account-creation timestamp.
- Preserve matching existing employee codes. Continue new running numbers after
  the highest retained number: retained 0145 and 0200 means next number 0201.
- Save supplied information to the corresponding existing employee-related
  tables. MobileNumber belongs to Employee; primary personal contact/address
  uses the existing EmployeeContact flow. Missing optional profile information
  can be completed by the employee after login.
- These business decisions are approved. Pattern insert/update endpoints,
  all-employee recoding, Employee import/template and their required tests remain
  implementation work; this confirmation alone is not an implementation/test pass.

### Capacity and invitations (2026-09-11; user delegated design choice)

- Count the initial Tenant Admin in the subscription MaxUsers limit. Employee
  import must enforce capacity in the same tenant-scoped transaction as account
  creation, including a commit-time recheck. Do not count the Host as a tenant seat.
- Separate account creation from invitation dispatch. Import confirmation does
  not automatically email all uploaded employees. An explicit Send invitations
  action dispatches invitations after the account results have been reviewed.
- Persist invitation pending/sent/failed state and retry invitations independently
  of Employee creation. Generate password setup tokens at dispatch time using the
  existing token flow. These decisions do not authorize sending emails now.
- Implementation started with the shared EmployeeCodePatternFormatter and focused
  tests in existing folders. Initial 14 formatter tests passed; subsequent preview
  additions and endpoint/persistence integration are WIP, not yet verified.

### Employee implementation checkpoint (2026-09-11; WIP)

- COMPLETE validation milestone: 19 formatter/preview tests, 8 route/permission
  tests and 7 isolated PostgreSQL pattern tests passed (34 distinct cases).
  Two initial database failures were missing joining dates in the restored test
  fixture; the disposable fixture now supplies explicit synthetic dates/codes.
  Production behavior still rejects missing dates. A subsequent switch from global
  table locks to tenant-specific locks requires the affected DB checks again.
- WIP: POST `/api/Tenant/add-employee-code-pattern` and PUT
  `/api/Tenant/update-employee-code-pattern`: `Confirm=false` previews all existing
  employees; `Confirm=true` requires the unchanged `PreviewHash`. Code allocation
  now shares a tenant lock with recoding and participates in the account transaction.
- WIP: Employee preview/confirm/jobs/retry/cancel/template/report routes reuse the
  existing durable queue and Employee permission pipeline (`EMP_LIST`). An explicit
  `/api/Employee/bulk/send-invitations` action is separate from account creation.
  New code is not deployed and its import/invitation tests are still in progress.
- Confirm reserves the approved code range. A changed pattern/counter blocks
  confirmation with a fresh-preview requirement. Cancellation can leave unused
  sequence gaps; previously issued/reserved numbers are not recycled.
- Shared capacity calculation counts all non-soft-deleted Employee records,
  including initial Admin and suspended employees. Host users are not Employee
  seats. A single valid active subscription/plan with positive MaxUsers is required.
  Ordinary Employee creation also uses the same insertion-time capacity guard.
- Optional contact/address columns map through the existing CreateContact DTO and
  AutoMapper profile to primary personal EmployeeContact. ContactNumber is required
  when that record is supplied. Unknown Employee source columns require explicit
  mapping/removal rather than silent data loss.
- Invitation states: Pending, Sending, Sent, Failed, DeliveryUnknown, NotRequired.
  Pending/known-failed invitations can be explicitly dispatched again independently
  of import. Sent rows are skipped. Interrupted/uncertain deliveries require log
  review; they are never automatically resent. No real invitations have been sent.
- COMPLETE artifact creation: `docs/bulk-upload/05-employees.xlsx` and matching
  header-only CSV contain 27 columns. Both workbook sheets were visually inspected;
  headers and no-data rows were inspected, with no formula errors. The workbook
  currently labels the Employee API as pending until acceptance is complete.
- Isolated fixture only: five bulk migrations, including AddEmployeeBulkImport.sql,
  applied to `127.0.0.1:55439/skolenza_bulk_test`. The new migration expands the queue
  master range and adds normalized login / tenant employee-code uniqueness. Existing
  duplicates cause migration failure; no records are merged/deleted by migration.
- PENDING: finish focused import/invitation tests, audit legacy Host aggregate
  pattern edits for the preview requirement, update UI handoff with final evidence,
  and perform authorized deployment/live acceptance. Prior passed master suites
  remain untouched. Employee import is not marked COMPLETE at this checkpoint.

### Employee validation results (2026-09-11; supersedes the WIP test checkpoint)

- COMPLETE: 77 distinct focused automated cases passed, zero remaining failures
  or skips. Breakdown: 19 formatter/preview, 10 pattern permission/Host-guard,
  9 PostgreSQL pattern, 17 Employee parsing/XLSX, 9 Employee route-authentication,
  8 Employee database workflow and 5 invitation-dispatch tests.
  Evidence inventory: `docs/bulk-upload/results/employee-automated-summary.json`.
- Changed tenant-lock and insertion-time capacity paths were retested because
  implementation changed. Previously passed unrelated master suites were not
  rerun. Initial invalid-fixture failures and compile errors were corrected;
  they are not counted as additional successful cases.
- COMPLETE local implementation/tests: pattern add/update preview and confirmation,
  unchanged-code preservation, all non-deleted employee recoding (including Admin
  and inactive employees), archived-code collision protection, transaction-safe
  sequence allocation, Employee durable import, account/role/image/contact writes,
  tenant/reference checks, capacity rechecks, reports, and independent invitations.
  Host aggregate edits now reject pattern changes that would bypass preview.
- Database acceptance used real PostgreSQL, existing AutoMapper profiles, the
  existing Employee repository and actual EMP_LIST permission function. Assertions
  reconciled created account/login/role/image/address records with job results,
  checked preserved 0145/0200 -> 0201, capacity changes, stale drafts and cross-tenant
  rejection. Invitation tests used recorded results/fake mail services; no real
  invitation emails were sent.
- COMPLETE: template and UI guide at `docs/bulk-upload/05-employees.xlsx`, matching
  CSV and `EMPLOYEE_IMPORT_UI.md`. No new coding folders were introduced.
- BLOCKED HTTP execution: automatic approval review rejected starting a separate
  isolated local API process, returning only `blocked by policy`. That command did
  not run. Route-attribute tests and repository tests are not represented as an
  authenticated HTTP upload test.
- PENDING: deployment of this Employee phase, target backup and new Employee
  migration, authenticated live upload -> preview -> confirm -> worker -> report
  -> target DB reconciliation. Earlier Render/master acceptance remains separate.
  Therefore overall Employee import acceptance remains WIP, not fully COMPLETE.

### Live continuation (2026-09-11; deployment in progress)

- User reports a Render build is running; no duplicate deployment was triggered.
- COMPLETE EmployeeType bulk acceptance: authenticated my-menu now contains module
  79 with Add/Update/View. XLSX created two sample types; CSV and paste each skipped
  both existing rows. All three workers completed and both report rows matched.
  Evidence: `docs/bulk-upload/results/EmployeeType-*-*.json` and report CSV files.
- COMPLETE read-only DB reconciliation: all 12 saved master jobs and 8 sample
  master records match report IDs, values, tenant ownership and status counts.
  Earlier master uploads were not rerun; only their saved evidence was reconciled.
- PENDING EmployeeType CRUD acceptance: Delete is absent from current my-menu;
  latest update/delete routes are not yet visible in live Swagger.
- PENDING Employee phase: current Swagger still lacks Employee bulk and pattern
  add/update routes while the user's build runs. Fresh target backup started;
  completion and new migration are not yet claimed. Browser tooling could not
  attach the Render dashboard, so service/build status is not independently verified.

- COMPLETE fresh target backup: custom archive
  `C:/Users/qtech/AppData/Local/Skolenza/Backups/workforcedb-pre-employee-20260911-161759.dump`
  (804094 bytes), archive listing and full archive read passed; restore not run.
  SHA256: `9CFF12F60CEB540C5ABCC0267B9FA57EADA387C47F07289A122A6BD091A85869`.
- Read-only migration preflight: zero duplicate normalized tenant employee-code
  groups and zero duplicate normalized login groups. Both new unique indexes are
  absent; queue constraint still allows masters 1–4. No migration applied yet.
- Dashboard temporarily connected: deployment `dep-dai06o2d0e5s73972ka0` was Building
  commit `3387971ee59de9a41cd10550d683758b3603c6b8`; old commit `d2754cd` remained
  Live. This verifies build activity, not completion or service suspension.

### Local release correction (2026-09-11)

- The deployment above FAILED during `dotnet publish` (exit 1). Local Release
  publish reproduced three compiler errors in EmployeeTypeHandlers: its response
  DTO namespace differed from the repository's existing response DTO.
- Fixed the handler with an explicit alias to the existing repository/mapping DTO;
  no new DTO, folder or business rule was introduced. Release publish now passes
  (exit 0); evidence: `artifacts/bulk-import/employee-release-build.log`.
- User requested finishing local work before any further Render build/deployment.
  Target migration, service suspension/restart and live Employee acceptance remain
  pending. The failed build did not replace the previously live version.
- COMPLETE focused regression: both new EmployeeType list response/paging cases
  passed (2 passed, 0 failed, 0 skipped). They verify the existing repository DTO,
  trusted tenant scope, inactive record visibility and empty out-of-range pages.
  Evidence: `artifacts/bulk-import/employee-type-response-regression.trx`.
  Earlier 77 Employee tests and passed master suites were not rerun.

### Target Employee migration and deployment (2026-09-11)

- COMPLETE: fix commit `bdd095d3e48a8283e10da803199839be13d079a5` already matched
  local main_branch and remote main_branch. Render displayed Live deployment
  `dep-dai0ak7qj5pc73aqlshg`; no duplicate commit/deployment was needed.
- COMPLETE: live Swagger exposes all nine Employee bulk routes, pattern add/update
  and EmployeeType update/delete. A cache-busting Swagger request verified this.
- COMPLETE: Render service was explicitly suspended and Suspended status observed.
  The pending `AddEmployeeBulkImport.sql` ran against workforcedb_34hi_duis and
  committed both unique indexes and the master-range constraint update. Prior four
  migrations were not rerun. Log: `artifacts/bulk-import/employee-target-migration.log`.
- Resume service requested after successful commit. Authenticated post-restart
  verification and remaining Employee/CRUD/pattern acceptance are in progress.

### Live acceptance checkpoint (2026-09-11)

- COMPLETE: service resumed; authenticated login and new routes work after migration.
- COMPLETE Employee CSV upload -> preview -> confirm -> worker -> report: job
  `abfc9cce-3561-4066-9b51-f2f95424c530` created 2, failed 0. Supplied codes
  QT/2026/0145 and QT/2026/0200 were preserved. Both employees have exactly one
  login, role 24 and initial image; tenant, mobile and personal contact/address
  match the report. Evidence: `docs/bulk-upload/results/employee-live-*`.
- COMPLETE paste replay: job `df2a388c-c78e-462e-8aca-504b2bc7fcd6` created 0,
  existing 2, failed 0. Next-code preview produced QT/2026/0201; its draft was
  cancelled without insertion. No invitation emails were sent.
- COMPLETE EmployeeType CRUD: role 22 received only module 79/Delete through the
  existing assign-role-permissions API. Fresh my-menu contains Add/Update/Delete/
  View. A dedicated UIQA type was created, renamed/deactivated, then soft deleted;
  it is absent from the tenant list. Evidence: `employee-type-crud-*.json` and
  `employee-type-live-menu.json` under the results directory.
- User supplied Admin joining date 24/07/2018. Saved through Employee official/update
  with existing EMP_OVERVIEW module 9/Update; the initial module 8 request was
  correctly rejected. No direct database patch was used.
- Pattern preview exposed a legacy defect: the original Admin code uses account
  creation year 2026. Fix recognizes only an exact current-pattern code using the
  persisted AddedDateTime, preserves its sequence, and formats the proposed code
  using original joining date. AddedDateTime is included in the preview fingerprint.
  Unknown dates/codes remain blocked. Three focused tests passed (two new cases
  and one affected missing-date case); unrelated passed tests were not rerun.
- Fix committed as `2c91b6eb`; push is awaiting Git Credential Manager sign-in.
  PENDING: deploy this correction, live pattern recoding confirmation and final
  code/DB checks. Overall acceptance remains WIP until these finish.

### FINAL acceptance — COMPLETE (2026-09-11)

This checkpoint supersedes earlier WIP/blocker notes for the requested release.

- Correction `2c91b6eb5908cabd39da19e04a77d9d48a67730b` pushed successfully.
  Render deployment `dep-dai0l23m8hqs73dmbdeg` displayed Deploy succeeded | Live.
- Backup, suspension, Employee migration, resume and authenticated API verification
  complete. Both unique indexes exist on the target database.
- Employee CSV create and paste replay worker/report checks passed: 2 created,
  then 0 created/2 existing. DB reconciliation verifies tenant, codes, mobile,
  contact/address and one login/role/image per employee. Next generated preview
  is QT/2026/0201; unused test draft was cancelled. No invitations were sent.
- Pattern update preview/confirm recoded all three tenant employees using a test
  separator, then restored the original QT/year/slash/four-digit configuration.
  Final DB codes: QT/2018/0001, QT/2026/0145, QT/2026/0200; LastUsedNumber=200.
  Admin joining date is the user-confirmed 2018-07-24. No identity/login IDs changed.
- Add-pattern endpoint correctly rejects an already configured tenant with 409.
  Successful missing-pattern insertion is covered by existing isolated DB tests;
  no live pattern was deleted merely to manufacture a missing-pattern fixture.
- EmployeeType manual Create/Update/Delete/list and four CRUD my-menu actions pass.
  The dedicated CRUD QA row is soft-deleted; the two bulk sample types remain.
- Evidence: `docs/bulk-upload/results/employee-*.json` and reports; pattern apply/
  restore responses and `employee-pattern-final-db.json` contain final results.
  Live Employee input used CSV/paste; XLSX parsing/date systems remain covered by
  automated tests. No live SMTP/invitation acceptance is claimed.
- Requested deployment/import/recoding/EmployeeType acceptance is COMPLETE.
  Earlier passed unrelated suites were not rerun. Only changed/new legacy-date
  cases were run after the final code correction.

### Scenario documentation — COMPLETE (2026-09-11)

- Added `docs/bulk-upload/BULK_API_SCENARIOS.md` and linked the sample README.
- Covers all five module examples, mandatory/optional inputs, template discovery,
  shared API sequence, job/row statuses, retry, destination tables, retained draft
  data, pattern approval and optional invitations.
- Verified against DTOs, controllers, workflow/parser, repository and report code:
  cancellation remains possible in Draft/Queued/Running at batch boundaries;
  committed inserts remain. Terminal-job cancel does not change status. No bulk
  job TTL/automatic purge exists; parsed source data stays in PreviewJson.
- Documentation only; no business code changed and no passed tests repeated.

### Resend email verification — 2026-09-11

- Brevo authenticated log review found 49 events in the selected date range.
  Latest `axionvibe@gmail.com` attempt has `Sent` and `Delivered` at 22:09; earlier
  `mca.deepesh@gmail.com` attempts also have Delivered/Open/Clicked events.
- Historical `Error` events coexist with successful attempts. The screenshot's
  success envelope plus error toast is consistent with multiple overlapping resend
  requests; it is not evidence of one contradictory API response. No extra resend
  was triggered during review.
- The supplied Render log still records SMTP connect timeout on port 587. The
  authenticated Brevo result confirms delivery is possible for some attempts, but
  does not remove the intermittent-failure issue. Keep this item WIP until each
  click maps to one request and the Render timeout/configuration is addressed.

## 2026-09-12 — bulk menu seed correction (WIP)

> Historical design record. The 2026-09-15 canonical hierarchy uses independent
> `TENANT_DEPARTMENT`, `TENANT_DESIGNATION`, and `TENANT_ROLE` roots with
> `DEPARTMENT`, `DESIGNATION`, and `ROLE` leaves. Import/Export now belong to
> those leaves; the bulk child-module design below is superseded.

Superseded by the user-approved hierarchy below: the standalone `BULKUPLOAD`
parent must not be retained. Each distinct bulk page is a child of its related
existing functional module. Existing functional module names, routes, CRUD
operations and catalogue identities remain unchanged.
Existing Add/Create duplicates are NOT merged by the existing normalization code;
prior statements claiming that normalization removed duplicates were incorrect.
Bulk mapping selection uses one active operation per required operation type.
Pending: SQL integration tests, dependency-aware cleanup of pre-existing operation
duplicates, and API permission compatibility for the new bulk ModuleIds. Employee
bulk currently validates EMP_LIST, so the new child ModuleId cannot be substituted
in API calls without a reviewed permission-pipeline change. Tenant sync is owned
by the user as requested. Do not mark MyMenu/API acceptance COMPLETE yet.

### 2026-09-13 — approved tenant bulk hierarchy seed (COMPLETE locally)

- Remove the obsolete tenant-scope `BULKUPLOAD` root and its direct catalogue,
  plan, entitlement and role-grant references.
- `BULK_EMPLOYEES` is a child of `EMP_MGMT`.
- `BULK_DEPARTMENTS` is a child of `DEPARTMENT`.
- `BULK_DESIGNATIONS` is a child of `DESIGNATION`.
- `BULK_ROLES` is a child of `ROLE`.
- `BULK_EMPLOYEE_TYPES` is a child of `EMPLOYEE_TYPE`.
- Each bulk child maps the existing operation types View=4, Export=11 and
  Import=12. The seed creates Export/Import only when that operation type is
  absent; it does not create Add/Create aliases. Export here is permission/menu
  catalogue metadata; the UI developer owns the export implementation.
- Bulk child plan coverage inherits from its corresponding existing functional
  plan source. Tenant entitlement and role grants remain owned by the existing
  synchronization/permission flows.
- Functional parents become non-leaf so MyMenu can return their bulk child. The
  UI developer owns rendering the existing functional parent page plus its child.
- No Host Card/Device bulk hierarchy is changed by this decision.
- Full consolidated seed executed twice successfully on the isolated clone
  `skolenza_seed_hierarchy_test` after supplying its required test-only active
  TenantEmailConfig prerequisite. Production DB was not used. Final verification:
  five tenant bulk children, zero `BULKUPLOAD` rows, 15 active child mappings
  (exactly one View/Export/Import per child), five active plan mappings per child,
  correct parents/non-leaf parent state, and two active canonical Host Admin users.
  The second run retained the same counts, proving this tenant section idempotent.
- Existing Host Card/Device data in the cloned baseline contains two active Import
  mappings per Host bulk module (operation IDs 13 and 23). This was visible in the
  consolidated seed output and is not caused by, or changed within, the approved
  tenant hierarchy scope. It remains an explicitly recorded separate cleanup item.
- Evidence: `artifacts/bulk-functional-parent-seed-run2.log` and
  `artifacts/bulk-functional-parent-seed-run3-idempotent.log`.
- User reconfirmed that these Tenant bulk `PageName` values must remain unchanged:
  `bulk-employees`, `bulk-departments`, `bulk-designations`, `bulk-roles` and
  `bulk-employee-types`. Future operation/mapping seed work must not rewrite them.

## 2026-09-12 — Host CardBulk / DeviceBulk (WIP)

User-approved scope: CardBulk under Tenant RFID Management and DeviceBulk under
Device Catalogue, both ModuleScope=2. No Host/Tenant scope conversion is authorized.
Reviewed existing CreateTenantCardMaster and CreateDeviceMaster handlers, request
DTOs, bulk queue ownership and worker architecture. Device catalogue target is
DeviceMaster; it is not TenantDevice installation or attendance-device enrollment.
Device required fields follow current handler: SNo, DeviceCode, DeviceName,
CompanyName, ModelNo and valid DeviceType. Card number stays text (leading zeros),
encrypted at rest, and masked in responses; existing numeric/price rules apply.
Card upload tenant selection is confirmed: exactly one Host-selected tenant per
upload/job; mixed-tenant rows are rejected. Tenant-Device assignment is deliberately
outside this import and remains manual. Existing durable queue uses TenantEmployee
actor ownership, so Host job ownership and persisted Host permission revalidation
need explicit implementation.
Implementation and new feature tests are PENDING; no feature-complete claim and
no database migration/deployment have been performed for these two imports.

### Host bulk implementation progress — 2026-09-12

Confirmed decision: CardBulk imports into exactly one Host-selected tenant per job.
Added HostBulkImportTableMapper using existing Host DTO property definitions;
forbids spreadsheet permission/tenant/audit fields, preserves string identifiers,
requires explicit mapping for unfamiliar headers, and parses ISO invoice dates,
invariant decimals and defined enums without echoing private cell values.
Validation: HostBulkImportTableMapperTests passed 11/11, zero failures/skips.
Evidence: artifacts/host-bulk-import-tests.log. These are mapper tests only.
Remaining: Host preview persistence, encryption-safe card staging, duplicate checks,
Host job ownership and worker authorization, both endpoint sets, scope-2 menu seed,
and workflow/database tests. Feature status remains WIP; not deployed or usable yet.

### Host CardBulk / DeviceBulk completed locally — 2026-09-12

The earlier mapper-only status above is superseded by this section.

- COMPLETE: both scope-2 endpoint sets (preview, confirm, job, jobs, retry,
  cancel, template, report), existing Host runtime permission checks,
  owner/tenant isolation, worker permission recheck and row savepoints.
- COMPLETE: DeviceMaster insertion with manual TenantDevice assignment;
  one-selected-tenant Card inventory insertion with encrypted staging,
  masked output, existing procurement mapping and duplicate protection.
- COMPLETE: AddHostBulkImport.sql, targeted SeedHostBulkImportModules.sql,
  both consolidated menu seed references, publish inclusion and migration
  runner -HostBulkOnly. Host grants use the existing role permission flow.
  Scope-2 modules intentionally have no PlanModuleMapping.
- Validation: 36 distinct Host tests passed (14 mapper, 12 permission, 10
  PostgreSQL/local HTTP), plus 75 shared bulk regression tests. Zero skips.
  Both HTTP upload -> preview -> confirm -> worker -> report flows reconciled
  with actual rows in the isolated database. JWT/Host context is a test fixture.
- Migration and targeted seed applied twice to local PostgreSQL
  127.0.0.1:55439/skolenza_bulk_test; second run preserved module identities.
- PENDING RELEASE: target DB backup/migration, production publish/restart,
  Host role grants/menu refresh and production authenticated smoke verification.
  No production deployment/test is claimed for this implementation.
- Handoff: docs/bulk-upload/HOST_CARD_DEVICE_IMPORT.md. Evidence:
  artifacts/host-bulk-build.log, host-bulk-workflow-tests.log,
  host-bulk-http-regression-tests.log and host-bulk-final-edge-tests.log.
- Release publish succeeded to artifacts/host-bulk-release with both Host SQL
  files included. The migration runner -HostBulkOnly passed on the isolated clone.
  Enable BulkImport__WorkerEnabled=true after migration. Release/runner evidence:
  artifacts/host-bulk-release-build.log and artifacts/host-bulk-migration-runner.log.

### 2026-09-12 — Employee Phase 0/1 regression rerun

- COMPLETE: common profile calculator, 13-section response, overall percentage metadata, and verified-implies-locked persistence invariant.
- COMPLETE: Angular response/request contract corrected (`data`, numeric `tabInfoType`).
- COMPLETE: backend solution build passed with zero errors; focused Employee profile suite passed 16/16.
- BLOCKED (environment): Angular dependencies are absent and global npm is broken; Angular compilation is not recorded as passed.
- PENDING: EmployeeBulk assignment writers for Work Locations, Devices, Work Arrangement, Work Pattern, and Overrides require explicit import row contracts and DB acceptance tests.

### Host catalogue bulk imports — COMPLETE locally (2026-09-13)

- Added four scope-2 bulk children: `HOST_MODULE_CATALOGUE_BULK` under
  `HOST_MODULES`, `HOST_SUBMODULE_CATALOGUE_BULK` under `HOST_SUBMODULES`,
  `HOST_OPERATION_CATALOGUE_BULK` under `HOST_OPERATIONS`, and
  `HOST_MODULE_OPERATION_CATALOGUE_BULK` under `HOST_MODULE_OPERATIONS`.
- Each child reuses canonical View=4, Export=11 and Import=12 operations. The seed
  creates no aliases or subscription-plan mappings and preserves stored
  `Module.PageName` and `Operation.OperationName` on reruns.
- Added eight durable routes beneath each base: `/api/Module/import`,
  `/api/SubModule/import`, `/api/Operation/import`, and
  `/api/ModuleOperation/import`. Suffixes are `preview`, `confirm`, `job`, `jobs`,
  `retry`, `cancel`, `template`, and `report`.
- Parent/child identity uses `ModuleCode`; a child also uses `ParentModuleCode`.
  Operation identity uses `OperationType`; mapping identity uses
  `ModuleCode + OperationType`. Database identity, tenant, permission and audit
  columns are rejected. Existing records are skipped without overwrite. A reused
  OperationType with a different immutable OperationName is invalid. Missing
  mapping dependencies are invalid and never auto-created.
- `AddHostBulkImport.sql` permits durable master values 1–11. Both targeted and
  consolidated seeds contain the four definitions and mappings.
- Validation: 35 mapper/permission/controller tests and one focused PostgreSQL
  catalogue lifecycle test passed, all with zero failures/skips. The pre-final
  affected Host regression run also passed 42/42; already-passed cases were not
  repeated after documentation-only changes.
  Isolated PostgreSQL created Module → Child → Operation → Mapping, then replayed
  as Existing. Seed ran twice and retained correct parent/scope/page identity and
  exactly three operation types per child. Evidence: `artifacts/host-catalogue-bulk-*`.
- Local implementation is COMPLETE. Production migration/seed, Host grants and
  deployed authenticated smoke testing are pending release activities.
- UI/API handoff with inputs and call order: `docs/bulk-upload/HOST_CATALOGUE_IMPORT.md`.
  The targeted Host seed passed twice. A full consolidated-seed test was blocked
  earlier by its existing mandatory TenantEmailConfig prerequisite, before the
  embedded Host section; it is not recorded as a full consolidated-seed pass.
- Production-shaped legacy cleanup regression fixed on 2026-09-13: an existing
  `BULKUPLOAD` parent with `HOST_CARD_BULK` still attached previously failed with
  `FK_Module_ParentModule`. The complete seed now reparents all known Host bulk
  children before deletion. The exact legacy state was reproduced on isolated
  PostgreSQL; the block completed, removed the obsolete root and placed CardBulk
  under `HOST_TENANT_RFID_MANAGEMENT`. Unknown legacy children retain a hidden,
  inactive compatibility parent for explicit review instead of failing the seed.
  Evidence: `artifacts/host-catalogue-bulk-fk-cleanup-*`.

- New Employee profile characterization/permission tests passed 14/14 with zero skips.
- Related Work/Device and EmployeeBulk unit regressions passed 181/181 with zero skips.
- Employee import/type/code isolated PostgreSQL tests passed 17/17 with zero skips against
  `skolenza_bulk_test` on port 55439.
- A full isolated-database suite rerun produced 424 passes, 10 failures and 7 skips. The failures are
  HostBulk fixture migration drift: the current `BulkImportJob_Master_check` in that test database
  rejects newer Host bulk master values. This is explicitly a failed HostBulk acceptance rerun, not
  an EmployeeBulk pass or an Employee profile regression. Reapply/verify the Host bulk migration on
  the isolated fixture before claiming the full suite green.

### Tenant policy durable imports — implemented locally (2026-09-16)

### 2026-10-14 — Worker host-shutdown cancellation regression

- The durable worker now handles cancellation both while waiting for its next
  `PeriodicTimer` tick and while processing a batch. Normal API shutdown no longer
  propagates `OperationCanceledException` from the idle wait.
- Focused Release verification passed 1/1 with zero failures/skips. The full
  Release solution build completed with zero errors, and a local start/poll/stop
  runtime cycle produced no worker cancellation exception.
- Local fix status is COMPLETE. Render publication and deployed shutdown
  verification were not run. Evidence:
  `docs/testing/bulk/worker-graceful-shutdown/2026-10-14.md`.

2026-10-03 live acceptance update: created twelve realistic Policy Definitions through the
existing authenticated durable import UI. Preview passed 12/12; completion was 12 imported,
0 skipped and 0 failed. Render read-only reconciliation confirms 12 Draft version-1 policies,
28 metadata-backed rules and 12 Permanent EmployeeType applicability rows. No policy was
published or assigned. Evidence: [draft definitions](testing/policy/bulk-draft-definitions/2026-10-03.md).

2026-10-03 live acceptance update: through the signed-in Angular bulk UI and Render API,
nine missing-category Policy Types were created with realistic names (9 imported, 0 failed).
Identical replay preview detected all nine as existing; confirmed replay completed and
read-only Render reconciliation showed twelve category-covered types and zero duplicate
codes. No definitions/assignments or schema changes were made. This narrow scenario is
COMPLETE; the broader durable policy import release acceptance remains WIP.
Evidence: [bulk missing types](testing/policy/bulk-missing-types/2026-10-03.md).

- Added Policy Type (master 12), Policy Definition (13), and Policy Assignment
  (14) to the existing durable job engine. No separate queue or permission model
  was introduced.
- Routes are under `/api/TenantPolicy/bulk/{target}` for `types`, `definitions`,
  and `assignments`: preview, confirm, job, jobs, retry, cancel, template and report.
- Each target binds to its existing leaf module: `TENANT_POLICY_TYPES`,
  `TENANT_POLICY_DEFINITIONS`, or `TENANT_POLICY_ASSIGNMENTS`. The worker checks
  authorization again before writes.
- Definition creates version 1 as Draft. Assignment accepts only a Published
  version and creates missing Assigned acknowledgement evidence. Policy Type can
  reactivate an inactive, non-deleted match when the source requests active.
- Preview and worker-time validation cover required values, source duplicates,
  ISO dates, JSON shape, active rule types, same-tenant references, geography
  hierarchy and published assignment targets.
- Local API build passed. Focused policy and affected regression tests: 61 passed,
  3 existing environment-dependent tests skipped, 0 failed. PostgreSQL policy
  lifecycle and deployed authenticated verification were not run because no
  policy test connection/deployed context was configured. Status is **WIP /
  release-blocked**, not COMPLETE.
- UI contract: `docs/UIdeveloperDoc/TENANT_POLICY_API.md`. Evidence:
  `docs/testing/policy/bulk-import/2026-09-16.md`.
- Release migration `AddPolicyBulkImport.sql` extends the durable master check to
  14. The API publish includes it and `ApplyBulkImportMigrations.ps1
  -PolicyBulkOnly` discovers it; validate-only passed against Development config
  without changing the database.
- Release publish passed and included both the policy migration and runner.

### 2026-10-03 — Policy assignment applicability guard and popup/export contract

- WIP: the existing Policy Assignment durable import remains the only bulk queue; no parallel
  upload system was introduced. It supports 100+ rows through bounded worker batches.
- Added the Map Policies candidate contract. It resolves the selected Published version's
  effective Applicability and returns employee code, employee type, department and designation
  plus eligible-population filter facets.
- Direct and durable bulk assignment now repeat the same applicability check at write time.
  Employees outside applicability are rejected and never inserted.
- Added unpaged mapped-assignment CSV export under the existing Policy Assignments permission leaf.
- Local automated verification is recorded at
  `docs/testing/policy/assignment-applicability-mapping/2026-10-03.md`. Deployed authenticated
  acceptance remains pending.

