# Canonical staff member route naming handoff

The Angular client and API use these canonical routes after the 2026-10-11 naming alignment:

- `GET /api/device-ddl-options/staff-member-device-credentials`
- `GET /api/device-ddl-options/staff-member-device-access-windows`
- `POST /api/Institution/add-staff-member-code-pattern`
- `PUT /api/Institution/update-staff-member-code-pattern`
- `GET /api/Institution/get-staff-member-code-pattern`
- `GET /api/StatData/Dashboard/StaffMembers/Statistics`

Authentication and permission IDs continue through the existing authenticated menu/permission pipeline. UI code must not hard-code numeric module or operation IDs. Request and response DTO shapes are unchanged; only route spelling was aligned. The device DDL endpoints return the existing `ApiResponse<DeviceDdlField[]>` contract. Code-pattern writes retain their existing preview/confirm workflow and permission checks.

Local verification: .NET Release build passed, 125 focused API tests passed, Angular production build passed, 83 focused Angular tests passed, and all six routes were present in local Swagger. This is local verification, not deployment acceptance.
