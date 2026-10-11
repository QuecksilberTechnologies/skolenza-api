# EmployeeType Rules

Latest live acceptance, 11 September: COMPLETE. Existing role-permission API granted
module 79/Delete to tenant Admin role 22. Fresh my-menu shows all four CRUD actions.
Dedicated QA type create/update/deactivate/delete succeeded, and the deleted type
is absent from list results. Bulk XLSX/CSV/paste and DB/report checks also passed.
Evidence is in `docs/bulk-upload/results/employee-type-crud-*.json` and
`employee-type-live-menu.json`. Earlier pending notes below are historical.

Current seed decision, 15 September: the existing module is migrated in place to
`EMPLOYEE_TYPE`, a direct leaf child of `EMP_MGMT`. It owns Add, Update, Delete,
View, Import, and Export mappings. Plan coverage inherits from `EMP_LIST`.

## Scope and ownership

- `EmployeeType` is tenant-owned. The authenticated Tenant and employee audit actor are authoritative; clients never supply either value.
- All EmployeeType CRUD actions use the existing `EmployeeTypePermissionBehavior` and `EMPLOYEE_TYPE` module permission pipeline.
- `EMPLOYEE_TYPE` is a Tenant-scope (`ModuleScope = 1`) leaf under the existing Employee Management hierarchy.
- Its master mapping contains CRUD plus bulk operations:
  `Add`, `Update`, `Delete`, `View`, `Import`, and `Export`. Their database IDs come from the Operation master;
  do not select by `OperationType` because legacy master rows reuse those numeric values.

## Active and deletion state

- `IsActive` controls whether an EmployeeType is available for normal selection. It does not make a dependency safe to ignore.
- Delete is a soft delete of the EmployeeType: `IsSoftDeleted = true`, `IsActive = false`, and the soft-delete audit fields are recorded.
- No EmployeeType dependency is cascaded or hard deleted by an EmployeeType delete.

## Delete dependency rule

The delete API returns a conflict when a protected dependency exists. For tables with a soft-delete flag, only rows whose soft-delete flag is `false` or `null` block deletion; active and inactive rows both block.

- `Employee`: non-soft-deleted employee rows block.
- `EmployeesChangedTypeHistory`: history blocks while its related employee is non-soft-deleted. The history table has no soft-delete field of its own.
- `UnStructuredPolicyTypeMappingWithEmployeeType` and `PolicyLeaveTypeMapping`: non-soft-deleted rows block.
- Accommodation, meal, and travel allowance mappings: rows with `IsSoftDelete = false` or `null` block.
- `EmployeeTypeBasicMenu`: any mapping row blocks. This legacy mapping table has no soft-delete columns, so it cannot be safely soft-deleted or ignored based on `IsActive`.

## Change safety

- Rename uniqueness is Tenant-scoped, trimmed, case-insensitive, and ignores soft-deleted EmployeeTypes.
- Update and delete always load the EmployeeType by both `TenantId` and identifier.
- Preserve this document when changing EmployeeType entity relations, CRUD behavior, mapping, permission behavior, or seed metadata.

## Validation

- 2026-09-11: the focused EmployeeType automation suite passed 6 tests with 0 failures.
  Nine database-fixture tests were skipped because `SKOLENZA_BULK_TEST_CONNECTION` was not configured;
  they require the isolated `skolenza_bulk_test` database and are not treated as passes.
- Historical 2026-09-11 live database verification after the earlier corrective seed confirmed that both
  `ModuleOperationMapping` and Tenant 8's `TenantEnabledOperation` have exactly the four canonical
  CRUD operations for the module. The 2026-09-15 seed decision subsequently adds Import and Export.
- 2026-09-11 release correction: handler response alias now uses the same existing
  DTO as repository/AutoMapper. Release publish passed; two new list/paging
  regressions passed with zero failures or skips. Evidence:
  `artifacts/bulk-import/employee-type-response-regression.trx`.
- Live bulk acceptance is complete: XLSX created two types; CSV/paste each skipped
  both. DB/report reconciliation passed. My-menu contains Add/Update/View; Delete
  grant and manual update/delete HTTP acceptance are still pending. The failed
  Render build did not deploy these local CRUD changes.
