# Host catalogue bulk import handoff

Updated: 2026-09-13. These APIs are Host-only and use the existing Host role
permission pipeline. Grant View/Import on the matching existing scope-2 module first.
Export is catalogue permission metadata; no export business endpoint was added.

## Shared API lifecycle

Each base exposes `POST preview`, `POST confirm`, `GET job`, `GET jobs`,
`POST retry`, `POST cancel`, `GET template`, and `GET report`:

| Import | Base URL | Permission module code |
| --- | --- | --- |
| Parent Module | `/api/Module/import` | `HOST_MODULES` |
| Child Module | `/api/SubModule/import` | `HOST_SUBMODULES` |
| Operation | `/api/Operation/import` | `HOST_OPERATIONS` |
| Operation mapping | `/api/ModuleOperation/import` | `HOST_MODULE_OPERATIONS` |

Preview is multipart form data containing `ModuleId`, `OperationId`, one `File`
or `PastedText`, optional `RequestId`, `SheetName`, and `ColumnMappingJson`.
Confirm/retry accepts `jobId`, `moduleId`, `operationId`, and optional
`scheduledAtUtc`. Job/report uses the same IDs as query parameters. Follow the
order Module → Child Module → Operation → Mapping when one file depends on another.

## Input examples

Parent Module mandatory fields are `ModuleCode`, `ModuleName`, `PageName`, and
`ModuleScope` (1 Tenant, 2 Host). Example:

```csv
ModuleCode,ModuleName,PageName,DisplayName,URLPath,ModuleScope,IsModuleDisplayInUI,IsCommonMenu,IsActive
HOST_QA,Host-QA,host-qa,Host QA,/app/host-qa,2,true,false,true
```

Child Module requires those fields plus an active same-scope `ParentModuleCode`:

```csv
ParentModuleCode,ModuleCode,ModuleName,PageName,ModuleScope,IsActive
HOST_QA,HOST_QA_REPORTS,Host-QA-Reports,host-qa-reports,2,true
```

Operation requires `OperationName` and positive `OperationType`:

```csv
OperationName,OperationType,Remark,IsActive,IconImage
Approve QA,101,Approve QA records,true,check
```

Operation mapping requires active dependencies identified by `ModuleCode` and
`OperationType`:

```csv
ModuleCode,OperationType,PageURL,IconURL,IsCommonItem,IsOperational,Priority,Remark,IsActive
HOST_QA_REPORTS,101,/app/host-qa/reports,check,false,true,10,QA approval,true
```

Database `Id`, `ParentModuleId`, `ModuleId`, `OperationId`, TenantId, permission
IDs and audit IDs are forbidden spreadsheet columns. Existing identities are
reported as Existing and are never overwritten. In particular, an existing
Module keeps its PageName and an existing OperationType keeps its OperationName.
A different name for an existing OperationType blocks confirmation. Mapping does
not auto-create missing Module or Operation dependencies.

Preview saves only a draft. Data reaches `Module`, `Operation`, or
`ModuleOperationMapping` when the confirmed worker batch commits. Poll `job` until
Completed, CompletedWithErrors, Failed, or Cancelled, then download `report`.
Draft/Queued/Running jobs may be cancelled at batch boundaries; already committed
rows remain and terminal jobs cannot be undone.

## Database release order

1. Back up the intended database and stop the API/worker.
2. Run `AddHostBulkImport.sql`.
3. Run the consolidated complete seed or `SeedHostBulkImportModules.sql`.
4. Grant Import/Export on each required existing module through the Host role permission flow.
5. Restart and perform authenticated preview/confirm/job/report smoke tests.

Local evidence is in `artifacts/host-catalogue-bulk-*`. The targeted seed passed
twice on the isolated database. A complete consolidated-seed attempt stopped at
its pre-existing mandatory TenantEmailConfig prerequisite before reaching the
embedded Host block; this is recorded as blocked, not as a successful full run.
