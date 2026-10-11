# Role-based Dashboard Data API

## Behavior

`GET /api/Dashboard/{roleTypeCode}` returns one complete dashboard payload for the authenticated user's trusted RoleType. The UI sends the stable RoleType code in the route. The API validates it against the authenticated user's trusted RoleType and rejects a mismatch.

`Bearer identity -> permission pipeline -> trusted RoleTypeId -> stable RoleTypeCode -> dashboard data`

Supported codes are `TENANT_ADMIN`, `PEOPLE_MANAGER`, `WORKFORCE_USER`, and `EXTERNAL_USER`.

## Request

The bearer token and Dashboard View permission identifiers are mandatory. Resolve `moduleId` and `operationId` through the existing authenticated menu/permission flow; numeric IDs must not be hardcoded.

```http
GET /api/Dashboard/TENANT_ADMIN?moduleId=<dashboard-module>&operationId=<view-operation>
Authorization: Bearer <token>
```

There is no request body. This is a read-only endpoint. Swagger exposes the complete `DashboardDataDTOApiResponse` schema for HTTP 200.

## Response

Only the property matching `roleTypeCode` is populated; the other persona properties are `null`.

```json
{
  "isSucceeded": true,
  "data": {
    "roleTypeCode": "TENANT_ADMIN",
    "roleTypeName": "Tenant Administrator",
    "generatedAtUtc": "2026-10-04T10:00:00Z",
    "tenantAdministrator": {
      "summary": { "data": { "totalEmployees": 25, "newHiresThisMonth": 3, "openPositions": 10, "pendingApprovals": 3 }, "source": "MIXED", "isPlaceholder": false, "placeholderFields": ["OpenPositions", "PendingApprovals"] },
      "employeeOverview": { "data": { "total": 25, "active": 22, "inactive": 1, "onLeave": 2 }, "source": "DATABASE", "isPlaceholder": false },
      "currentlyOnLeave": { "data": [], "source": "DATABASE", "isPlaceholder": false },
      "employeesOnNotice": { "data": [{ "code": "SAMPLE-NOTICE-001", "employeeName": "Neha Singh", "expectedLastWorkingDate": "2026-10-22", "noticeDaysRemaining": 18, "status": "NOTICE_PERIOD" }], "source": "TEMPORARY_STATIC", "isPlaceholder": true },
      "exitedEmployees": { "data": [{ "employeeId": "encoded-id", "employeeName": "Former Employee", "dateOfExit": "2026-10-01T00:00:00" }], "source": "DATABASE", "isPlaceholder": false },
      "locations": { "data": { "total": 2, "active": 2, "headOffice": 1, "locations": [] }, "source": "DATABASE", "isPlaceholder": false },
      "upcomingBirthdays": { "data": [], "source": "DATABASE", "isPlaceholder": false },
      "recentOnboarding": { "data": [], "source": "DATABASE", "isPlaceholder": false },
      "departmentHeadcount": { "data": [], "source": "DATABASE", "isPlaceholder": false },
      "storageStatus": { "data": { "capacityGigabytes": 50, "usedGigabytes": 31.4, "categories": [] }, "source": "TEMPORARY_STATIC", "isPlaceholder": true },
      "hiringPipeline": { "data": { "totalApplicants": 148, "stages": [] }, "source": "TEMPORARY_STATIC", "isPlaceholder": true }
    },
    "peopleManager": null,
    "workforceUser": null,
    "externalUser": null
  }
}
```

`recentOnboarding` covers the last 30 days. `exitedEmployees` is separate and covers `DateOfExit` within the last 7 days. `employeesOnNotice` is temporary static data because an authoritative accepted-resignation source is not currently available.

People Manager returns team summary, attendance, notice, leave, birthdays, and recent onboarding. Workforce User returns profile, attendance, leave, tasks, documents, and announcements. External User returns client summary, open tickets, recent activity, client-side employees, and documents. These three personas currently return `TEMPORARY_STATIC` sections for UI integration.

Each section exposes `source`, `isPlaceholder`, and optional `placeholderFields` so the UI can distinguish database, mixed, and temporary values.

## Errors and status

Authentication, tenant access, and Dashboard View permission use the established pipeline. Invalid access uses the standard error envelope. Unsupported trusted RoleTypes receive permission denied. Local and deployed verification are recorded separately in the scenario report.
