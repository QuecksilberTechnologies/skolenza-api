# Tenant Plan Entitlement: Pre-Implementation Analysis Guide

Last updated: 2026-08-29

## Purpose

Use this document **before changing any subscription plan, module, operation, role, or authorization code**. Its purpose is to prevent an implementation from changing an existing Tenant's purchased access accidentally.

The required architecture is an **entitlement snapshot**:

> When a Tenant purchases a plan, the system copies that plan's entitled modules and operations into Tenant-specific entitlement tables. Later changes to the product plan must not automatically change existing Tenants.

## Non-negotiable decision

Do **not** remove or replace these tables:

```text
TenantEnabledModule
TenantEnabledOperation
```

They are the Tenant entitlement snapshot and are required for menu visibility, API authorization, and role-level permissions.

## 1. Three separate layers

| Layer | Tables | Meaning | Who may change it |
|---|---|---|---|
| Product catalogue | `SubscriptionPlan`, `Module`, `Operation`, `PlanModuleMapping`, `ModuleOperationMapping` | What a plan offers today. | Host product/plan administration. |
| Tenant entitlement snapshot | `TenantSubscription`, `TenantEnabledModule`, `TenantEnabledOperation` | What a specific Tenant actually received at purchase/upgrade time. | Controlled subscription/entitlement workflow only. |
| Role permission | `Role`, `UserRole`, `RoleModuleAndPermission` | Which Tenant role may use which entitled module/operation. | Tenant role administration, constrained by entitlement snapshot. |

```text
                 PRODUCT CATALOGUE

SubscriptionPlan ----> PlanModuleMapping <---- Module
                                               |
                                               v
Operation ----> ModuleOperationMapping <-------+
                                               |
                                               v
                 Tenant purchases / changes plan
                                               |
                                               v
                  TENANT ENTITLEMENT SNAPSHOT

                   TenantEnabledModule
                   TenantEnabledOperation
                             |
                             v
                 ROLE-LEVEL PERMISSION FILTER

                   RoleModuleAndPermission
                             |
                +------------+------------+
                v                         v
          Menu visibility            API action permission
```

## 2. Source-of-truth rule

| Question | Correct source |
|---|---|
| “What does this plan currently include?” | `PlanModuleMapping` + global `Module` + `ModuleOperationMapping` |
| “What did Tenant 62 receive when it purchased?” | `TenantEnabledModule` + `TenantEnabledOperation` for Tenant 62 |
| “Can this role use an operation?” | Active `RoleModuleAndPermission` **and** active Tenant entitlement for the same Tenant/module/operation |
| “Should a plan edit change existing Tenants?” | No—unless an explicit, audited entitlement migration is requested. |

Never use the current `PlanModuleMapping` directly for a Tenant's runtime menu or API permission check. Doing so would make a future plan edit affect old Tenants.

## 3. Required purchase/snapshot flow

The full Tenant plan purchase, renewal, upgrade, or entitlement-provision operation must be transactional.

```text
1. Validate active SubscriptionPlan
2. Read active PlanModuleMapping rows for that plan
3. Expand module hierarchy when the product rule requires parents to be included
4. Insert/update TenantSubscription
5. Snapshot selected modules into TenantEnabledModule
6. Read ModuleOperationMapping for the entitled modules
7. Snapshot allowed operations into TenantEnabledOperation
8. Create/reconcile role permissions only within the entitlement set
9. Set audit fields for every changed row
10. Save and commit one transaction
```

### Snapshot fields to verify

Before implementation, verify the real schema and entity fields for:

```text
TenantEnabledModule
  TenantId, ModuleId, ParentModuleId, IsLeafNode, IsEnabled,
  AddedById, AddedDateTime, UpdatedById, UpdatedDateTime

TenantEnabledOperation
  TenantId, ModuleId, OperationId, IsEnabled, IsOperationUsed,
  AddedById, AddedDateTime, UpdatedById, UpdatedDateTime
```

### Snapshot creation rules

- Insert only the modules selected from the purchased plan.
- Create operations only for the modules in the Tenant snapshot.
- Set `IsEnabled = true` only for newly provisioned entitlement records.
- Preserve existing disabled state unless the business request explicitly says to re-enable it.
- Do not set `AddedById = TenantId` by assumption; use the authenticated audit actor required by the established flow.
- Use unique business keys to prevent duplicate snapshot rows:

```text
TenantEnabledModule:    TenantId + ModuleId
TenantEnabledOperation: TenantId + ModuleId + OperationId
```

## 4. Runtime authorization flow

```text
Authenticated user
  -> trusted Tenant from token
  -> active user role(s)
  -> RoleModuleAndPermission: role allows module/operation
  -> TenantEnabledModule: Tenant has enabled module
  -> TenantEnabledOperation: Tenant has enabled operation
  -> allow menu/API action
```

The effective permission is the intersection:

```text
effective access =
  role permission is active
  AND Tenant module entitlement is enabled
  AND Tenant operation entitlement is enabled
  AND all records belong to the same Tenant/module/operation
```

Role permissions must never grant an operation that is absent or disabled in the Tenant snapshot.

## 5. Required Tenant ownership in queries

Every runtime entitlement/permission query must check the same Tenant boundary.

Correct join identity:

```text
RoleModuleAndPermission.RoleId = current role
AND TenantEnabledOperation.TenantId = authenticated Tenant
AND TenantEnabledOperation.ModuleId = RoleModuleAndPermission.ModuleId
AND TenantEnabledOperation.OperationId = RoleModuleAndPermission.OperationId
AND both role permission and Tenant operation are active/enabled
```

Do not join `RoleModuleAndPermission` to `TenantEnabledOperation` only by `OperationId`; the same operation can be used by multiple modules and Tenants.

## 6. Menu hierarchy rules

Runtime menu data must be rooted in `TenantEnabledModule`, not in global plan mappings.

```text
TenantEnabledModule
  ParentModuleId = null       -> top-level menu/header
  ParentModuleId has value    -> child under that Tenant-enabled parent
  IsLeafNode = true           -> leaf item
  IsLeafNode = false or null  -> header/container when existing data uses null
```

Important existing-data rule: current rows can contain `IsLeafNode = null`. Header queries must use:

```text
IsLeafNode != true
```

instead of:

```text
IsLeafNode == false
```

The latter excludes null records and can return a successful but empty menu response.

`TenantParentModule/get-module-headers` intentionally reads from `TenantEnabledModule`. Its filters should be:

```text
TenantId matches decrypted requested Tenant
ParentModuleId is null for headers
IsLeafNode is not true
IsModuleDisplayInUI is true
IsEnabled is optional
```

`ModuleScope` must not be used as an entitlement filter for this Tenant snapshot header endpoint unless a future, explicit business rule introduces scope-specific Tenant entitlements.

## 7. Existing code: verified points and implementation risks

The following are observations from the current codebase. They are not automatic approval to change any flow.

| Area | Current state | Required analysis before change |
|---|---|---|
| Tenant snapshot tables | `WorkforceDbContext` maps `TenantEnabledModules` and `TenantEnabledOperations`. | Preserve them as the runtime entitlement source. |
| Snapshot preparation | `TenantModuleConfigurationRepository.CreateByDefaultEnabledModulesAsync(...)` accepts module/operation entities and assigns Tenant/audit/default values. | Confirm the caller builds them only from the purchased plan's active mappings and uses the correct audit actor. |
| Plan mapping | `SavePlanModuleMappingCommandHandler` updates product-plan mappings atomically. | Verify it never alters existing Tenant entitlement rows. |
| Tenant module header API | `TenantParentModuleRepository.GetHeaderTreeAsync(...)` reads `TenantEnabledModule`. | Keep Tenant ownership; treat `IsLeafNode = null` as non-leaf; do not require module scope for this endpoint. |
| Tenant module/operation update | `TenantEnabledModuleOperationsUpdateCommandHandler` currently returns success through a placeholder (`isUpdated = true`) and does not persist the requested change. | Implement only after defining upgrade/downgrade/cascade rules and transaction semantics. |
| Role permission check | `PermissionRepository.GetPermissionsByRoleAsync(...)` currently joins by operation. | Review and enforce TenantId + ModuleId + OperationId identity; invalidate cache whenever role or entitlement changes. |

## 8. Plan edit, upgrade, downgrade, and renewal decisions

These four actions are different. Never implement one using another action's rules.

| Action | Expected effect on existing Tenant snapshot |
|---|---|
| Edit plan catalogue | No change to existing Tenants. Only future purchases use the edited plan. |
| New Tenant purchase | Create a fresh entitlement snapshot from the purchased plan. |
| Upgrade | Explicitly add the new plan's permitted modules/operations; decide whether prior custom disabled states remain unchanged. |
| Downgrade | Explicitly decide which entitlements are disabled/removed, what happens to roles and feature data, and how authorization reacts immediately. |
| Renewal of same plan | Preserve snapshot unless the business request explicitly includes a reconciliation/migration. |
| Admin entitlement override | Store it as Tenant-specific entitlement state with audit history; never edit the global plan merely to change one Tenant. |

Before implementing an upgrade or downgrade, the product owner must decide:

1. Whether removed features are soft-disabled or hard-deleted from snapshot tables.
2. Whether existing role permission rows are disabled, retained but ineffective, or deleted.
3. Whether historical feature data remains readable.
4. Whether an active user must log in again or whether permission cache is invalidated immediately.
5. Whether the operation has a preview/report of the entitlement delta before commit.

## 9. API and security analysis checklist

Before adding or changing an entitlement endpoint, answer all of these questions:

### Request boundary

- Is this a Host-only endpoint, Tenant admin endpoint, or Tenant employee endpoint?
- Does a Host request use the existing central Host runtime permission validator?
- Does a Tenant request derive Tenant scope from the trusted token?
- If a Tenant ID is accepted, is it encrypted/decrypted through the existing identifier-protection flow?
- Are `ModuleId` and `OperationId` required for normal users while the Super Admin bypass remains central?

### Data selection

- Is the operation reading the product catalogue or the Tenant snapshot?
- Does every read/write contain Tenant ownership validation?
- Are active/non-soft-deleted states filtered correctly for the relevant table?
- Does the query handle nullable `IsLeafNode` values consistently?

### Change safety

- Does the operation run in one transaction where multiple tables change?
- Are audit fields set from the real authenticated actor?
- Are duplicate Tenant module/operation snapshot records prevented?
- Is cache invalidation defined after a role, entitlement, or subscription change?
- Is there a rollback path for all writes?

### Response safety

- Does the response expose only the safe DTO shape?
- Are raw Tenant IDs, passwords, encryption keys, hashes, and refresh tokens excluded?
- Is the returned Tenant ID encrypted where applicable?

## 10. Minimum test matrix

Run these cases before delivering an entitlement-related implementation.

| Test | Expected result |
|---|---|
| Create plan A with modules M1/M2, then Tenant buys plan A | Tenant snapshot contains only M1/M2 and their selected operations. |
| Edit plan A later to add M3 | Existing Tenant snapshot stays M1/M2. New purchaser receives M1/M2/M3. |
| Disable Tenant M2 entitlement | Menu hides M2; M2 API operations are denied even if role permission still exists. |
| Role grants operation not entitled for Tenant | Access is denied. |
| Role grants an entitled operation | Access is allowed only when role and entitlement records are active. |
| Tenant header rows use `IsLeafNode = null` | Header tree returns them. |
| `IsEnabled = true` / `false` / omitted | Endpoint returns matching enabled rows / matching disabled rows / both states respectively. |
| Normal Host user calls admin endpoint | Existing ModuleId + OperationId permission validation is required. |
| Super Admin calls admin endpoint | Existing central bypass works without duplicate auth logic. |
| Plan-update transaction fails midway | No partial Tenant snapshot/role state is committed. |

## 11. Implementation sequence

Use this order for any future entitlement work:

```text
1. Read this document and identify the requested business action.
2. Inspect entity, DbContext, DTO, handler, repository, current API, and actual DB schema.
3. Confirm source-of-truth table: plan catalogue vs Tenant snapshot.
4. Document upgrade/downgrade/override behavior if a snapshot may change.
5. Reuse existing central authentication, encryption, and permission services.
6. Implement the smallest scoped change with a single transaction where needed.
7. Add/retain Swagger XML documentation.
8. Run focused tests, git diff --check, and the required build.
9. Verify that no global plan edit changed a pre-existing Tenant snapshot.
```

## 12. Do not do these things

- Do not delete `TenantEnabledModule` or `TenantEnabledOperation` because plan mappings already exist.
- Do not resolve Tenant runtime menu/API access directly from `PlanModuleMapping`.
- Do not update all Tenants when a global plan mapping changes.
- Do not use a raw Tenant ID when the endpoint contract requires encrypted Tenant IDs.
- Do not add duplicate JWT, encryption, or authorization logic instead of using central flows.
- Do not silently remove role permissions during a plan change without the agreed downgrade policy.
- Do not return generic success from an entitlement update handler if no database write occurred.

