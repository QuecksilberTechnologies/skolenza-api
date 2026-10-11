# Institution and Staff API compatibility

The API now publishes Skolenza names as the canonical routes while retaining the AxionPro routes
as compatibility aliases. New Angular calls use the canonical routes.

| Capability | Canonical route | Compatibility route |
|---|---|---|
| Institution administration | `/api/Institution` | `/api/Tenant` |
| Institution type lookup | `/api/InstitutionType` | `/api/TenantIndustry` |
| Campus configuration | `/api/Campus` | `/api/TenantLocation` |
| Staff directory and profile | `/api/Staff` | `/api/Employee` |
| Staff monitoring | `/api/Staff/Monitoring` | `/api/Employee/Monitoring` |
| Staff employment types | `/api/StaffEmploymentType` | `/api/EmployeeType` |
| Staff campus assignment | `/api/StaffCampusAssignment` | `/api/EmployeeLocationAssignment` |
| Staff device enrolment | `/api/StaffDeviceEnrollment` | `/api/EmployeeDeviceEnrollment` |
| Staff work arrangements | `/api/StaffWorkArrangement` | `/api/EmployeeWorkArrangement` |
| Staff work patterns | `/api/StaffWorkPattern` | `/api/EmployeeWorkPattern` |
| Staff work-mode overrides | `/api/StaffWorkModeOverride` | `/api/EmployeeWorkModeOverride` |
| Staff reporting mapping | `/api/StaffReportingMapping` | `/api/EmployeeManagerMapping` |

Authentication and permission payloads remain unchanged: consumers resolve current `ModuleId` and
`OperationId` from the authenticated menu/permission response and send those IDs through the
existing request DTO. No numeric permission ID is hard-coded.

This phase changes controller organization, routes, module codes and UI callers. Existing entity,
DTO and database column names such as `TenantId` and `EmployeeId` remain compatibility contracts
until their coordinated physical-schema phase.
