# Attendance Punch API

## Purpose

This API records self-service Mobile or Web attendance for the employee represented by the authenticated Tenant bearer token. Biometric device enrollment is not required for these two channels. The client cannot submit another employee ID.

## Endpoints

### `POST /api/Attendance/mark-attendance`

Authentication: Tenant bearer token. This is an employee self-service action, so `ModuleId`, `OperationId`, and `EmployeeId` are not accepted.

```json
{
  "action": 1,
  "attendanceDeviceTypeId": 1,
  "tenantLocationId": 1,
  "latitude": 23.1815,
  "longitude": 79.9864,
  "accuracyMeters": 18.5,
  "clientOccurredAt": "2026-09-24T10:30:00+05:30",
  "idempotencyKey": "661a418b-4294-4f87-bd82-d3f08712ba27"
}
```

`action`: `1 = CheckIn`, `2 = CheckOut`. Load `attendanceDeviceTypeId` from `GET /api/Attendance/device-types`; never hard-code its numeric value. This employee endpoint accepts the stable `MOBILE` and `WEB` types and rejects Biometric and Manual types. Generate one UUID per button action and reuse the same UUID only while retrying that action.

The authoritative time is the server UTC time. `clientOccurredAt` is audit information. For physical work modes, omit `tenantLocationId` to use the arrangement primary location, or send an allowed location. For Work From Home, omit the location. Send latitude and longitude together when policy requires GPS. The API checks the active employee, effective arrangement, typed published policy configuration, allowed channel, location assignment/scope, attendance-enabled location, geofence, local work date and check-in/check-out order.

Representative success:

```json
{
  "isSucceeded": true,
  "message": "Attendance punch recorded successfully.",
  "data": {
    "id": 101,
    "action": 1,
    "attendanceDeviceTypeId": 1,
    "attendanceDeviceTypeCode": "MOBILE",
    "attendanceDeviceType": "Mobile",
    "workDate": "2026-09-24",
    "occurredAtUtc": "2026-09-24T05:00:03Z",
    "tenantLocationId": 1,
    "distanceFromLocationMeters": 12.34,
    "isCurrentlyCheckedIn": true
  }
}
```

Expected validation/conflict messages include inactive employee, no effective arrangement, disabled channel, missing GPS, invalid/disabled location, outside geofence, already checked in, and check-in required before check-out.

### `GET /api/Attendance/today`

Authentication: Tenant bearer token. It returns the authenticated employee's local business date, ordered immutable punch timeline, and `isCurrentlyCheckedIn`. Call on page load and after each successful punch.

### `GET /api/Attendance/device-types`

Authentication: Tenant bearer token. Returns active seeded attendance sources with `id`, stable `code`, display `name`, and `requiresDeviceRegistration`. Load this lookup before submitting a punch. `MOBILE`, `WEB`, `BIOMETRIC`, and `MANUAL` are server-owned codes; only Mobile/Web are accepted by the self-service mark endpoint.

## Persistence and concurrency

Apply `database-scripts/AddEmployeeAttendancePunch.sql`. `EmployeeAttendancePunch` is an immutable ledger. A unique Tenant/employee/idempotency-key index makes network retries safe. A PostgreSQL transaction advisory lock serializes simultaneous punches for one employee. Work date uses the selected location timezone, or the active head-office timezone when no physical location applies.

## UI flow

1. Call `GET /today`.
2. If `isCurrentlyCheckedIn` is false, show Check in; otherwise show Check out.
3. Obtain browser/mobile GPS only when required by the selected policy and work mode.
4. Submit explicit `action`, the selected `attendanceDeviceTypeId`, and a new idempotency UUID.
5. Disable repeated clicks until the call finishes. A retry caused by a timeout must reuse that UUID.

## Tested status

On 2026-09-24 the solution built with zero errors, the focused attendance/work-arrangement suite passed 21 of 21 tests, and the full suite passed 541 tests with zero failures and 95 environment-gated skips. The configured Development database migrations and rollback-safe real-row smoke passed. Authenticated employee HTTP success-path verification still requires a Tenant employee token; see the matching scenario report for the exact boundary.
