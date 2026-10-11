# Employee Module API and DTO Contracts

## Operational tenant defaults (2026-09-12, local implementation)

POST `/api/Employee/section-defaults`, authenticated tenant Admin with existing
EMP_LIST/Update permission identifiers. IDs below are placeholders, not production grants.

Input `UpdateEmployeeSectionDefaultsRequestDTO`:

```json
{"moduleId": 1, "operationId": 2, "isEditAllowed": false}
```

Response `ApiResponse<List<EmployeeSectionDefaultResponseDTO>>`:

```json
{
  "isSucceeded": true,
  "message": "Employee operational section defaults updated.",
  "data": [
    {"moduleCode": "EMP_WORK_LOCATIONS", "isEditAllowed": false},
    {"moduleCode": "EMP_DEVICES", "isEditAllowed": false},
    {"moduleCode": "EMP_WORK_ARRANGEMENT", "isEditAllowed": false},
    {"moduleCode": "EMP_WORK_PATTERN", "isEditAllowed": false},
    {"moduleCode": "EMP_OVERRIDES", "isEditAllowed": false}
  ],
  "errors": []
}
```

Tenant and audit employee come from authenticated context. All five writes are atomic.
Missing tenant settings default to false. Migration: AddTenantEmployeeSectionDefaults.sql.
Employee-role commands consult defaults in the existing Employee permission pipeline.
HTTP production acceptance is still pending; this is not a deployment claim.

Identity GET now accepts optional `employeeId` (encoded); absent means current employee.
Legacy `countryNationalityId` is retained but persisted Employee.CountryId controls lookup.

## Implemented profile completion/status contract

`GET /api/Employee/get-all-percentage` returns its section list in `data` and the overall rounded mean in `completionPercentage`. `data` contains all 13 non-transactional Employee profile/configuration tabs; empty tabs are 0 percent with `isSectionCreate=false`.

The Employee list reuses this same overall section mean instead of reporting
Overview-only completion. Bank is incomplete until a Primary row exists and
then averages all saved rows. Identity applicability comes from the employee's
active country rules: mandatory catalogue rows always participate, optional
rows participate after creation, and each participating row requires both the
identity value and uploaded document. These rules are backend-owned; no field or
country list is hardcoded in the UI.

```json
{
  "isSucceeded": true,
  "message": "Employee profile completion retrieved.",
  "data": [{ "sectionName": "Overview", "completionPercent": 86, "isInfoVerified": false, "isEditAllowed": true, "isSectionCreate": true }],
  "completionPercentage": 54,
  "errors": []
}
```

`POST /api/Employee/update-bulk` input:

```json
{
  "employeeId": "encoded-employee-id",
  "moduleId": 1,
  "operationId": 18,
  "isActive": true,
  "sections": [{ "tabInfoType": 6, "isVerified": true, "isEditAllowed": false }]
}
```

Response: `{ "isSucceeded": true, "message": "Section status updated successfully.", "data": true, "errors": [] }`. When verified is true, persisted effective editability is always false.

Status: **LIVING IMPLEMENTATION DOCUMENT — WIP**  
Updated: 2026-09-12

This document records the input and response DTO contracts observed while each Employee feature is
implemented and tested. It must be updated with test evidence before a feature is marked COMPLETE.
All endpoints use the existing `ApiResponse<T>`/paged envelope unless a row says otherwise. Common
permission/paging properties inherited from base request DTOs remain part of the wire contract even
when not repeated in a derived DTO source file.

## Common envelopes

`ApiResponse<T>`: `isSucceeded`, `message`, `data`, `errors`, plus optional `errorCode`, paging,
`isPrimaryMarked`, `hasAllDocUploaded`, and `completionPercentage` metadata.

`CompletionSectionDTO`: `sectionName:string?`, `completionPercent:double?`,
`isInfoVerified:bool?`, `isEditAllowed:bool?`, `isSectionCreate:bool`.

`PermissionRequestDTO`: existing module/operation permission identifiers. Tenant and actor must be
derived from authenticated context; client-supplied ownership/audit IDs are not authoritative.

## Overview / Employee

| Method and route | Input DTO | Response DTO / purpose |
| --- | --- | --- |
| `POST /api/Employee/create` | `CreateBaseEmployeeRequestDTO` | Created Employee response through `ApiResponse` |
| `GET /api/Employee/get` | `GetBaseEmployeeRequestDTO` | `GetBaseEmployeeInfoResponseDTO` |
| `GET /api/Employee/get-all` | `GetAllEmployeeInfoRequestDTO` | Paged `GetAllEmployeeInfoResponseDTO` |
| `POST /api/Employee/update` | `UpdateEmployeeRequestDTO` | Updated Employee response |
| `POST /api/Employee/official/update` | `UpdateEmployeeRequestOfficialDTO` | Updated Employee response |
| `DELETE /api/Employee/delete-all` | `DeleteBaseEmployeeRequestDTO` | Delete/soft-delete result |
| `PUT /api/Employee/update-status` | `ActivateAllEmployeeRequestDTO` | Employee active-status result |
| `GET /api/Employee/get-summary` | `GetEmployeeSummaryRequestDTO` | Employee summary |
| `GET /api/Employee/get-profile-summary` | `GetEmployeeSummaryRequestDTO` | Employee profile summary |
| `GET /api/Employee/Image/get` | `GetEmployeeImageRequestDTO` | `GetEmployeeImageReponseDTO` |
| `POST /api/Employee/profile/pic/update` | `UpdateEmployeeImageRequestDTO` | Updated profile-image result |
| `GET /api/Employee/get-all-percentage` | query `employeeId` + `PermissionRequestDTO` | `ApiResponse<List<CompletionSectionDTO>>`; list is currently in `data` |
| `POST /api/Employee/update-edit-status` | `UpdateEditStatusRequestDTO_` | `ApiResponse<bool>` |
| `POST /api/Employee/update-verification-status` | `UpdateVerificationStatusRequestDTO_` | `ApiResponse<bool>` |
| `POST /api/Employee/update-bulk` | `UpdateEmployeeSectionStatusRequestDTO` | `ApiResponse<bool>` |
| `POST /api/Employee/reset-password` | `ResetEmployeePasswordRequestDTO` | `EMP_LIST` + `Reset Password`; reset result; password values must never be logged |

Principal request fields:

- `CreateBaseEmployeeRequestDTO`: `employeeDocumentId?`, `firstName`, `middleName?`, `lastName`,
  `dateOfBirth?`, `dateOfOnBoarding?`, `designationId`, `countryId`, `departmentId`, `employeeTypeId`,
  `genderId`, `roleId`, `referalId?`, `hasPermanent`, `isActive`, `officialEmail`.
- `GetBaseEmployeeRequestDTO`: employee/name/date/designation/type/status/edit/verification filters.
- `GetAllEmployeeInfoRequestDTO`: employee/code/name/date/marital/type/gender/designation/department/
  email filters plus inherited permission and paging fields.
- `UpdateEmployeeRequestDTO`: `employeeId`, names, DOB, marital/contact/blood/gender/relation,
  remark and description.
- `UpdateEmployeeRequestOfficialDTO`: `employeeId`, personal fields plus country, department,
  designation, employee type, onboarding/exit, permanent/edit/verify/active flags.
- `GetBaseEmployeeInfoResponseDTO`: encoded ID, employment code, names, gender, designation,
  department, emergency contact, blood/mobile/relation, country/nationality, role/type, dates,
  official email, permanent/active/edit/verified flags and `completionPercentage`.
- `GetAllEmployeeInfoResponseDTO`: encoded employee ID, identity/display fields, department,
  designation, type, status/image and per-row `completionPercentage`.
- `UpdateEmployeeSectionStatusRequestDTO`: encoded `employeeId`, optional `isActive`, and `sections`;
  each section carries `tabInfoType`, `isVerified`, `isEditAllowed`.

## Bank

| Operation | Input DTO | Response DTO |
| --- | --- | --- |
| Create | `CreateBankRequestDTO` (multipart) | `GetBankResponseDTO` |
| Read | `GetBankReqestDTO` | list/result of `GetBankResponseDTO` |
| Update | `UpdateBankReqestDTO` (multipart) | `GetBankResponseDTO` |
| Delete | `DeleteBankRequestDTO` | delete result |

Create/update data: encoded employee ID; record ID on update; bank name, account number, IFSC,
branch, account type, UPI, primary flag and optional cancelled-cheque file. Response adds encoded IDs,
file metadata, uploaded flag, active/edit/verified flags and completion percentage.

## Contact

| Operation | Input DTO | Response DTO |
| --- | --- | --- |
| Create | `CreateContactRequestDTO` | `GetContactResponseDTO` |
| Read | `GetContactRequestDTO` | list/result of `GetContactResponseDTO` |
| Update | `UpdateContactRequestDTO` | `GetContactResponseDTO` |
| Delete | shared `DeleteRequestDTO` | delete result |

Fields: employee/record IDs, contact name/type/number, relation, alternate number, email, primary flag,
country/state/district, house/landmark/street/address, remark and description. Response adds location
names, active/edit/verified/audit state and completion percentage.

## Experience

| Operation | Input DTO | Response DTO |
| --- | --- | --- |
| Create | `CreateExperienceRequestDTO` (multipart) | `GetEmployeeExperienceResponseDTO` |
| Read | `GetExperienceRequestDTO` | list/result of `GetEmployeeExperienceResponseDTO` |
| Update | `UpdateExperienceRequestDTO` (multipart) | `GetEmployeeExperienceResponseDTO` |
| Delete row | shared `DeleteRequestDTO` | delete result |
| Delete document | shared `DeleteRequestDTO` | delete result |

Fields include employee/record IDs, CTC, company, designation, prior company employee ID, dates,
experience duration, WFH/foreign/gap flags, working country/state/district, leaving/gap reasons,
colleague and reporting-manager contacts, verification email, and document rows (`documentType`,
file/name/path/remark). Response adds completion, experience verification modes, info verification,
editability and document metadata.

## Education

| Operation | Input DTO | Response DTO |
| --- | --- | --- |
| Create | `CreateEducationRequestDTO` (multipart) | `GetEducationResponseDTO` |
| Read | `GetEducationRequestDTO` | list/result of `GetEducationResponseDTO` |
| Update | `UpdateEducationRequestDTO` | `GetEducationResponseDTO` |
| Delete | `DeleteEducationRequestDTO` | delete result |

Fields: employee/record ID, degree, institute, dates, score value/type, grade/division, education-gap
state/years/reason, remark and document. Response adds file metadata, edit/verified/active flags and
completion percentage.

## Dependent

| Operation | Input DTO | Response DTO |
| --- | --- | --- |
| Create | `CreateDependentRequestDTO` (multipart) | `GetDependentResponseDTO` |
| Read | `GetDependentRequestDTO` | list/result of `GetDependentResponseDTO` |
| Read detail/counts | `GetDependentRequestDTO` | `GetDependentsDetailResponseDTO` |
| Update | `UpdateDependentRequestDTO` (multipart) | `GetDependentResponseDTO` |
| Delete | shared `DeleteRequestDTO` | delete result |

Fields: employee/record ID, dependent name, relation, DOB, covered-in-policy and married flags,
remark/description, proof file/upload state. Detail response includes total dependents, children,
spouses, parents, in-laws and dependent rows. No country-specific spouse limit is established by DTO.

## Insurance

| Operation | Input DTO | Response DTO |
| --- | --- | --- |
| Enrol | `CreateEmployeeEnrolledRequestDTO` | enrolled policy response |
| List | `GetEnrolledEmployeeRequestDTO` | `GetEmployeeEnrolledResponseDTO` rows |
| Delete | `DeleteEnrolledEmployeePolicyRequestDTO` body | delete result |

Fields: employee ID, policy type, insurance policy, dependent flag, start/end dates and dependent
coverage rows (`dependentId`, relation, covered). No active Employee Insurance update action exists.

## Identity (current Sensitive contract)

| Operation | Input DTO | Response DTO |
| --- | --- | --- |
| Create | `CreateEmployeeIdentityRequestDTO` (multipart) | identity creation result |
| Country-driven read | `GetIdentityRequestDTO` | `CountryIdentityDocumentResponseDTO` / current identity response path |
| Update | Angular declares `CreateEmployeeSensitiveRes` request path | Backend endpoint currently inactive; contract unresolved |

Create contains a list of identity rows with employee ID, identity category document ID, value,
document code/file, effective dates, file metadata, verification/audit/edit/upload/active state.
Country option response contains country code/name, identity category name, identity document ID,
document name and mandatory flag. The legacy `GetIdentityResponseDTO` still exposes hard-coded-style
fields such as Aadhaar/PAN/passport; this overlaps the normalized identity tables and must be resolved.

## Work Locations

All responses use `EmployeeLocationAssignmentResponseDTO` inside the common envelope.

| Operation | Input DTO |
| --- | --- |
| Create | `CreateEmployeeLocationAssignmentRequestDTO`: `employeeId`, `tenantLocationId`, `isPrimary`, `isAttendanceAllowed`, `effectiveFrom`, `effectiveTo?`, `isActive` |
| Get by ID/delete | route ID + `PermissionRequestDTO` |
| List | `EmployeeLocationAssignmentFilterRequestDTO`: employee/resolved employee, location, primary/status, paging + permission |
| Update | `UpdateEmployeeLocationAssignmentRequestDTO`: create fields + `id` |
| Status | `UpdateEmployeeLocationAssignmentStatusRequestDTO`: `id`, `isActive` + permission |

Response: IDs, employee name/code, location name/code, primary/attendance flags, effective dates and
active state.

## Devices

All safe read responses use `EmployeeDeviceEnrollmentResponseDTO`; PIN, raw card number, face image
and physical device-enrol ID are intentionally absent.

| Operation | Input DTO |
| --- | --- |
| Create | `CreateEmployeeDeviceEnrollmentRequestDTO`: employee/device/card IDs, access effective dates, access windows, active |
| Get by ID/delete | encoded route ID + `PermissionRequestDTO` |
| List | `EmployeeDeviceEnrollmentFilterRequestDTO`: employee/device/location/status/search/paging + permission |
| Update | `UpdateEmployeeDeviceEnrollmentRequestDTO`: enrollment ID, access dates/windows |
| Status | `UpdateEmployeeDeviceEnrollmentStatusRequestDTO`: enrollment ID + active |
| Face | `UpsertEmployeeDeviceFaceRequestDTO`: enrollment ID + JPEG/PNG face file |
| PIN | `UpsertEmployeeDevicePinRequestDTO`: enrollment ID + write-only PIN |
| Card | `BindEmployeeDeviceCardRequestDTO`: enrollment ID + tenant card ID |
| Credential removal | `RemoveEmployeeDeviceCredentialRequestDTO`: enrollment ID + credential type |

Response: encoded IDs, employee/device/location/card display data, masked card number, enrolled flags,
deployment/command statuses, access dates/windows, last sync and active state.

## Work Arrangement

| Operation | Input DTO | Response DTO |
| --- | --- | --- |
| Create | `CreateEmployeeWorkArrangementRequestDTO` | `EmployeeWorkArrangementResponseDTO` |
| Get/delete | route ID + `PermissionRequestDTO` | same response/delete result |
| List | `EmployeeWorkArrangementFilterRequestDTO` | paged response rows |
| Update | create fields + `id` | same response |
| Status | `id`, `isActive` + permission | mutation result |

Business fields: employee, attendance policy, primary location, work mode, optional hybrid type,
minimum office days/week/month, maximum WFH days/month, effective dates and active state.

## Work Pattern

| Operation | Input DTO | Response DTO |
| --- | --- | --- |
| Create | arrangement ID, day of week, work mode, optional location, working-day and active flags | `EmployeeWorkPatternResponseDTO` |
| Get/delete | route ID + `PermissionRequestDTO` | same response/delete result |
| List | arrangement/day/status/paging + permission | paged rows |
| Update | create fields + `id` | same response |
| Status | `id`, `isActive` + permission | mutation result |

## Overrides

| Operation | Input DTO | Response DTO |
| --- | --- | --- |
| Create | employee, optional arrangement, requested mode, from/to dates, optional location, reason, active | `EmployeeWorkModeOverrideResponseDTO` |
| Get/delete | route ID + `PermissionRequestDTO` | same response/delete result |
| List | employee/mode/approval/date/status/search/paging + permission | paged rows |
| Update | create fields + `id` | same response |
| Status | `id`, `isActive` + permission | mutation result |

Response additionally owns approval status/name, approval remark and rejection remark; callers do not
set approval fields through create/update DTOs.

## Employee bulk

Employee bulk retains the common durable bulk DTOs documented in
`AI_ASSISTED_BULK_IMPORT_REFERENCE.md`:

- preview: `BulkImportPreviewRequestDTO` -> `BulkImportPreviewResponseDTO`;
- confirm/retry/cancel/invitations/job list/detail: `BulkImportJobRequestDTO` -> job/result DTOs;
- template/report endpoints return CSV files rather than JSON DTO envelopes.

Reporting manager, work-location/arrangement, policy and device enrollment bulk assignments remain
separate PENDING features; base Employee import must not acquire those side effects.

## Test evidence

| Feature | Evidence | Status |
| --- | --- | --- |
| Legacy route, percentage and permission-pipeline characterization | `EmployeeProfileCharacterizationTests`; 14/14 passed on 2026-09-12 | COMPLETE for Phase 0/1 static/unit scope |
| Recent Work/Device route + server-owned module contracts | Existing `RecentDeviceEmployeeApiContractTests`; included in 181-test related regression run | PASS |
| Authenticated CRUD/permission/DB tests for every legacy tab | Not yet present for the full matrix | PENDING |
| Live `data` versus Angular `sections` contract | Not yet captured | PENDING |
| Identity update | Backend route inactive | BLOCKED BY CONTRACT DECISION |

Additional validation on 2026-09-12:

- related Work/Device and EmployeeBulk unit regressions: 181/181 passed, zero skips;
- Employee import/type/code isolated PostgreSQL tests: 17/17 passed, zero skips;
- Playwright login-route test after installing Chromium: 1/1 passed;
- live unauthenticated GET probes: all protected Employee families rejected access. Endpoints with
  required query DTO fields first returned model-validation 400 when fields were omitted; with valid
  DTO shape they returned 401 `Token missing` before data access;
- full suite with isolated DB: 424 passed, 10 failed, 7 skipped. The 10 failures are HostBulk fixture
  migration drift (`BulkImportJob_Master_check` rejects the newer Host master values), not Employee
  profile test failures. They are recorded as failures, not passes.
