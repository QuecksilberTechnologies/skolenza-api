# Employee Reset Password

The Reset Password action belongs to the existing Employees screen/module. The standalone Employee Password Management module has been permanently removed.

## Permission contract

- Module code: `EMP_LIST`
- Operation: `Reset Password`
- Operation database Id observed in the configured Development DB: `21`
- UI must resolve the current `moduleId` and `operationId` through the authenticated menu/permission response. Do not hard-code IDs.

## API

`POST /api/Employee/reset-password`

```json
{
  "moduleId": 8,
  "operationId": 21,
  "employeeId": "<encoded employee id>",
  "newPassword": "<new password>",
  "confirmPassword": "<same new password>"
}
```

`employeeId`, `newPassword`, and `confirmPassword` are mandatory. The two password values must match. Tenant identity and actor identity come from authentication. Password values must never be logged or persisted as plain text.

Representative success response:

```json
{
  "isSucceeded": true,
  "message": "Employee password reset successfully.",
  "data": true,
  "errors": []
}
```

The existing permission pipeline verifies that the supplied module resolves to `EMP_LIST`, the actor has Operation `Reset Password`, and the selected employee belongs to the authenticated tenant. The handler hashes the password and updates the employee's active login credential.

## Persistence

- `ModuleOperationMapping`: active mapping between `EMP_LIST` and Operation Id 21.
- `TenantEnabledOperation`: tenant entitlement moved from Module 38 to `EMP_LIST`.
- `RoleModuleAndPermission`: existing Reset Password role grant moved from Module 38 to `EMP_LIST`.
- `Operation`: Id 21 remains active; it is not deleted or recreated.
- Module 38 and all its dependent rows are removed from `Module`, `ModuleOperationMapping`, `TenantEnabledModule`, `TenantEnabledOperation`, `RoleModuleAndPermission`, and `PlanModuleMapping`.

## Status

The Development-configured DB migration and local automated tests were run on 2026-09-23: 3 passed, 0 failed, 0 skipped. See the [scenario report](../testing/employee/reset-password-module-consolidation/2026-09-23.md). No deployed environment or authenticated live HTTP request was tested.
