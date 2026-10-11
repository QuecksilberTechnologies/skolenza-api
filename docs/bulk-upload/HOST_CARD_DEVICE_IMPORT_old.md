# Host CardBulk and DeviceBulk release guide

Implementation: complete locally. Production migration/deployment and production
smoke verification are separate release steps; no production run is claimed here.

## Menu and permission contract

| Import | Parent | ModuleCode | Scope | Destination |
|---|---|---|---:|---|
| Card Bulk | Tenant RFID Management | HOST_TENANT_RFID_MANAGEMENT | 2 | TenantCardMaster, one selected tenant |
| Device Bulk | Device Catalogue | HOST_DEVICE_SETUP | 2 | DeviceMaster, global catalogue |

Read module/operation IDs from the authenticated Host menu; IDs differ by database.
Use Import (operation type 12) for preview/confirm/retry/cancel; View (type 4) or
Import for template/job/jobs/report. These are operation **types**, not IDs.
The existing Host permission function validates current grants, including Super Admin.
Host modules do not use subscription PlanModuleMapping or tenant entitlement sync.
Grant Import/Export on these existing modules through the Host role permission screen.
Device import does not assign, occupy or install a device. Tenant-Device remains manual.

## URLs and mandatory request fields

Base paths:

- Device: `/api/DeviceMaster/import`
- Card: `/api/TenantCardMaster/import`

Both expose the following exact routes:

| Method and suffix | Input | Result |
|---|---|---|
| GET `/template` | ModuleId, OperationId in query | CSV starter headers |
| POST `/preview` | multipart/form-data: ModuleId, OperationId; exactly one File or PastedText | Saved draft, mapping, errors, Ready/Existing/Invalid counts, JobId, CanCommit |
| POST `/confirm` | JSON: ModuleId, OperationId, JobId | Queued saved job; no replacement rows accepted |
| GET `/job` | ModuleId, OperationId, JobId in query | Status, row results, CreatedCount, ExistingCount, FailedCount |
| GET `/jobs` | ModuleId, OperationId; PageNumber=1, PageSize=20 | Current Host user's jobs for target/selected tenant |
| POST `/retry` | Same JSON as confirm | Failed rows queued again; created records retained |
| POST `/cancel` | Same JSON as confirm | Cancellation at batch boundary |
| GET `/report` | Same query as job | CSV results; cards masked |

All routes require a valid Host bearer token. Card requests also require the
encoded `TenantId` returned by the existing Host tenant API (template does not
need a tenant). Select one tenant in UI and send it on preview and later actions.
Device requests must omit TenantId. Never include tenant, permission, assignment
or audit columns inside a spreadsheet.

Optional preview fields: RequestId (new GUID per corrected upload), SheetName,
ColumnMappingJson. Retrying the same RequestId and identical input returns the
saved draft; reusing it for another tenant/master/source returns a conflict.
ColumnMappingJson maps target to source, e.g. `{"SNo":"Serial","DeviceCode":"Code"}`.
Unknown names are not guessed; missing mandatory mappings block confirmation.

## Device example

Required nonblank columns: SNo, DeviceCode, DeviceName, CompanyName, ModelNo,
DeviceType. Use this CSV or paste it; Excel cells for identifiers should be Text:

```csv
SNo,DeviceCode,DeviceName,CompanyName,ModelNo,DeviceType,IsActive
000123,DEMO-FACE-01,Reception Face Model,Demo Manufacturer,DEMO-F100,Face,true
```

DeviceType accepts the existing names Face, Fingerprint, Card, FaceFingerprint,
FaceCard, MultiBiometric, AccessControl, Other (or their defined enum numbers 1–8).
Optional columns are existing DeviceMasterRequestDTO editable fields, including
Price, CurrencyKey, SupportsFace, SupportsCard, FirmwareVersion, Description,
Remark, warranty/capacity/connectivity fields. Omitted fields retain existing DTO
defaults. The starter template contains the required fields plus IsActive;
at most 64 columns may be uploaded at once. DeviceCode or CompanyName+ModelNo
duplicates are detected using the same case-insensitive rules as manual create.
Existing records, including inactive non-deleted records, are skipped, not overwritten.

## Card example

CardNumber is the required column: 1–19 ASCII digits, stored as text to preserve
zeros. Format Excel card cells as Text before entry, especially beyond 15 digits;
Excel numeric precision loss cannot be recovered by the API.

```csv
CardNumber,CardReference,PurchaseCurrencyCode,UnitPurchasePriceExcludingTax,CgstAmount,PurchaseInvoiceDate,IsActive
0000123456789012,DEMO-CARD-01,INR,100,9,2026-09-12,true
```

For the selected tenant this creates an Available inventory card with landed cost
109 using the existing procurement calculation. Currency defaults to INR, amounts
to zero, IsActive to true. Optional procurement fields match the template:
supplier/place-of-supply country/state IDs, supplier/invoice details, tax treatment,
CGST/SGST/IGST/foreign tax and customs/freight. Dates must be ISO text yyyy-MM-dd;
decimals use `.` without grouping/currency symbols; booleans use true/false.
Card duplicates are tenant-scoped. Duplicate rows within one upload are invalid;
cards already present in the selected inventory are marked Existing.

## Preview, confirmation and polling example

Send the sample file to `/preview` with the menu's ModuleId/Import OperationId.
For Card also send TenantId outside the file. A valid draft has CanCommit=true.
Preview creates only BulkImportJob; no catalogue/card record exists yet.

Send JSON to `/confirm` (replace placeholders with actual values):

```json
{
  "moduleId": 81,
  "operationId": 23,
  "jobId": "00000000-0000-0000-0000-000000000001"
}
```

The numbers above are examples only. Card JSON must additionally contain
`"tenantId":"<selected encoded tenant ID>"`. Optional ScheduledAtUtc must be a
future UTC timestamp; omitted means queue now. Confirmation is idempotent.
Poll `/job` until a terminal status. Existing numeric status contract:
Draft=1, Queued=2, Running=3, Completed=4, CompletedWithErrors=5, Failed=6,
Cancelled=7. Worker permission failures produce Failed; row constraint failures
produce Failed rows and CompletedWithErrors. Read the report before retrying.
CreatedCount/ExistingCount/FailedCount describe committed row outcomes; a cancelled
job can have unprocessed rows. HostRecordId identifies the corresponding stored row.

## Storage, privacy and cancellation

BulkImportJob stores the parsed snapshot, owner, selected tenant, counts/cursor and
status. There is no automatic retention purge. Source files themselves are not
retained by this workflow. Card numbers in the saved snapshot are encrypted;
public preview, JSON results and CSV reports contain masked numbers. The existing
encryption key is not stored with the job. Device data lands only in DeviceMaster;
card procurement data lands only in TenantCardMaster after the worker commits.

Cancel is allowed in Draft, Queued and Running. While a batch is executing the
cancel call waits for that batch transaction; committed rows remain. After a
terminal status cancel is a no-op. No API reverses already-created inventory.
Retry accepts Failed/CompletedWithErrors and never recreates Created rows.
Worker revalidates Host grants, field rules, duplicates and selected tenant before
insertion. Unique indexes protect concurrent manual/bulk creation conflicts.

## Deployment to an existing Employee-bulk database

1. Back up the target DB and stop API/worker instances that use the queue.
2. Run `AddHostBulkImport.sql`, then `SeedHostBulkImportModules.sql` in pgAdmin,
   or the existing migration runner with `-HostBulkOnly` (command below).
3. Publish the updated API; both SQL files are included in the publish output.
4. Grant View/Import via the existing Host role permission flow; refresh the Host
   menu/session as required by the current application.
5. Enable `BulkImport__WorkerEnabled=true` after migration (disabled by default),
   start API/worker and run one authorized sample per target, compare report IDs
   and counts with DeviceMaster/TenantCardMaster, then record production evidence.

```powershell
./database-scripts/ApplyBulkImportMigrations.ps1 -Environment Production -HostBulkOnly -PsqlPath 'C:/Program Files/PostgreSQL/18/bin/psql.exe'
```

The runner uses the selected API configuration/connection override. Do not use the
complete reset seed on an existing production database. Both consolidated seed
references include the Host menu block for fresh installations, but the standalone
Host seed is the targeted upgrade. Existing names are preserved; IDs resolve by
ModuleCode. If a unique-index migration finds old duplicates, it rolls back and
requires explicit data correction; it never merges/deletes records automatically.

## Evidence

- Application/API build: zero errors (existing repository warnings remain).
- Release publish succeeded to artifacts/host-bulk-release, including the two
  targeted Host SQL files and migration runner. Release log:
  artifacts/host-bulk-release-build.log. The Host-only migration runner also
  passed against the isolated clone: artifacts/host-bulk-migration-runner.log.
- Host mapper: 14 tests; Host authorization: 12 tests; Host PostgreSQL/HTTP: 10
  tests across the recorded runs. Total 36 distinct Host tests, all passed.
- Shared bulk regression: 75 passed. These are local tests, not Render evidence.
- Real PostgreSQL clone: migration and Host seed executed successfully twice;
  the second run retained two modules and four View/Import mappings.
- Authenticated local HTTP tests use the actual controllers, multipart binding,
  handlers, workflow, real database and Host permission function. The test harness
  supplies a test Host context and signed test JWT; production login is not tested.
- Logs: artifacts/host-bulk-build.log, host-bulk-workflow-tests.log,
  host-bulk-http-regression-tests.log, host-bulk-final-edge-tests.log.
