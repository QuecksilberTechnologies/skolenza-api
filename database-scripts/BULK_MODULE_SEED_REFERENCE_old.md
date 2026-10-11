# Bulk module seed reference

Updated: 2026-09-15. This note records which module/operation seed is authoritative
for bulk onboarding and what each seed changes. It does not grant permissions.

## Existing-module operation model — user-confirmed 2026-09-13

Separate Tenant and Host `*BULK*` navigation Module rows are retired. Bulk features
use Import (`OperationType=12`) and Export (`OperationType=11`) mappings on their
existing functional modules. Existing Module `PageName` and Operation
`OperationName` values remain unchanged.

| Bulk feature | Permission ModuleCode |
| --- | --- |
| Employee | `EMP_LIST` |
| Department | `DEPARTMENT` |
| Designation | `DESIGNATION` |
| Role | `ROLE` |
| EmployeeType | `EMPLOYEE_TYPE` |
| Host Card catalogue | `HOST_TENANT_RFID_MANAGEMENT` |
| Host Device catalogue | `HOST_DEVICE_SETUP` |
| Host Module / Child Module / Operation / Mapping | `HOST_MODULES` / `HOST_SUBMODULES` / `HOST_OPERATIONS` / `HOST_MODULE_OPERATIONS` |

`SeedBulkImportModules.sql` and `SeedHostBulkImportModules.sql` migrate matching
existing grants to the functional modules, remove obsolete dependencies from
`TenantEnabledModule`, `TenantEnabledOperation`, role, mapping and plan tables,
then delete the obsolete Module rows in FK-safe order. The complete seed embeds
the same logic and no longer inserts bulk child modules.

Validation: the complete seed ran successfully twice on isolated
`skolenza_bulk_test`; 36 focused tests passed with zero failures/skips. Target DB
was backed up, both targeted seeds completed, and reconciliation returned zero
bulk-named Module rows, zero orphan tenant entitlement rows, 22 active functional
Import/Export mappings and zero duplicates. Evidence and command details:
`docs/testing/bulk/module-operation-cleanup/2026-09-13.md`.

The older child-module sections below are historical and superseded by this
decision wherever they describe the current menu/permission contract.

## Immutable PageName rule — user-confirmed 2026-09-13

Never change an existing Module `PageName` while editing Module, child-module,
Operation or ModuleOperationMapping seed sections. `PageName` is the stable UI
component-registration identity, independent of URLPath and display metadata.
Assign the approved value when a module is first inserted; every later seed rerun
must preserve it. This applies to Tenant and Host module seeds.

## Immutable OperationName rule — user-confirmed 2026-09-13

Never rename or overwrite an existing `Operation.OperationName` in Module,
child-module, Operation or ModuleOperationMapping seed work. Reuse the existing
canonical operation/type. If an operation type is genuinely missing, insert its
approved initial name once; subsequent reruns must preserve the stored name and
must not introduce an alias or duplicate operation.

### Add/Create consolidation — user-confirmed 2026-09-14

This later explicit decision supersedes the immutable-name rule only for the
duplicate Add/Create pair representing `OperationType=1`. The complete seed keeps
one canonical **Add**, migrates ModuleOperationMapping and persisted permission/
entitlement references, and removes redundant Create/Add aliases. Other operation
names remain immutable. UI consumers must continue resolving IDs dynamically.

## Which seed to use?

`database-scripts/production-seed/04-access-and-host/001-modules-operations-two-host-admins.sql`
is now the only authoritative production module/operation seed. It includes the
38 backup-recovered `Module` rows plus the consolidated Common, Host and Tenant
catalogue. The former root-level duplicate was removed on 2026-09-14. IDs are
generated; permission consumers must resolve them through the authenticated flow.

## Bulk-related module entries

| ModuleCode | Scope | Used by |
|---|---:|---|
| `EMP_LIST` | Tenant | Employee bulk preview/confirm/jobs/report/template/invitations |
| `DEPARTMENT` | Tenant | Department bulk |
| `DESIGNATION` | Tenant | Designation bulk |
| `ROLE` | Tenant | Role bulk |
| `EMPLOYEE_TYPE` | Tenant | EmployeeType bulk |
| `TENANT_EMPLOYEE_CODE` | Tenant | Employee-code pattern add/update |

Historical implementation (superseded): the complete seed previously used a functional-parent hierarchy:
`BULK_EMPLOYEES` under `EMP_MGMT`; Department, Designation, Role and EmployeeType
bulk children under their corresponding existing tenant modules. There is no
standalone tenant `BULKUPLOAD` parent. Each bulk child has View (4), Export (11)
and Import (12) catalogue mappings. Export is seed permission metadata only; its
UI implementation belongs to the UI developer.

## Canonical tenant master hierarchy — 2026-09-15

The current leaf identities and hierarchy are:

```text
TENANT_DEPARTMENT (root)
└── DEPARTMENT

TENANT_DESIGNATION (root)
└── DESIGNATION

TENANT_ROLE (root)
└── ROLE

EMP_MGMT (existing root)
└── EMPLOYEE_TYPE
```

The three new root parents have no operation mappings. Each leaf retains its PageName and
URL and owns Add, Update, Delete, View, Import, and Export. The authoritative and
standalone bulk seeds rename a legacy leaf ModuleCode in place, preserving its
ID and therefore its entitlement and permission foreign keys. If both legacy
and canonical rows already exist with different IDs, the seed stops with a
conflict instead of silently merging permission decisions.

Local integration verification (2026-09-13): the entire complete seed ran twice
successfully on an isolated clone after adding its required test-only email-config
fixture. Both runs produced five bulk children, no `BULKUPLOAD` root, exactly 15
active tenant bulk mappings, and five plan mappings per child. Both canonical Host
Admin accounts were present. No production DB was used. Existing duplicate Host
Card/Device Import mappings in the baseline are recorded separately and were not
altered by this tenant-only hierarchy decision.

`EMPLOYEE_TYPE` is also maintained by the idempotent
`database-scripts/SeedTenantEmployeeTypeModule.sql`; the current complete consolidated
seed contains that module as well. That script creates CRUD catalogue/plan coverage;
tenant grants still use entitlement synchronization and the existing permission API.

For the complete bulk catalogue, run the new idempotent
`database-scripts/SeedBulkImportModules.sql`. It ensures the five bulk master
modules, the Import operation (type 12), their `ModuleOperationMapping` rows and
plan inheritance from `EMP_LIST`. It intentionally does not create
`TenantEnabledModule`, `TenantEnabledOperation` or `RoleModuleAndPermission`
grants; run the existing entitlement synchronization and role-permission command
afterward to make Bulk Import appear in a tenant's menu.

Bulk master import uses the existing Add/Import grant. Canonical CRUD operation
types are View=4, Add=1, Update=2, Delete=3; Import is enum value 12 when
the environment has an Import operation. Do not hardcode IDs or grant roles in UI.

## Tables changed

| Seed section | Tables | Effect |
|---|---|---|
| Email setup | `DefaultEmailConfig`, `TenantEmailConfig` | Creates/upgrades email config and active defaults |
| Module definitions | `Module` | Idempotently inserts missing metadata; parents resolve by ModuleCode |
| Operation normalization | `Operation` | Ensures active CRUD metadata/icons |
| Catalogue mappings | `ModuleOperationMapping` | Adds/updates module-operation catalogue rows |
| Plan inheritance | `PlanModuleMapping` | Copies selected baseline modules to subscription plans |
| EmployeeType add-on | `Module`, `ModuleOperationMapping`, `PlanModuleMapping`, `TenantEnabledOperation`, stale `RoleModuleAndPermission` | Adds CRUD catalogue, cleans stale EmployeeType rows; no role grant |
| Host baseline | Host role/permission tables and related module tables | Normalizes existing Host hierarchy/baseline |

Bulk job storage is separate: run `AddDurableMasterBulkImport.sql` and
`AddEmployeeBulkImport.sql` for `skolenza.BulkImportJob` and Employee uniqueness
constraints.

## Recommended reference and order

Use the canonical seed for deployment, then `SeedBulkImportModules.sql` (which
covers EmployeeType catalogue too; the older EmployeeType-only script remains
safe/idempotent),
existing Host entitlement synchronization, existing tenant role-permission grants,
and finally durable bulk migrations. Verify by stable ModuleCode and authenticated
`my-menu`; never assume numeric IDs. Before merging the 38 backup-only modules into
the canonical seed, review their product scope—they are not all bulk modules.
## Host catalogue bulk seed — 2026-09-13 (superseded child-module design)

`SeedHostBulkImportModules.sql` and the embedded consolidated complete-seed block
add these scope-2 children without `PlanModuleMapping` rows:

| Child code | Parent code | PageName |
| --- | --- | --- |
| HOST_MODULE_CATALOGUE_BULK | HOST_MODULES | host-module-bulk |
| HOST_SUBMODULE_CATALOGUE_BULK | HOST_SUBMODULES | host-submodule-bulk |
| HOST_OPERATION_CATALOGUE_BULK | HOST_OPERATIONS | host-operation-bulk |
| HOST_MODULE_OPERATION_CATALOGUE_BULK | HOST_MODULE_OPERATIONS | host-module-operation-bulk |

Each child maps one canonical View (4), Export (11), and Import (12). Grant them
through the existing Host role permission flow. The seed ran twice on the isolated
`skolenza_bulk_test`; identities and counts remained stable.

The consolidated seed reparents known legacy Host bulk children before deleting
the obsolete `BULKUPLOAD` root. This prevents `FK_Module_ParentModule` failures on
databases seeded by an earlier hierarchy. If an unknown legacy child remains, the
root is hidden/deactivated and retained for review instead of aborting the seed.

