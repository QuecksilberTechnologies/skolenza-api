# Host Catalogue Bulk Upload — UI Developer Implementation Contract

Updated: 2026-09-13

Backend status: implemented and locally tested

UI scope: four Host Admin bulk-import screens

This document is a direct UI implementation contract for Claude, Codex, or a UI
developer. Do not hard-code database ModuleId or OperationId values. Obtain the
current values from the authenticated Host user's existing menu/permission data.

## 1. Features available

| Screen | Menu module code | Parent menu | API base | Destination table |
| --- | --- | --- | --- | --- |
| Module Bulk | `HOST_MODULES` | Host Modules | `/api/Module/import` | `skolenza.Module` |
| Child Module Bulk | `HOST_SUBMODULES` | Host Submodules | `/api/SubModule/import` | `skolenza.Module` |
| Operation Bulk | `HOST_OPERATIONS` | Host Operations | `/api/Operation/import` | `skolenza.Operation` |
| Operation Mapping Bulk | `HOST_MODULE_OPERATIONS` | Host Module Operations | `/api/ModuleOperation/import` | `skolenza.ModuleOperationMapping` |

All four screens support CSV, XLSX and pasted tab/comma-delimited data, explicit
column mapping, saved preview, confirmation, background execution, scheduling,
job history, progress polling, cancellation, retry and result-report download.

The existing functional modules are scope `2` and map canonical View (`OperationType=4`), Export
(`11`) and Import (`12`) operations. Export currently represents menu/permission
metadata. These controllers do not expose catalogue-export business endpoints.

## 2. Permission rules

The access token must belong to a Host user. Use the existing Authorization Bearer
token/interceptor. Resolve the selected bulk menu's IDs from `my-menu` or the
existing Host permission response:

- `preview`, `confirm`, `retry`, `cancel`: use the granted **Import** operation.
- `job`, `jobs`, `template`, `report`: use granted **View** or **Import**.
- A ModuleId belonging to a different bulk screen is rejected with `403`.
- A Tenant user or missing Host grant is rejected by the existing permission flow.
- Do not send `TenantId` for these four catalogue imports.

Illustrative menu values only:

```ts
const permission = {
  moduleCode: 'HOST_MODULES',
  moduleId: moduleFromMyMenu.id,
  viewOperationId: moduleFromMyMenu.operations.find(x => x.operationType === 4)!.id,
  importOperationId: moduleFromMyMenu.operations.find(x => x.operationType === 12)!.id
};
```

## 3. Common endpoint list

Append each suffix to the relevant API base from section 1.

| Method | Suffix | Purpose | Permission |
| --- | --- | --- | --- |
| `POST` | `/preview` | Parse, validate and save a Draft; inserts no catalogue data | Import |
| `POST` | `/confirm` | Queue a valid saved Draft | Import |
| `GET` | `/job` | Get one owned job with rows and counts | View or Import |
| `GET` | `/jobs` | List this Host user's jobs for this import type | View or Import |
| `POST` | `/retry` | Retry failed/unprocessed rows | Import |
| `POST` | `/cancel` | Cancel Draft/Queued/Running at a batch boundary | Import |
| `GET` | `/template` | Download the canonical CSV header | View or Import |
| `GET` | `/report` | Download the final CSV row report | View or Import |

Example full URL:

```text
POST https://skolenza-api.onrender.com/api/Module/import/preview
```

## 4. Preview request: multipart FormData

Do not send preview as JSON. Let the browser set the multipart boundary.

| Form field | Required | Format |
| --- | --- | --- |
| `ModuleId` | Yes | Integer from current Host permission/menu |
| `OperationId` | Yes | Granted Import operation ID |
| `RequestId` | Recommended | Client UUID; keep it for retrying the identical request |
| `File` | One source required | One `.csv` or `.xlsx` file |
| `PastedText` | One source required | Header row plus CSV or tab-separated rows; alternative to File |
| `SheetName` | Conditional | Exact XLSX worksheet name when selection is needed |
| `ColumnMappingJson` | Optional | JSON string `{ "TargetField": "Uploaded Header" }` |

Supply exactly one of `File` and `PastedText`.

```ts
function previewBulk(base: string, permission: BulkPermission, file: File) {
  const form = new FormData();
  form.append('ModuleId', String(permission.moduleId));
  form.append('OperationId', String(permission.importOperationId));
  form.append('RequestId', crypto.randomUUID());
  form.append('File', file);
  return http.post<ApiResponse<BulkPreview>>(`${base}/preview`, form);
}
```

Custom header mapping example when the Excel headers are `Code`, `Title`, `Page`:

```ts
form.append('ColumnMappingJson', JSON.stringify({
  ModuleCode: 'Code',
  ModuleName: 'Title',
  PageName: 'Page'
}));
```

Unknown uploaded columns are ignored. Unknown target names, repeated headers, or
using one source column for two targets are rejected.

## 5. Excel/CSV contracts and examples

### 5.1 Parent Module Bulk

Required: `ModuleCode`, `ModuleName`, `PageName`, `ModuleScope`.

Optional: `DisplayName`, `URLPath`, `IsModuleDisplayInUI`, `IsCommonMenu`,
`IsActive`, `ImageIconWeb`, `ImageIconMobile`, `ItemPriority`, `Remark`.

```csv
ModuleCode,ModuleName,PageName,DisplayName,URLPath,IsModuleDisplayInUI,IsCommonMenu,ModuleScope,IsActive,ImageIconWeb,ImageIconMobile,ItemPriority,Remark
HOST_QA,Host-QA,host-qa,Host QA,/app/host-qa,true,false,2,true,bi bi-check,check,800,QA catalogue
TENANT_QA,Tenant-QA,tenant-qa,Tenant QA,/app/tenant-qa,true,false,1,true,bi bi-check,check,810,Tenant feature catalogue
```

`ParentModuleId` is server controlled and always null for these parent rows.

### 5.2 Child Module Bulk

Required: `ParentModuleCode`, `ModuleCode`, `ModuleName`, `PageName`,
`ModuleScope`. The parent must already be active in the same scope.

Optional fields are the same as Parent Module Bulk.

```csv
ParentModuleCode,ModuleCode,ModuleName,PageName,DisplayName,URLPath,ModuleScope,IsActive,ItemPriority,Remark
HOST_MODULES,HOST_QA_REPORTS,Host-QA-Reports,host-qa-reports,QA Reports,/app/host-qa/reports,2,true,805,Host QA reports
EMPLOYEE_TYPE,TENANT_QA_TYPES,Tenant-QA-Types,tenant-qa-types,QA Types,/app/qa-types,1,true,815,Tenant QA types
```

Upload parent modules first. Wait for their job to complete before previewing
children that reference them.

### 5.3 Operation Bulk

Required: `OperationName`, `OperationType`. `OperationType` must be a positive
integer and is the stable lookup identity.

Optional: `Remark`, `IsActive`, `IconImage`.

```csv
OperationName,OperationType,Remark,IsActive,IconImage
Approve QA,101,Approve QA records,true,bi bi-check
Reject QA,102,Reject QA records,true,bi bi-x
```

If an OperationType already exists with the same name, the row is Existing. If it
exists with another name, preview is Invalid because `OperationName` is immutable.

### 5.4 Module-Operation Mapping Bulk

Required: `ModuleCode`, `OperationType`.

Optional: `DataViewStructureId`, `PageTypeId`, `PageURL`, `IconURL`,
`IsCommonItem`, `IsOperational`, `Priority`, `Remark`, `IsActive`.

```csv
ModuleCode,OperationType,PageURL,IconURL,IsCommonItem,IsOperational,Priority,Remark,IsActive
HOST_QA_REPORTS,101,/app/host-qa/reports/approve,bi bi-check,false,true,10,Approve action,true
HOST_QA_REPORTS,102,/app/host-qa/reports/reject,bi bi-x,false,true,20,Reject action,true
```

The Module and Operation must already exist and be active. The API never creates
missing dependencies automatically.

### 5.5 Forbidden spreadsheet columns

Never include `Id`, `TenantId`, `ParentModuleId`, `ModuleId`, `OperationId`,
`HostUserId`, `AddedById`, `UpdatedById`, assignment IDs or permission IDs. The API
rejects these fields even if the UI does not map them.

## 6. Preview response

All JSON uses the existing `ApiResponse` envelope. Enum values are numeric.

```json
{
  "isSucceeded": true,
  "message": "",
  "data": {
    "jobId": "caab58f2-23e3-4c11-a62f-87145bb9193a",
    "master": 8,
    "sourceColumns": ["ModuleCode", "ModuleName", "PageName", "ModuleScope"],
    "columnMapping": {
      "ModuleCode": "ModuleCode",
      "ModuleName": "ModuleName",
      "PageName": "PageName",
      "ModuleScope": "ModuleScope"
    },
    "errors": [],
    "rows": [
      {
        "hostRecordId": null,
        "processed": false,
        "rowNumber": 2,
        "values": {
          "ModuleCode": "HOST_QA",
          "ModuleName": "Host-QA",
          "PageName": "host-qa",
          "ModuleScope": "2"
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

Master values: Module `8`, Child Module `9`, Operation `10`, Mapping `11`.

Row status values: Ready `1`, Existing `2`, Invalid `3`, Created `4`, Failed `5`.
Render row errors and global `data.errors`. Enable Confirm only when
`data.canCommit === true`. Preview never inserts into catalogue tables.

## 7. Confirm and scheduling

```http
POST /api/Module/import/confirm
Content-Type: application/json
```

```json
{
  "jobId": "caab58f2-23e3-4c11-a62f-87145bb9193a",
  "moduleId": 82,
  "operationId": 23,
  "scheduledAtUtc": null
}
```

Use `scheduledAtUtc: null` to queue now. For scheduling, send a future ISO UTC
value such as `"2026-09-14T01:30:00Z"`. HTTP 200 means the job was accepted; it
does not mean rows have finished inserting. Confirm never accepts replacement rows.

## 8. Poll job completion

```http
GET /api/Module/import/job?JobId=caab58f2-23e3-4c11-a62f-87145bb9193a&ModuleId=82&OperationId=4
```

```json
{
  "isSucceeded": true,
  "data": {
    "jobId": "caab58f2-23e3-4c11-a62f-87145bb9193a",
    "master": 8,
    "status": 4,
    "createdAtUtc": "2026-09-13T10:00:00Z",
    "updatedAtUtc": "2026-09-13T10:00:03Z",
    "scheduledAtUtc": null,
    "totalRows": 2,
    "processedRows": 2,
    "createdCount": 1,
    "existingCount": 1,
    "failedCount": 0,
    "error": null,
    "preview": {
      "rows": [
        { "rowNumber": 2, "hostRecordId": 120, "processed": true, "status": 4, "values": { "ModuleCode": "HOST_QA" }, "errors": [] },
        { "rowNumber": 3, "hostRecordId": 72, "processed": true, "status": 2, "values": { "ModuleCode": "HOST_MODULES" }, "errors": [] }
      ]
    }
  },
  "errors": []
}
```

Job status values:

| Value | Status | UI behavior |
| --- | --- | --- |
| 1 | Draft | Show preview and Confirm/Cancel |
| 2 | Queued | Poll; allow Cancel |
| 3 | Running | Poll; allow Cancel; committed rows may already exist |
| 4 | Completed | Stop polling; show counts/report |
| 5 | CompletedWithErrors | Stop polling; show Retry/report |
| 6 | Failed | Stop polling; show job error and Retry |
| 7 | Cancelled | Stop polling; committed rows remain |

Poll every 2–5 seconds and stop on `4`, `5`, `6`, or `7`.

## 9. History, retry, cancellation and report

History:

```http
GET /api/Module/import/jobs?ModuleId=82&OperationId=4&PageNumber=1&PageSize=20
```

The response `data` is a newest-first job array. Request another page until fewer
than `PageSize` records are returned.

Retry body:

```json
{
  "jobId": "caab58f2-23e3-4c11-a62f-87145bb9193a",
  "moduleId": 82,
  "operationId": 23,
  "scheduledAtUtc": null
}
```

Retry only Failed or CompletedWithErrors jobs. Already Created rows are not
inserted again.

Cancel body:

```json
{
  "jobId": "caab58f2-23e3-4c11-a62f-87145bb9193a",
  "moduleId": 82,
  "operationId": 23
}
```

Cancellation works for Draft, Queued and Running jobs at worker batch boundaries.
It does not roll back rows already committed. Terminal jobs cannot be undone.

Report:

```http
GET /api/Module/import/report?JobId=caab58f2-23e3-4c11-a62f-87145bb9193a&ModuleId=82&OperationId=4
```

Treat the response as Blob (`text/csv`). Report columns are `RowNumber`, `Status`,
`RecordId`, `Values`, and `Errors`.

## 10. Template download

```ts
http.get(`${base}/template`, {
  params: { ModuleId: moduleId, OperationId: viewOperationId },
  responseType: 'blob'
});
```

The response is raw `text/csv`, not an `ApiResponse`. The header contains every
supported field. The UI may offer this as “Download template”.

## 11. UI screen behavior

Recommended screen sequence:

1. Load the current Host menu and find the module by stable ModuleCode.
2. Let the user download a template or select CSV/XLSX/paste.
3. Show detected headers and explicit target-to-source mapping.
4. Submit preview and show Ready, Existing and Invalid rows separately.
5. Keep Confirm disabled unless `canCommit` is true.
6. Ask for explicit confirmation and optional schedule time.
7. Confirm, then poll the job.
8. Show Created/Existing/Failed counts and row messages.
9. Offer report download; offer Retry only for statuses 5 or 6.
10. Refresh the relevant catalogue list after completion.

Do not allow inline editing of saved preview rows. The user must correct the file
or pasted data and create a new preview with a new RequestId.

## 12. Error handling

- `400`: bad file/mapping/format, missing dependency, invalid row contract.
- `401`: missing, expired or invalid authentication token.
- `403`: authenticated user lacks the requested Host module/operation permission.
- `404`: job is missing or belongs to another Host actor/import target.
- `409`: request identity/input conflict or current state conflict.
- `500`: unexpected server failure; show correlation/error information available
  from the existing global API handler and do not claim the job succeeded.

Always inspect both HTTP status and the response envelope. A successful preview
can contain invalid rows; `canCommit` is the final confirmation gate.

## 13. Data durability and isolation

Draft rows and job progress are stored in `skolenza.BulkImportJob` as a durable
snapshot. Confirmed worker rows are inserted into the destination tables listed in
section 1. Job access is restricted to the same authenticated Host actor, selected
bulk target and permission context. Worker execution rechecks the persisted Host
permission before inserting. There is currently no automatic job TTL/purge.

Existing catalogue records are never updated by bulk import. Module `PageName` and
Operation `OperationName` remain unchanged for existing records. Concurrent unique
or foreign-key conflicts become row failures in the final report.

## 14. Backend verification evidence

- Build passed.
- Mapper, permission and controller contract tests: 35 passed, 0 failed/skipped.
- Isolated PostgreSQL lifecycle: 1 passed, 0 failed/skipped.
- Lifecycle created Module → Child Module → Operation → Mapping and verified replay
  became Existing.
- Targeted Host seed passed twice and retained correct hierarchy/mapping counts.
- Evidence files: `artifacts/host-catalogue-bulk-*`.

Production deployment, migration, Host grants and authenticated deployed smoke
testing remain release steps; local test success must not be presented as deployed
production acceptance.
