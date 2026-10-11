# Holiday

## Purpose

`Holiday` now assigns each holiday to one physical tenant
location. Country and state are resolved through `TenantLocation`; they are not
copied into the holiday row. The year is derived from `HolidayDate`.

## Current API and permission flow

The menu module is `TENANT_POLICY_HOLIDAY` under `TENANT_POLICIES`,
with route `/app/holidays`. The `View`, `Add`, `Update`, `Delete`, `Import`, and `Export` operations
are mapped through the existing tenant permission pipeline. The UI must resolve
current `moduleId` and `operationId` through the authenticated menu/permission
flow. Observed numeric IDs are not portable and must not be hardcoded.

All routes below require an authenticated tenant employee. The API checks that
the supplied module is the holiday module, the operation matches the action,
and the tenant employee has the runtime permission. TenantId comes from the
validated authentication context. A caller cannot choose another tenant.

| Action | Route | Operation | Inputs | Persistence |
| --- | --- | --- | --- | --- |
| List | `GET /api/Holiday/get` | View | `moduleId`, `operationId`; optional `tenantLocationId`, `holidayYear` | Read active, nondeleted holidays of authenticated tenant |
| Detail | `GET /api/Holiday/{id}` | View | `moduleId`, `operationId` query | Read one nondeleted holiday, including inactive, of authenticated tenant |
| Create | `POST /api/Holiday` | Add | JSON body below | Insert `Holiday` |
| Update | `PUT /api/Holiday/{id}` | Update | JSON body below; path Id wins | Update same tenant's active row |
| Delete | `DELETE /api/Holiday/{id}` | Delete | `moduleId`, `operationId` query | Soft-delete row; retain history/audit |
| Import | `POST /api/Holiday/import` | Import | multipart form with permission IDs and CSV/XLSX `file` or `pastedText` | Reject existing same-date rows (active or inactive); insert only when the whole file is valid |
| Export | `GET /api/Holiday/export` | Export | permission IDs; optional `tenantLocationId`, `holidayYear` | Download UTF-8 CSV of active tenant holidays |
| Calendar display constants | `GET /api/Holiday/calendar-display-constants` | Token only | No query/body/permission IDs | Read stable status/color/priority constants; no persistence |

### Employee calendar display constants

The UI must call this once when opening the employee calendar and cache the result for the current application session:

```http
GET /api/Holiday/calendar-display-constants
Authorization: Bearer <token>
```

This endpoint validates the bearer token and trusted Tenant context. It does not accept `moduleId` or `operationId`, because it returns shared, non-persistent display constants only.

Representative response:

```json
{
  "isSucceeded": true,
  "message": "Holiday calendar display constants retrieved successfully.",
  "data": {
    "styles": [
      { "statusCode": "MANDATORY_HOLIDAY", "label": "Mandatory holiday", "backgroundColor": "#DC2626", "textColor": "#FFFFFF", "priority": 900 },
      { "statusCode": "ON_LEAVE", "label": "On leave", "backgroundColor": "#16A34A", "textColor": "#FFFFFF", "priority": 800 },
      { "statusCode": "TAKEN", "label": "Taken", "backgroundColor": "#16A34A", "textColor": "#FFFFFF", "priority": 700 },
      { "statusCode": "APPROVED", "label": "Approved", "backgroundColor": "#2563EB", "textColor": "#FFFFFF", "priority": 600 },
      { "statusCode": "PENDING", "label": "Pending", "backgroundColor": "#EAB308", "textColor": "#111827", "priority": 500 },
      { "statusCode": "OPTIONAL_HOLIDAY", "label": "Optional holiday", "backgroundColor": "#F59E0B", "textColor": "#111827", "priority": 400 },
      { "statusCode": "REJECTED", "label": "Rejected", "backgroundColor": "#6B7280", "textColor": "#FFFFFF", "priority": 300 },
      { "statusCode": "CANCELLED", "label": "Cancelled", "backgroundColor": "#9CA3AF", "textColor": "#111827", "priority": 200 },
      { "statusCode": "WEEKLY_OFF", "label": "Weekly off", "backgroundColor": "#D1D5DB", "textColor": "#111827", "priority": 100 }
    ]
  },
  "errors": []
}
```

UI mapping:

- Holiday `isOptional=false` → `MANDATORY_HOLIDAY`.
- Holiday `isOptional=true` → `OPTIONAL_HOLIDAY`.
- Pending leave request → `PENDING`; approved request → `APPROVED`.
- Employee currently on leave → `ON_LEAVE`; completed leave date → `TAKEN`.
- Rejected or cancelled request → `REJECTED` or `CANCELLED`.
- Configured weekly rest day → `WEEKLY_OFF`.
- If multiple event sources identify the same date, render the style with the highest numeric `priority`.

Colors are application constants, not Holiday-table columns. This API does not fetch employee leave history or leave requests; those APIs supply the date/status data and these constants supply presentation metadata.

Copyable list example (IDs illustrative):

```http
GET /api/Holiday/get?moduleId=118&operationId=4&tenantLocationId=5&holidayYear=2027
Authorization: Bearer <token>
```

Copyable create example (IDs illustrative):

```json
{
  "moduleId": 118,
  "operationId": 1,
  "tenantLocationId": 5,
  "holidayName": "Republic Day",
  "holidayDate": "2027-01-26",
  "isOptional": false,
  "description": "Mumbai office holiday",
  "icon": "bi bi-flag"
}
```

Update uses the same holiday fields with the Update operation. Example:

```http
PUT /api/Holiday/42
Content-Type: application/json
```

```json
{
  "moduleId": 118,
  "operationId": 2,
  "tenantLocationId": 5,
  "holidayName": "Republic Day",
  "holidayDate": "2027-01-26",
  "isOptional": false,
  "description": "India office holiday",
  "icon": "bi bi-flag"
}
```

Delete example (IDs illustrative):

```http
DELETE /api/Holiday/42?moduleId=118&operationId=3
Authorization: Bearer <token>
```

Import form example (IDs illustrative):

```text
POST /api/Holiday/import
Content-Type: multipart/form-data
moduleId=118
operationId=<current Import operation ID>
file=@holidays-2027.csv
```

Supply exactly one `file` or `pastedText`. CSV and XLSX files are accepted;
the shared bounded parser enforces its 5 MB and 5,000-row limits. Use these
column names exactly; `HolidayDate` is ISO `YYYY-MM-DD` and `IsOptional` is
`true` or `false`:

```csv
TenantLocationId,HolidayName,HolidayDate,IsOptional,Description,Icon
5,Republic Day,2027-01-26,false,India office holiday,bi bi-flag
5,Optional Festival Holiday,2027-03-25,true,Employee choice,bi bi-stars
```

The location must be active and belong to the authenticated tenant. A duplicate
location/date **within the file**, even with a different holiday name, fails
the whole upload. An existing non-soft-deleted holiday at the same location/date
also fails the whole upload whether `IsActive` is true or false. The error
names the existing row Id and status: edit that Id or soft-delete it first.
Any invalid row
fails the whole upload before saving any row. This is a synchronous bounded
import; there is no draft/confirm job, polling, retry or cancellation endpoint.
For a failed or uncertain upload, check GET/export and the conflict Id before retrying.

Illustrative invalid-row response: HTTP 400, with a row-numbered validation
message; the precise envelope is produced by the shared exception middleware.
No holiday rows are inserted when validation fails.

Representative import response (illustrative):

```json
{
  "isSucceeded": true,
  "message": "Holiday import completed successfully.",
  "data": {
    "totalRows": 2,
    "createdCount": 1,
    "skippedExistingCount": 0
  },
  "errors": []
}
```

Export example (IDs illustrative):

```http
GET /api/Holiday/export?moduleId=118&operationId=<current Export operation ID>&tenantLocationId=5&holidayYear=2027
Authorization: Bearer <token>
```

The response is `text/csv; charset=utf-8` with attachment filename
`holidays.csv`; even an empty result contains the same header.
Its columns match the import template. Reimporting unchanged rows returns a
duplicate validation error; it does not insert or overwrite them.
CSV text cells are quoted and spreadsheet-formula prefixes are escaped.

Representative successful create/detail response; actual IDs will differ:

```json
{
  "isSucceeded": true,
  "message": "Holiday created successfully.",
  "data": {
    "id": 42,
    "tenantId": 9,
    "tenantLocationId": 5,
    "holidayName": "Republic Day",
    "holidayDate": "2027-01-26",
    "isOptional": false,
    "isActive": true,
    "description": "Mumbai office holiday",
    "icon": "bi bi-flag"
  },
  "errors": []
}
```

List returns the same objects in a `data` array; delete returns `data: true`.
Expected failures use the standard API error envelope: 400 for missing/invalid
fields, inactive or foreign tenant location, or any non-soft-deleted holiday
on the same tenant/location/date; 401 for invalid authentication; 403 for wrong module/operation
or denied permission; 404 when a holiday is missing, deleted, or belongs to
another tenant. These examples describe the local code contract, not captured
deployed responses.

Representative validation error shape (illustrative, not a captured response):

```json
{
  "isSucceeded": false,
  "message": "TenantLocationId is required.",
  "data": null,
  "errors": ["TenantLocationId is required."],
  "errorCode": "VALIDATION_ERROR"
}
```

Illustrative duplicate response (HTTP 400; actual Id/status come from the DB):

```json
{
  "isSucceeded": false,
  "message": "A holiday entry already exists for this location and date (Id 42, inactive). Edit that entry or soft-delete it before creating another.",
  "data": null,
  "errors": ["A holiday entry already exists for this location and date (Id 42, inactive). Edit that entry or soft-delete it before creating another."],
  "errorCode": "VALIDATION_ERROR"
}
```

`holidayDate` is ISO `YYYY-MM-DD`, without time/timezone. `holidayName` is 1–100
characters; optional `description` is at most 255 and optional `icon` is at most
100 characters. Only one non-soft-deleted
holiday is allowed per tenant/location/date, regardless of name or `IsActive`.
The duplicate error includes the existing Id and active/inactive status. Use
GET by Id and PUT to edit an inactive entry, or DELETE by Id to soft-delete it;
the list endpoint still shows active rows only. PUT does not reactivate an
inactive entry. Deleting a holiday sets soft-delete/audit fields;
it does not remove approved employee leave or generate policy rules. Calendar
policy versioning is outside this endpoint; the CSV/XLSX import above is a
separate synchronous route.
No polling, retry or cancellation protocol is required for these synchronous
single-row endpoints. Failed writes should be corrected and resubmitted; do
not automatically retry a create after an uncertain network outcome without
first checking the list for an existing matching holiday.

## Persistence

Data is stored in `skolenza."Holiday"`. `TenantLocationId`
references `skolenza."TenantLocation"("Id")`. Country, state, district and city
come from that location relationship. The retained fields are:

- `TenantId` for tenant isolation
- `TenantLocationId` for the applicable office/location
- `HolidayName`, `HolidayDate`, `IsOptional`, `Description`, and nullable `Icon`
- active, soft-delete, and audit fields

`CountryCode`, `StateCode`, `HolidayYear`, and duplicate `Remark` are removed by
the guarded migration. Existing rows require an explicit location mapping; the
migration intentionally stops instead of guessing when unmapped rows exist.

## Validation status

- Calendar-display constants scenario: [2026-09-24 report](../testing/holiday-calendar/calendar-display-constants/2026-09-24.md).

- Rename/Icon scenario: [2026-09-23 report](../testing/holiday-calendar/rename-to-holiday/2026-09-23.md).
- Current local route is `/api/Holiday`; the previous `/api/HolidayCalandar` route is no longer exposed.
- Development DB rename preserved all 31 existing Tenant 8 / Jabalpur 2026 rows and added nullable `Icon varchar(100)`.

- Local solution build: PASS with 0 errors on 2026-09-23; existing repository warnings remain.
- Focused automated tests: 16 passed, 0 failed, 0 skipped on 2026-09-23,
  including schema/seed, CRUD contract, permission, rollback-only PostgreSQL
  CRUD and import/export coverage.
- Local HTTP authentication smoke: all five routes returned 401 without a token.
- Target DB rename migration: APPLIED on 2026-09-23 to the Development-configured
  database. Post-migration inspection confirmed `skolenza."Holiday"`, nullable
  `Icon varchar(100)`, renamed constraints/indexes/sequence, and all 31 existing
  Tenant 8 / Jabalpur 2026 rows. A data-only backup was saved under the repository's
  local `DBFullBACKUP` directory before migration.
- Deployed API CRUD and tenant permission verification: NOT RUN. The database
  test covers repository persistence, tenant filtering, duplicate detection and
  soft-delete in a rolled-back transaction; it does not exercise HTTP/auth.

See the [scenario report](../testing/holiday-calendar/tenant-location-refactor/2026-09-16.md).
See also the [target DB migration report](../testing/holiday-calendar/tenant-location-refactor/2026-09-22.md).
The [child module seed report](../testing/holiday-calendar/policy-child-module/2026-09-22.md)
records its target DB and idempotence checks; tenant menu visibility remains unverified.
The [CRUD implementation report](../testing/holiday-calendar/crud/2026-09-22.md)
records the local verification and remaining live tests.
The [import/export report](../testing/holiday-calendar/import-export/2026-09-22.md)
records the seed, CSV round-trip and permission checks.
The [unique-date report](../testing/holiday-calendar/unique-date/2026-09-22.md)
records inactive/date conflict behavior, the database index and remaining HTTP checks.
