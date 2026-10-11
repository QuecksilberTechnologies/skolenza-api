# Auto-seed execution record

This folder records database seed and migration execution. A script is marked **EXECUTED** only after a target database connection returns success and its verification query is captured.

## Current run

Status: **SAFE BULK SEED EXECUTION COMPLETE** (development connection from `appsettings.Development.json`).

Executed successfully on 2026-09-11: `AddDurableMasterBulkImport.sql`, `AddEmployeeBulkImport.sql`, `SeedTenantEmployeeTypeModule.sql`, and `SeedBulkImportModules.sql`. Each returned successfully through Npgsql. Existing module names were preserved; scripts are conditional/idempotent.

Scope verification: bulk catalogue rows were inserted/verified with `ModuleScope = 1` (tenant scope). No Host Admin module, operation grant, tenant role grant, or tenant entitlement was inserted by the bulk module seed.

The full consolidated production seed was not replayed because it is a restore/reference script and could overwrite or conflict with existing production data.

The configured development connection is in `skolenza.api/appsettings.Development.json`. Before execution, take a target backup and confirm the target database. Then run scripts sequentially and append timestamp, script name, transaction result, and verification output here.

## Approved execution order

1. `AddDurableMasterBulkImport.sql`
2. `AddEmployeeBulkImport.sql`
3. `SeedTenantEmployeeTypeModule.sql`
4. `SeedBulkImportModules.sql`
5. `production-seed/04-access-and-host/001-modules-operations-two-host-admins.sql` only as part of the documented canonical reset sequence

The complete seed-data file is a reference/restore script, not an automatic replacement for the canonical production seed. Destructive scripts (`ClearTenantDependencies.sql`, reset/replace scripts, and full backup restore) are excluded from automatic execution.

## Scope recorded for the bulk scripts

- Existing module names are preserved; inserts are conditional on module code.
- EmployeeType operations remain CRUD plus the required `Import` operation.
- Bulk mappings are added for Employee, Department, Designation, Role/Permission, and EmployeeType.
- Role grants and tenant enablement remain on the existing permission pipeline.

## Host bulk validation — 2026-09-12

AddHostBulkImport.sql and SeedHostBulkImportModules.sql were executed successfully
twice against the isolated local PostgreSQL database skolenza_bulk_test on port
55439. Both runs succeeded; two Host modules and four View/Import mappings remained.
No target production database execution occurred in this session.
Both consolidated seed references contain the same Host menu block. Existing
production installations should use the two targeted scripts or the migration
runner's -HostBulkOnly option. No tenant plan mappings or role grants are inserted
by this scope-2 seed; the existing Host role permission flow owns grants.

## Existing-module bulk operation cleanup — 2026-09-13

This supersedes the earlier child-module seed record above. A fresh target backup
was created before execution. `SeedBulkImportModules.sql` and
`SeedHostBulkImportModules.sql` then completed successfully against the configured
Render development database. Target verification returned:

- zero Module rows whose ModuleCode contains `BULK`;
- zero orphan TenantEnabledModule and TenantEnabledOperation rows;
- exactly 22 active Import/Export mappings on eleven existing functional modules;
- zero duplicate functional ModuleId/OperationId mappings;
- EMP_LIST restored under EMP_MGMT and four tenant masters under TENANT_MGMT.

The complete consolidated seed also completed first-run and rerun on isolated
`skolenza_bulk_test`. Focused automated validation passed 36/36 with no skips.
See `docs/testing/bulk/module-operation-cleanup/2026-09-13.md` for the mapping,
commands, evidence and remaining deployed API/menu smoke checks.
