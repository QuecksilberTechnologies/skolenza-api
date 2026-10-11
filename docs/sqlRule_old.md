# SQL Rule: Tenant Module and Role Permission Creation

## Purpose

This document records the current application rules used when a tenant is created and its subscribed modules, enabled operations, and Tenant Admin permissions are generated.

## Direct answer: parent versus child module

- `TenantEnabledModule` receives both eligible parent modules and eligible child modules returned for the selected subscription plan.
- `TenantEnabledOperation` is prepared only from modules where `Module.IsLeafNode = true`.
- `RoleModuleAndPermission` is consequently created for the leaf/child module and its operations.
- A parent/container module does not normally receive a `RoleModuleAndPermission` row because it is not included in the leaf-module operation lookup.
- The parent remains available for navigation and hierarchy through `Module.ParentModuleId` and `TenantEnabledModule.ParentModuleId`.

Example:

```text
Employee Management                         Parent/container
├── Employees                               Leaf/child → role permissions
├── Departments                             Leaf/child → role permissions
└── Designations                            Leaf/child → role permissions
```

The stored permission combination is:

```text
RoleId + child/leaf ModuleId + OperationId
```

## Tenant-creation sequence

### 1. Read modules mapped to the selected plan

Source tables:

- `PlanModuleMapping`
- `Module`

`PlanModuleMapping` filters:

```text
SubscriptionPlanId = selected plan ID
IsActive = true
```

Duplicate module IDs are removed. A missing or invalid subscription plan returns no modules and tenant creation stops with `No modules found for selected subscription plan.`

Eligible `Module` filters:

```text
ModuleScope = 1 (Tenant) OR ModuleScope = 3 (Common)
IsModuleDisplayInUI = true
IsActive = true
```

The repository composes the eligible module branches so the required hierarchy is available.

### 2. Create tenant-enabled modules

For every returned subscription module, the application prepares a `TenantEnabledModule` row:

```text
TenantId       = newly created tenant ID
ModuleId       = Module.Id
ParentModuleId = Module.ParentModuleId
IsLeafNode     = Module.IsLeafNode
IsEnabled      = true
```

This step contains both eligible parents and children. It preserves the menu hierarchy for the tenant.

### 3. Select leaf modules for operation mapping

The application applies this filter:

```csharp
subscriptionModules.Where(module => module.IsLeafNode == true)
```

Therefore, only child/leaf modules proceed to operation-level permission preparation.

### 4. Read operations for leaf modules

Source tables:

- `ModuleOperationMapping`
- `Operation`

`ModuleOperationMapping` filters:

```text
ModuleId IN selected leaf-module IDs
IsActive = true
```

The current repository includes the related `Operation` record. This particular query does not add a separate `Operation.IsActive` predicate.

### 5. Create tenant-enabled operations

Each selected module-operation mapping is converted into `TenantEnabledOperation` and assigned the newly created `TenantId`.

Before Tenant Admin permissions are prepared, enabled operations are read using:

```text
TenantEnabledOperation.TenantId = newly created tenant ID
TenantEnabledOperation.IsEnabled = true
```

The result is grouped by `ModuleId`. The current read does not add separate `Module.IsActive` or `Operation.IsActive` filters; its effective filter is the tenant-specific enabled-operation row.

### 6. Find the Tenant Admin role

Source table:

- `Role`

Tenant Admin filters:

```text
TenantId = newly created tenant ID
RoleType = Admin
IsActive = true
IsSoftDeleted = false
IsSystemDefault = false
```

The default Manager and Employee roles may also be created during tenant registration, but this automatic full-permission step targets only the Tenant Admin role.

### 7. Create RoleModuleAndPermission rows

For every enabled child-module operation, the application prepares:

```text
RoleId      = Tenant Admin Role.Id
ModuleId    = enabled child/leaf Module.Id
OperationId = enabled Operation.Id
HasAccess   = true
IsActive    = true
Remark      = Auto-assigned during tenant creation for admin role
```

Before insertion, the in-memory list is deduplicated by:

```text
RoleId + ModuleId + OperationId
```

`RoleModuleAndPermission` has no direct `TenantId` column. Tenant ownership and isolation are derived through `Role.TenantId`.

## Tables involved and responsibility

| Table | Responsibility | Parent row included? |
|---|---|---:|
| `SubscriptionPlan` | Selected tenant plan | Not applicable |
| `PlanModuleMapping` | Maps plan to modules | Can reference a branch used to compose hierarchy |
| `Module` | Module metadata and parent-child hierarchy | Yes |
| `ModuleOperationMapping` | Declares operations available for a module | Leaf/child lookup during tenant creation |
| `Operation` | Operation metadata such as Add, Update, Delete, View, Import, and Export | Not applicable |
| `TenantEnabledModule` | Enables subscribed module hierarchy for one tenant | Yes, parent and child |
| `TenantEnabledOperation` | Enables a module-operation combination for one tenant | Generated from leaf/child modules |
| `Role` | Holds the tenant-owned role | Not applicable |
| `RoleModuleAndPermission` | Grants a role access to a module operation | Child/leaf module in the current creation flow |

## Why Add permission can be missing

For Department, Designation, Role, EmployeeType, or another tenant module, the Add permission will not be generated when any required link is absent:

1. The module is not actively mapped to the selected subscription plan.
2. The module is outside Tenant/Common scope.
3. The module is inactive or hidden from UI plan eligibility.
4. The module is not marked as a leaf node.
5. Its active `ModuleOperationMapping` for the Add operation is missing.
6. Its `TenantEnabledOperation` row is missing or disabled.
7. The Tenant Admin role is inactive, soft deleted, a system-default role, or belongs to another tenant.
8. The final `RoleModuleAndPermission` row is missing, inactive, or does not grant access.

## Diagnostic SQL pattern

Replace the example values before running this read-only diagnostic query:

```sql
SELECT
    role."TenantId",
    role."Id" AS "RoleId",
    role."RoleName",
    module."Id" AS "ModuleId",
    module."ModuleCode",
    module."ParentModuleId",
    module."IsLeafNode",
    operation."Id" AS "OperationId",
    operation."OperationName",
    permission."HasAccess",
    permission."IsActive" AS "PermissionIsActive",
    permission."IsSoftDeleted"
FROM skolenza."RoleModuleAndPermission" AS permission
JOIN skolenza."Role" AS role
  ON role."Id" = permission."RoleId"
JOIN skolenza."Module" AS module
  ON module."Id" = permission."ModuleId"
JOIN skolenza."Operation" AS operation
  ON operation."Id" = permission."OperationId"
WHERE role."TenantId" = :tenant_id
  AND role."IsActive" = TRUE
  AND role."IsSoftDeleted" = FALSE
  AND permission."HasAccess" = TRUE
  AND permission."IsActive" = TRUE
  AND permission."IsSoftDeleted" = FALSE
ORDER BY module."ParentModuleId", module."Id", operation."Id";
```

This query is diagnostic only. It does not change permissions.

## Implementation references

- `skolenza.application/Features/RegistrationCmd/Handlers/CreateTenantCommandHandler.cs`
- `skolenza.persistance/Repositories/PlanModuleMappingRepository.cs`
- `skolenza.persistance/Repositories/UserRolesPermissionOnModuleRepository.cs`
- `skolenza.persistance/Repositories/RoleRepository.cs`
- `skolenza.domain/Entity/RoleModuleAndPermission.cs`

## Maintenance rule

When module seed data changes, keep these relationships consistent:

```text
Module
→ ModuleOperationMapping
→ PlanModuleMapping
→ TenantEnabledModule
→ TenantEnabledOperation
→ RoleModuleAndPermission
```

Adding a module row alone does not grant permission. Its plan mapping, leaf classification, active operation mappings, tenant enablement, and role permission grant must all be present in the relevant flow.

## Host entitlement synchronization rule

The Host Admin entitlement synchronization API now completes this sequence inside the existing transaction:

```text
Active TenantSubscription and SubscriptionPlan
→ missing TenantEnabledModule rows
→ missing TenantEnabledOperation rows
→ save the entitlement snapshot
→ missing Tenant Admin RoleModuleAndPermission rows
→ commit
```

The final permission synchronization uses these source and target tables:

| Purpose | Table | Filter |
|---|---|---|
| Identify Tenant Admin | `Role` | Matching `TenantId`, Admin role type, active, not soft deleted, not system default |
| Identify entitled operations | `TenantEnabledOperation` | Matching `TenantId` and `IsEnabled = true` |
| Detect existing grants | `RoleModuleAndPermission` | Matching Tenant Admin `RoleId`, `ModuleId`, and `OperationId` |
| Insert missing grants | `RoleModuleAndPermission` | Only combinations for which no row currently exists |

Inserted permissions use:

```text
RoleId        = active Tenant Admin role ID
ModuleId      = enabled TenantEnabledOperation.ModuleId
OperationId   = enabled TenantEnabledOperation.OperationId
HasAccess     = true
IsActive      = true
IsSoftDeleted = false
AddedById     = authenticated Host user ID
```

The existence check deliberately considers every existing row for the exact `RoleId + ModuleId + OperationId` combination, including an inactive or soft-deleted row. Such a row is left unchanged and a second row is not inserted. Consequently, the synchronization is additive and duplicate-safe; it does not reactivate, overwrite, or delete an existing permission decision.

Only the active Tenant Admin role receives missing permissions. Other Tenant roles remain unchanged.

## Singular tenant master navigation hierarchy

The authoritative module seed groups the existing master pages under singular,
non-leaf navigation parents. Legacy leaf ModuleCode values are renamed in place
to the canonical singular identities; their database IDs, PageName values,
routes, and dependent foreign-key rows remain unchanged.

| Parent ModuleCode | Parent display name | Existing child ModuleCode | Existing child PageName |
|---|---|---|---|
| `TENANT_DEPARTMENT` | Tenant Department | `DEPARTMENT` | `tenant-departments` |
| `TENANT_DESIGNATION` | Tenant Designation | `DESIGNATION` | `tenant-designations` |
| `TENANT_ROLE` | Tenant Role | `ROLE` | `tenant-roles-permissions` |

The parents follow the current module DB rules:

- `ParentModuleId = NULL`; each is an independent root and is not under `TENANT_MGMT`.
- `ModuleScope = 1`, active, displayed in UI, and not a common menu.
- `IsLeafNode = false`.
- `URLPath = NULL` because the parent is a navigation group.
- `PageName`, web/mobile icons, item priority, and remark are populated.
- No `ModuleOperationMapping` is created for a parent.
- Add, Update, Delete, View, Import, and Export remain mapped to each leaf module.

Each parent receives active `PlanModuleMapping` coverage from its own child.
Consequently, both tenant-creation routes use the existing creation pipeline to
create the parent and child `TenantEnabledModule` rows. The leaf's active
`ModuleOperationMapping` rows create `TenantEnabledOperation` rows and default
Tenant Admin `RoleModuleAndPermission` grants.

For an existing tenant, run the Host entitlement synchronization after applying
the seed. The seed repairs the child `ParentModuleId` in `Module` and any
existing `TenantEnabledModule`. Entitlement synchronization adds missing parent
enablement, missing leaf operations, and missing Tenant Admin permissions.

Duplicate prevention uses the following stable combinations:

```text
Module:                  ModuleCode
PlanModuleMapping:       SubscriptionPlanId + ModuleId
TenantEnabledModule:     TenantId + ModuleId (existing sync rule)
TenantEnabledOperation:  TenantId + ModuleId + OperationId (existing sync rule)
RoleModuleAndPermission: RoleId + ModuleId + OperationId (existing sync rule)
```

Re-running the seed repairs the three parent metadata rows, hierarchy, and
active plan mappings. It does not create operations or role grants on the
non-leaf parents.

Legacy leaf migration is ID-preserving:

```text
TENANT_DEPARTMENTS       → DEPARTMENT
TENANT_DESIGNATIONS      → DESIGNATION
TENANT_ROLES_PERMISSIONS → ROLE
```

If both names already exist as different Module rows, the seed raises a conflict
and rolls back instead of guessing which permission/entitlement record to keep.

## EmployeeType module hierarchy

EmployeeType uses the existing Employee Management parent; no additional parent
module is created:

```text
EMP_MGMT
└── EMPLOYEE_TYPE
```

`EMPLOYEE_TYPE` is a Tenant-scope leaf with PageName `tenant-employee-types` and
URL `/employee-types`. Its active `ModuleOperationMapping` catalogue contains
Add, Update, Delete, View, Import, and Export. Active plan coverage inherits from
`EMP_LIST`, keeping it aligned with plans that include employee management.

The seed migrates legacy `TENANT_EMPLOYEE_TYPES` to `EMPLOYEE_TYPE` in place so
existing IDs and dependent entitlement/permission rows are preserved. Tenant
creation grants enabled operations to the default Tenant Admin through the normal
creation pipeline. Host entitlement sync adds only missing enabled operations
and missing Tenant Admin grants for existing tenants.
