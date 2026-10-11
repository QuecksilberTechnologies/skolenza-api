# Employee Work Location and Work Arrangement validation

## Purpose

Employee Work Location records define where an employee is allowed to mark attendance. Employee Work Arrangement records define the employee's effective work mode, Published Attendance policy version, and authoritative primary location. Create, update, status, and delete flows now protect the relationship between these records.

## Typed Attendance policy configuration

Generic Attendance policy create/update requests now include `attendanceConfiguration`. The same object is returned in policy detail and is copied when a version is cloned. It is mandatory only when the selected Policy Type belongs to the stable `ATTENDANCE` category.

```json
{
  "attendanceConfiguration": {
    "attendanceLocationScope": 2,
    "allowBiometric": false,
    "allowMobile": true,
    "allowWeb": true,
    "allowManualAttendance": false,
    "allowWorkFromHome": true,
    "requireGeoFenceForOffice": true,
    "requireGpsForRemote": true,
    "allowOutsideLocationWithApproval": false
  }
}
```

Location scope values are `1 PrimaryLocationOnly`, `2 AssignedLocations`, `3 AnyTenantLocation`, and `4 RemoteAnywhere`. At least one attendance channel must be enabled. Physical arrangements cannot use `RemoteAnywhere`; Work From Home requires `allowWorkFromHome=true` and no physical primary location.

`EmployeeDeviceEnrollment` remains a biometric-device registration record. Mobile or web attendance does not require a biometric device allotment. The production self-service flow is documented in `ATTENDANCE_PUNCH_API.md`; a successful punch response confirms persistence in the immutable attendance ledger.

## Authentication and permission

- Bearer token is required.
- Existing module/operation permission pipeline remains unchanged.
- Resolve current `ModuleId` and `OperationId` from the authenticated menu/permission flow. Do not hard-code numeric IDs from examples.
- Tenant and employee access remain server-resolved and tenant-isolated.

## Existing routes affected

- `POST /api/EmployeeLocationAssignment/create`
- `POST /api/EmployeeLocationAssignment/update`
- `POST /api/EmployeeLocationAssignment/update-status`
- `DELETE /api/EmployeeLocationAssignment/delete/{id}`
- `POST /api/EmployeeWorkArrangement/create`
- `POST /api/EmployeeWorkArrangement/update`
- `POST /api/EmployeeWorkArrangement/update-status`

No new endpoint, polling, retry, cancellation, file, Excel, CSV, or FormData contract was introduced.

## Validation behavior

### Effective windows

Dates are inclusive. Two active records conflict when their windows share even one date. `effectiveTo: null` means open-ended. Non-overlapping historical and future records are allowed.

Example: `2026-01-01..2026-01-31` and `2026-01-31..2026-02-28` overlap. The second period must start on `2026-02-01` to avoid a conflict.

### Primary-location relationship

When a Work Arrangement supplies `primaryTenantLocationId`, the employee must already have an Employee Location Assignment for exactly that location which:

- is active and not soft deleted;
- is marked `isPrimary: true`;
- has `isAttendanceAllowed: true`;
- starts on or before the arrangement;
- covers the arrangement's complete end date, including open-ended arrangements.

The API blocks shortening, moving, deactivating, or deleting that assignment while a live arrangement depends on it. Update the arrangement first.

### Work mode and location type

`TenantLocation.locationType` uses this backend numeric contract. UI enums and dropdown values must
match it exactly; do not infer the numbers from display order.

| ID | Location type |
| --- | --- |
| 1 | Head Office |
| 2 | Branch |
| 3 | Office |
| 4 | Plant |
| 5 | Warehouse |
| 6 | Client Site |
| 7 | Project Site |
| 8 | Campus |
| 9 | Remote Office |

| Work mode | Primary-location rule |
| --- | --- |
| Office | Required; Head Office, Branch, Office, Plant, Warehouse, Campus, or Remote Office |
| Hybrid | Required; same physical office types as Office |
| Client Site | Required; Client Site only |
| Field | Optional; when supplied, Client Site or Project Site |
| Work From Home | Physical primary location is not allowed |

The location dropdown should show both name and type, for example `PANIPAT-REFIN (Client Site)`.
The Work Arrangement UI filters this dropdown using the selected Work Mode and blocks submission
when a stale selected location no longer matches that mode.

## UI submit order

1. Create or update the employee's primary Location Assignment first.
2. Ensure its effective range covers the intended arrangement.
3. Create or update the Work Arrangement with the same location ID.
4. When changing primary location, change the Work Arrangement away from the old location before deactivating/deleting the old assignment.

Illustrative location assignment:

```json
{
  "moduleId": 0,
  "operationId": 0,
  "employeeId": "<encoded-employee-id>",
  "tenantLocationId": 21,
  "isPrimary": true,
  "isAttendanceAllowed": true,
  "effectiveFrom": "2026-10-01",
  "effectiveTo": null,
  "isActive": true
}
```

Illustrative Client Site arrangement:

```json
{
  "moduleId": 0,
  "operationId": 0,
  "employeeId": "<encoded-employee-id>",
  "policyVersionId": 318,
  "primaryTenantLocationId": 21,
  "workMode": 5,
  "hybridType": null,
  "minimumOfficeDaysPerWeek": null,
  "minimumOfficeDaysPerMonth": null,
  "maximumWFHDaysPerMonth": null,
  "effectiveFrom": "2026-10-01",
  "effectiveTo": null,
  "isActive": true
}
```

## Representative validation errors

The API now returns the first actionable reason instead of collapsing all reference, status, flag,
and date failures into one generic message. UI should display the server `message` unchanged.

| Condition | API message / correction |
| --- | --- |
| Employee is missing or soft deleted | Employee does not exist for this tenant or has been deleted. Select an existing employee. |
| Employee is inactive | Activate the employee before creating or activating the arrangement. |
| Location is missing or soft deleted | Primary location does not exist for this tenant or has been deleted. Select an existing location. |
| Location is inactive | Activate the tenant location before using it. |
| Location is not assigned to the employee | Add an Employee Location Assignment first. |
| Assignment is inactive | Activate the Employee Location Assignment. |
| Assignment is not primary | Mark that employee-location assignment as Primary. |
| Attendance is disabled | Enable `Attendance allowed` on that assignment. |
| Assignment starts after the arrangement | The message includes both dates and tells the admin which start date to move. |
| Assignment ends before the arrangement | The message includes both dates and tells the admin which end date to extend or shorten. |
| Arrangement is open ended but assignment has an end date | Remove the assignment end date or give the arrangement a compatible end date. |
| Location type conflicts with work mode | The message includes the actual location type and selected work mode. |
| Active arrangement dates overlap another active arrangement for the employee | The message includes both requested and existing date ranges. End dates are inclusive; the next arrangement must start on the following day. |

`IsSoftDeleted = true` employee, tenant-location, and employee-location-assignment rows are excluded
from validation queries. They never satisfy an arrangement dependency and are reported as missing or
deleted; their `IsActive` value does not make them eligible.

```json
{
  "isSucceeded": false,
  "message": "The employee location assignment starts on 04/02/2027, but the work arrangement starts on 26/09/2026. Move the location assignment start date to 26/09/2026 or earlier, or start the work arrangement on 04/02/2027 or later.",
  "data": null,
  "errors": [],
  "errorCode": "VALIDATION_ERROR"
}
```

```json
{
  "isSucceeded": false,
  "message": "The selected primary location has type 'HeadOffice', which is not valid for work mode 'ClientSite'. Select a compatible location or change the work mode.",
  "data": null,
  "errors": [],
  "errorCode": "VALIDATION_ERROR"
}
```

```json
{
  "isSucceeded": false,
  "message": "This location assignment is the primary location of an active work arrangement. Update the work arrangement first.",
  "data": null,
  "errors": [],
  "errorCode": "CONFLICT"
}
```

## Persistence and deployment

Tables are `EmployeeLocationAssignment`, `EmployeeWorkArrangement`, `TenantLocation`, `PolicyVersion`, and the one-to-one `AttendancePolicyVersionConfiguration`. Apply `database-scripts/AlignEmployeeWorkLocationAndArrangementValidation.sql` and `database-scripts/AddAttendancePolicyVersionConfiguration.sql`. The second script backfills existing Attendance versions once with compatibility settings and does not overwrite an existing typed configuration.

## Attendance-policy scope status

The selected generic `PolicyVersion` currently has no typed `AttendanceLocationScope`. The older separate `AttendancePolicy` table contains that field, while generic Policy rules only have free-form JSON such as `locationValidationRequired`. No JSON key was invented in this change. A stable generic-policy scope contract must be selected before this final cross-check can be implemented safely.

## Verification status — 2026-09-24

- Solution build: PASS with existing warnings, 0 errors.
- Focused automated tests: PASS, 27 passed, 0 failed, 0 skipped.
- Migration execution against a database: not run.
- Authenticated API and deployed verification: not run.

## Clear validation diagnostics — 2026-09-26

- Create, update, and activation of Work Arrangements use the same detailed validator.
- Build: PASS with existing warnings and 0 errors.
- Focused automated tests: PASS, 29 passed, 0 failed, 0 skipped.
- Full suite: 541 passed, 3 host-down failures, 95 skipped; after starting the local API, the
  exact three failed HTTP smoke tests passed 3/3.
- Authenticated HTTP and deployed verification: not run; no live API response is claimed.

## Employee Location Assignment update lifecycle correction — 2026-09-26

The Employee Location Assignment read contract now returns `employeeId` as the same tenant-salted
encoded string accepted by create and update. Previously, create worked because the employee dropdown
supplied the encoded identifier, while list/get-by-id returned the database `long`; edit copied that
number into the update body and ASP.NET rejected it before the handler with a JSON-to-string error.

The following UI/API flows now use consistent contracts:

- create and update send the encoded `employeeId` string unchanged;
- get-all and get-by-id return the encoded `employeeId` string;
- deactivate sends only assignment `id` and `isActive: false` to `update-status`;
- delete sends the assignment ID in `DELETE /delete/{id}`;
- update, deactivate, and delete reject a change that would invalidate an active, non-soft-deleted
  Work Arrangement using the assignment as its primary location;
- soft-deleted Work Arrangements are excluded from dependency checks.

Verification: backend focused tests 30/30 passed, Angular focused tests 15/15 passed, Angular
production build passed, and the backend full suite passed 545 with 0 failures and 95 explicit skips.
Local unauthenticated smoke returned `401` for create, update, update-status, and delete, confirming
all routes are active and protected. Authenticated mutation and deployed verification were not run.

## Work Arrangement date-overlap contract — 2026-09-26

An employee may keep multiple Work Arrangement rows as dated history, but no two active,
non-soft-deleted rows may cover the same calendar date. A missing `effectiveTo` means the arrangement
continues indefinitely. Because both endpoints are inclusive, an arrangement ending on 31/01/2026
allows the next arrangement to start on 01/02/2026, not 31/01/2026.

Create, update, and inactive-to-active status changes use one shared application validator. Update
excludes the row being edited. The repository returns the conflicting row so the API can return an
actionable message such as:

```json
{
  "isSucceeded": false,
  "message": "The requested work arrangement from 31/01/2026 to No end date overlaps an existing active work arrangement from 01/01/2026 to 31/01/2026. Change one period so they do not share any calendar date. Start and end dates are inclusive.",
  "errorCode": "CONFLICT"
}
```

Inactive rows do not block a new active schedule. Rows with `IsSoftDeleted = true` are excluded even
if an inconsistent legacy row still has `IsActive = true`. The database exclusion constraint remains
the final concurrency guard; the shared application validation supplies the readable error before save.

## Location-type contract correction — 2026-09-26

- Confirmed the Angular enum omitted `Office = 3`, shifting Plant through Remote Office down by one.
  Consequently, the UI serialized Client Site as `5`, while the API correctly interprets `5` as
  Warehouse and requires `6` for Client Site.
- Angular enum/options now match all nine backend values. The Work Arrangement dropdown filters by
  the API's work-mode compatibility matrix and Client Site requires a primary location client-side.
- Focused Angular tests: 38 passed, 0 failed, 0 skipped. Production build passed with existing bundle
  budget warnings.
- Development database reconciliation corrected the two explicitly identified tenant-10 client-site
  rows to `LocationType = 6`; Sumit Verma's Railway-Client assignment was also corrected to primary.
- Authenticated HTTP create from the running browser was not captured. Direct database reconciliation
  confirmed the published policy, typed configuration, location type, primary assignment, effective
  coverage and absence of an existing active arrangement for the reported 2028-02-04 case.

## Encoded employee identifier contract — 2026-09-27

Every Employee Work Arrangement response returns `employeeId` as the authenticated tenant's
salt-encoded string. This applies to create, update, status change, get-by-id, and paged-list responses.
The UI must preserve that string unchanged when it prepares an edit request:

```json
{
  "id": 123,
  "employeeId": "<tenant-salted-employee-id>",
  "policyVersionId": 456,
  "effectiveFrom": "2026-09-27",
  "workMode": 3,
  "isActive": true,
  "moduleId": "<resolved-from-menu>",
  "operationId": "<resolved-from-menu>"
}
```

The database `bigint` employee key is internal and is never part of this public response contract.
The API decodes the submitted string through the existing tenant-aware identifier pipeline and rejects
an identifier that belongs to another tenant or is otherwise invalid.

Scenario evidence: [encoded employee ID update — 2026-09-27](../testing/employee-work-arrangement/update-encoded-employee-id/2026-09-27.md).
