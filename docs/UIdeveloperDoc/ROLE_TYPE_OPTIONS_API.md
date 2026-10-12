# Institution Role Type Catalogue

## Behavior

`GET /api/Role/type-options` is the authoritative role-persona catalogue for
Role create, edit, filter and bulk-import guidance. UI code must not maintain a
separate option array or infer labels from numeric IDs.

The endpoint is bearer-authenticated and intentionally accepts no ModuleId or
OperationId. Existing Role mutations keep the established dynamic permission
pipeline.

## Response catalogue

| Id | Code | Name | Registration default |
| ---: | --- | --- | --- |
| 1 | `INSTITUTION_ADMIN` | Institution Administrator | Yes |
| 2 | `STAFF_MEMBER` | Staff Member | Yes |
| 3 | `STAFF_MANAGER` | Staff Manager | Yes |
| 4 | `EXTERNAL_USER` | External User | Yes |
| 5 | `STUDENT` | Student | No |
| 6 | `PARENT_GUARDIAN` | Parent / Guardian | No |

```json
{
  "isSucceeded": true,
  "message": "Role options retrieved successfully.",
  "errors": [],
  "data": [
    {
      "id": 2,
      "code": "STAFF_MEMBER",
      "name": "Staff Member",
      "description": "StaffMember self-service access to personal information, attendance, leave, documents, and assigned work features."
    }
  ]
}
```

Role create/update and bulk import must submit an `id` returned by this catalogue.
Unsupported values are rejected. Student and Parent / Guardian are reserved for
their future modules and are not auto-created during Institution registration.

## Verification status

- Backend role catalogue/mapping gate: PASS locally, 26/26.
- Institution registration regression gate: PASS locally, 14/14.
- Angular focused role/dashboard contract tests: PASS locally, 89/89 after the
  final dashboard-field, symbol and file cleanup.
- Angular production build: PASS locally.
- Database migration: prepared, application status recorded in the scenario report.
- Deployed API/UI verification: PENDING.
