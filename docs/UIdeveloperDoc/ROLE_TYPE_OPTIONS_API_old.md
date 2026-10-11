# Role Type Options API

## Purpose and screen behavior

`GET /api/Role/type-options` is the single source for the Role Type dropdown on
`/app/roles`. The Role list filter and the Add/Edit Role dialog use the same
Angular `RolesApi.roleTypes` signal. The response publishes four professional
access-persona labels while keeping persisted numeric values stable. Role names
such as Sales Manager or Developer and employee Designations remain separate
from this broad dashboard/access category.

Each option also exposes a stable `code`. UI business comparisons and future
API routing should use this code instead of the display name. The numeric `id`
remains the value persisted by the existing Role create/update contract.

The old Angular `ROLE_TYPES` array was removed. Do not add a second hardcoded role
type list in the UI.

## Authentication and permissions

- Bearer authentication is required.
- This constants lookup does not accept or validate `ModuleId` or `OperationId`.
- Angular's module-operation interceptor explicitly leaves this route unchanged.
- Existing Role create, update, list, delete, permission and bulk-import routes
  keep their established permission behavior.

The normal authorization header is supplied by the existing authentication
interceptor:

```http
GET /api/Role/type-options HTTP/1.1
Authorization: Bearer <access-token>
```

No dynamic permission-ID discovery is needed for this route. Numeric permission
IDs must not be appended.

## Request contract

| Method | Route | Query | Body |
| --- | --- | --- | --- |
| GET | `/api/Role/type-options` | None | None |

There are no mandatory or optional request fields, FormData fields, CSV columns,
polling parameters or cancellation commands.

## Success response

```json
{
  "isSucceeded": true,
  "message": "Role options fetched successfully.",
  "data": [
    {
      "id": 1,
      "code": "TENANT_ADMIN",
      "name": "Tenant Administrator",
      "description": "Full company workspace administration, including users, roles, settings, and tenant-level configuration."
    },
    {
      "id": 2,
      "code": "WORKFORCE_USER",
      "name": "Workforce User",
      "description": "Employee self-service access to personal information, attendance, leave, documents, and assigned work features."
    },
    {
      "id": 3,
      "code": "PEOPLE_MANAGER",
      "name": "People Manager",
      "description": "Team management access for supervisors and managers, subject to assigned role permissions."
    },
    {
      "id": 4,
      "code": "EXTERNAL_USER",
      "name": "External User",
      "description": "Limited portal access for clients, consultants, vendors, and other external users, subject to assigned role permissions."
    }
  ],
  "errors": []
}
```

Persisted values are stable: Tenant Administrator `1`, Workforce User `2`,
People Manager `3`, External User `4`. Default Role names are Tenant Administrator,
Workforce Member, People Manager and External Collaborator. Role names remain
separate from their broader Role Type persona.

Stable option codes are `TENANT_ADMIN`, `WORKFORCE_USER`, `PEOPLE_MANAGER` and
`EXTERNAL_USER`. Codes are constants-backed and do not depend on database IDs.
Unsupported persisted values still display as `Unknown` in Role/login response
mapping; they are not published as selectable options.

The Role list returns these default remarks for the generated roles:

| Role type | Default role name | Default remark |
| --- | --- | --- |
| Tenant Administrator | Tenant Administrator | Company workspace, users, roles and settings administration. |
| Workforce User | Workforce Member | Employee self-service, attendance, leave, documents and assigned work. |
| People Manager | People Manager | Team and supervisor management according to assigned permissions. |
| External User | External Collaborator | Client, consultant, vendor and external portal access. |

New tenants persist the matching Role name and remark. For an existing row that
still contains the exact legacy shared generated remark, the Role response returns
the matching professional remark. A customized Role remark is preserved unchanged.

## Error handling

An unauthenticated request is rejected by the API authentication middleware. A
representative envelope is:

```json
{
  "isSucceeded": false,
  "message": "Unauthorized.",
  "data": null,
  "errors": [],
  "errorCode": "UNAUTHORIZED"
}
```

The Role page keeps the dropdown empty when the lookup fails and displays the
central API/interceptor error message. The user may reopen the dialog or reload
the page to retry. There is no background polling or automatic write retry.

## Persistence and isolation

The handler reads application constants only. It does not read or write a database,
does not create tenant-scoped records and has no retention or cleanup behavior.
All authenticated tenants receive the same supported role-type catalogue.

## Angular integration

`RolesApi.getRoleTypeOptions()` loads the endpoint. `RolesList` loads it for the
filter and normal dialog flow. `RoleDialog` also loads it when the shared signal is
empty, covering direct dialog use. Add and Edit use the same API-backed options.

## Verification status — 2026-10-04

- Backend professional persona, stable code, name and remark mapping: 20 passed, 0 failed, 0 skipped.
- Protected tenant-registration regression: 14 passed, 0 failed, 0 skipped.
- Local and Render data migration: 16 default rows updated in each database; zero legacy default names and zero orphan UserRole references.
- Angular workspace was not changed or tested in this backend-only update.
- Authenticated local HTTP and deployed verification: pending.

Detailed evidence: [Professional role-type personas](../testing/role/professional-access-personas/2026-10-04.md).
