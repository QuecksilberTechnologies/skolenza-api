# Employee list assigned roles

## Purpose and status

`GET /api/Employee/get-all` now returns the Employee's current active role
assignments in `assignedRoles`. The route, query parameters, paging envelope and
all existing Employee fields remain unchanged. Backend implementation and local
contract verification are complete; deployed Render verification is pending the
next deployment.

## Authentication and permission

Send the current bearer access token. Resolve `ModuleId` and `OperationId`
dynamically through the existing authenticated menu/permission response. For the
Employee list, use the `EMP_LIST` module's `View` operation. Numeric IDs shown in
examples are illustrative and must not be hard-coded.

## Request

```http
GET /api/Employee/get-all?UserEmployeeId={encodedViewerEmployeeId}&PageNumber=1&PageSize=10&SortBy=employeeId&SortOrder=desc&ModuleId={employeeModuleId}&OperationId={viewOperationId}
Authorization: Bearer {accessToken}
```

There is no request body. `UserEmployeeId`, `ModuleId` and `OperationId` follow
the endpoint's existing permission contract. Paging/filter fields retain their
existing behavior.

## Response example

```json
{
  "isSucceeded": true,
  "message": "Employee info retrieved successfully.",
  "data": [
    {
      "employeeId": "1OZ8MZRL",
      "employementCode": "QT/2018/0001",
      "firstName": "Example Employee",
      "maxRoleAssigned": 2,
      "assignedRoles": [
        {
          "id": 1,
          "code": "SUPER_ADMIN",
          "name": "Super Admin",
          "type": "SYSTEM"
        }
      ]
    },
    {
      "employeeId": "ANOTHER_ENCODED_ID",
      "employementCode": "QT/2026/0201",
      "firstName": "No Role Employee",
      "maxRoleAssigned": 2,
      "assignedRoles": []
    }
  ],
  "errors": [],
  "pageNumber": 1,
  "pageSize": 10,
  "totalRecords": 2,
  "totalPages": 1
}
```

Each `assignedRoles` item contains:

| Property | Type | Meaning |
| --- | --- | --- |
| `id` | integer | Raw `Role.Id`, as approved for this response contract. |
| `code` | string | Role name normalized to uppercase underscore form; for example `Super Admin` becomes `SUPER_ADMIN`. The current schema has no separate persisted RoleCode column. |
| `name` | string | Persisted `Role.RoleName`. |
| `type` | string | `SYSTEM` when `Role.IsSystemDefault=true`; otherwise `CUSTOM`. |

Only active, non-soft-deleted `UserRole` assignments whose Role is active,
non-soft-deleted and owned by the same Tenant are returned. Duplicate active
assignments for one Role are collapsed by `RoleId`, ordered by `id`, and an
Employee with no effective roles receives `[]`, never `null`.

## UI behavior

Render `assignedRoles` directly as role chips or names. Use `id` when an existing
role-management action needs the Role identity. Treat `code` as a response label,
not as a persisted identifier. `maxRoleAssigned` continues to describe the
assignment limit; it is not the count of `assignedRoles`.

The endpoint writes no data. It reads `Employee`, `UserRole` and `Role` records;
the assigned-role query runs once for all Employees on the returned page.

Standard authentication and permission errors keep the existing API envelope:
an invalid/expired token produces `401`, while an authenticated permission denial
produces `403`.

## Verification

- Release automation-project build: PASS, 0 errors.
- JSON contract/default empty-array test: PASS locally.
- Disposable PostgreSQL role filtering/deduplication test: implemented; execution
  requires `SKOLENZA_BULK_TEST_CONNECTION` for the isolated
  `skolenza_bulk_test` database.
- Render/live response verification: PENDING after deployment.

