# Super-Admin Dashboard API

All routes are under `/api/Dashboard/SuperAdmin`. They are intentionally separate so the UI can
load each card independently. Every request requires the bearer token plus `moduleId` and
`operationId` resolved from the authenticated menu/permission flow. The handler permits only the
Tenant Super-Admin role type and all database reads are tenant scoped.

| Widget | Method and route | Source |
| --- | --- | --- |
| Top counters | `GET /Summary` | Employee; open positions and pending approvals are temporary values |
| Employee overview | `GET /EmployeeOverview` | Employee and approved current LeaveRequest |
| Upcoming birthdays | `GET /Birthdays?days=30` | Employee, Department, Designation, primary/first active Role |
| Recent onboarding | `GET /Onboarding?limit=10` | Employee, Department, Designation, Role |
| Department headcount | `GET /DepartmentHeadcount` | active Department and active Employee |
| Currently on leave | `GET /CurrentlyOnLeave` | approved current LeaveRequest and Employee |
| Locations | `GET /Locations?limit=3` | TenantLocation |
| Storage | `GET /Storage` | temporary display data until storage metering is implemented |
| Hiring pipeline | `GET /HiringPipeline` | temporary display data until recruitment is implemented |

`days` is optional and is clamped to 1-366. `limit` is optional and is clamped to 1-50.
The leave widgets count a request only when `ApprovedById` is present, `CancellationDate` is null,
and today's date falls inclusively between `FromDate` and `ToDate`. The current leave schema does
not expose a reusable approval-status enum.

Example query:

```http
GET /api/Dashboard/SuperAdmin/Birthdays?moduleId=<resolved-dashboard-module>&operationId=<resolved-view-operation>&days=30
Authorization: Bearer <token>
```

Birthday/onboarding rows return an opaque `employeeId`, `employeeName`, `departmentName`,
`designationName`, `roleName`, and the relevant date. Never attempt to derive a numeric employee ID.

Representative success payload (the standard `ApiResponse` wrapper may contain the usual project
message/status fields in addition to `data`):

```json
{
  "data": [
    {
      "employeeId": "PEZO0QLM",
      "employeeName": "Example Employee",
      "departmentName": "Engineering",
      "designationName": "Developer",
      "roleName": "Employee",
      "date": "2026-10-04T00:00:00"
    }
  ]
}
```

The existing exception middleware returns the project's standard validation/authorization errors.
Missing or invalid permission identifiers are rejected by the permission pipeline. A caller that
has the operation permission but is not the Tenant Super-Admin role type receives `403`. Query
values outside their accepted ranges are clamped rather than retried. These GET endpoints do not
require polling, retry, cancellation, FormData, CSV, or Excel payloads.

All reads execute at request time. No dashboard cache or dashboard persistence table is introduced.
Employee, department, designation, role, leave, and location joins are tenant scoped; soft-deleted
records are excluded where the source entity supports soft deletion.

Temporary values reproduce the current static screen and are response literals in their handlers,
not shared business constants. They must be replaced when the recruitment, approval, and storage
metering sources are implemented. These endpoints are implemented and locally compiled; deployment
and authenticated HTTP verification remain pending. Local verification passed the API build and
three focused contract tests; database-backed and deployed HTTP responses have not been claimed as
tested.
