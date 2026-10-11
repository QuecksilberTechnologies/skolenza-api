# Full API permission audit — findings

All 442 deployed Swagger endpoints were inspected on 2026-09-12. See API_PERMISSION_ENDPOINT_INVENTORY.md for every endpoint, fields, transport, module/action and matching UI file/line. This is a static contract audit, not 442 authenticated executions.

## Confirmed route/catalogue mismatches

1. POST /api/TenantDevice/create: Add lookup cannot resolve; catalogue provides Assign. Supplied live screenshot confirms missing OperationId.
2. DELETE /api/TenantDevice/delete/{id}: Delete lookup cannot resolve; catalogue provides Remove. Live call not executed.
3. DELETE /api/TenantDeviceConfiguration/delete/{id}: selected TENANT_DEVICE_CONFIG has no Delete action. Ownership/module/grant review required. Live call not executed.
4. GET /api/TenantDevice/get-all accepts a granted `HOST_DEVICE_SETUP` + `View`
   pair and returns HTTP 200. The Host grant check does not bind the supplied
   module to the TenantDevice endpoint's expected `TENANT_DEVICES` module. Confirmed
   by authenticated deployed API testing on 2026-09-13; API-only test scope did not
   change the permission implementation.

## 14 endpoints with UI service callers but no automatic ID mapping

These are candidates for missing IDs, not confirmed failing calls. Values may arrive from callers. Check DTO and actual payload before changing permissions.

| Method | Endpoint | Caller |
|---|---|---|
| POST | /api/Asset/add | src/app/core/services/assets-api.ts:28 |
| PUT | /api/Asset/update | src/app/core/services/assets-api.ts:41 |
| POST | /api/AttendancePolicy/create | src/app/core/services/attendance-policy-api.ts:40 |
| GET | /api/AttendancePolicy/get-all | src/app/core/services/attendance-policy-api.ts:28 |
| POST | /api/AttendancePolicy/update | src/app/core/services/attendance-policy-api.ts:47 |
| POST | /api/AttendancePolicy/update-status | src/app/core/services/attendance-policy-api.ts:56 |
| GET | /api/EmployeeType/get | src/app/core/services/employee-types-api.ts:51 |
| GET | /api/EmployeeType/option | src/app/core/services/employee-types-api.ts:58 |
| DELETE | /api/Insurance/delete | src/app/core/services/policies-insurance-api.ts:95 |
| POST | /api/Asset/Status/add | src/app/core/services/asset-status-api.ts:27 |
| PUT | /api/Asset/Status/update | src/app/core/services/asset-status-api.ts:40 |
| POST | /api/Asset/Type/add | src/app/core/services/asset-types-api.ts:31 |
| PUT | /api/Asset/Type/update | src/app/core/services/asset-types-api.ts:44 |
| DELETE | /api/Asset/Type/delete | src/app/core/services/asset-types-api.ts:51 |


## 28 additional endpoints without automatic mapping or matched service caller

UI implementation may be absent or dynamic; do not classify as an observed missing request.

- POST /api/device-commands/submit
- POST /api/Employee/bulk/send-invitations
- POST /api/Employee/bulk/preview
- POST /api/Employee/bulk/confirm
- GET /api/Employee/bulk/jobs/{jobId}
- GET /api/Employee/bulk/jobs
- POST /api/Employee/bulk/retry
- POST /api/Employee/bulk/cancel
- GET /api/Employee/bulk/template
- GET /api/Employee/bulk/jobs/{jobId}/report
- POST /api/EmployeeDeviceEnrollment/face/upsert
- POST /api/EmployeeDeviceEnrollment/pin/upsert
- POST /api/EmployeeType/bulk/preview
- POST /api/EmployeeType/bulk/confirm
- GET /api/EmployeeType/bulk/jobs/{jobId}
- GET /api/EmployeeType/bulk/jobs
- POST /api/EmployeeType/bulk/retry
- POST /api/EmployeeType/bulk/cancel
- GET /api/EmployeeType/bulk/template
- GET /api/EmployeeType/bulk/jobs/{jobId}/report
- POST /api/EmployeeType/add
- PUT /api/EmployeeType/update
- DELETE /api/EmployeeType/delete
- POST /api/MenuStructure/get-menus-structure
- DELETE /api/PolicyType/delete-doc
- POST /api/TicketClassification/create
- GET /api/TicketClassification/get
- PUT /api/TicketClassification/update


## Other categories

- 187 endpoints have matching static route, field placement and catalogue action. Session grants and caller overrides remain runtime-dependent.
- 199 endpoints declare no ModuleId/OperationId request fields in Swagger. This does not imply public access or no authorization.
- 9 endpoints carry target IDs or special permission contracts; caller permission IDs must not be blindly injected.
- 2 Employee verification/edit-status endpoints resolve module from tabInfoType and require payload-specific checks.

## Corrections and limits

Tenant/update-modules-and-operations contains target IDs inside modules/operations arrays; it is not a confirmed missing-permission-fields defect.
Employee/Sensitive/update is called by the UI but absent from deployed Swagger; the local backend route is commented out. Treat it as a separate route-contract issue.
This inspection made no UI code, permission grant or database changes.
