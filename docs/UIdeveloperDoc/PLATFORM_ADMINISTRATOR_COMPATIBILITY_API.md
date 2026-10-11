# Platform Administrator compatibility API

## Behavior

Skolenza exposes Platform Administrator terminology without breaking existing
AxionPro Host consumers. Each new route is an alias on the existing authorized
controller, so request DTOs, response envelopes, MediatR handlers, validation,
database writes and permission behavior are identical.

| Current Skolenza prefix | Legacy prefix retained | Purpose |
| --- | --- | --- |
| `/api/Platform` | `/api/Host` | Platform users, roles and module catalogue |
| `/api/PlatformAccess` | `/api/HostAccess` | Token-scoped access bootstrap |
| `/api/PlatformRolePermission` | `/api/HostRolePermission` | Role/module/operation grants |

All action suffixes remain unchanged in this compatibility slice. Examples:

- `GET /api/Platform/get-all-host-users`
- `GET /api/Platform/get-host-user-by-id/{id}`
- `POST /api/Platform/create-host-user`
- `POST /api/Platform/update-host-user`
- `POST /api/Platform/delete-host-user`
- `GET /api/Platform/get-all-host-roles`
- `GET /api/Platform/get-host-role-by-id/{id}`
- `POST /api/Platform/create-host-role`
- `POST /api/Platform/update-host-role`
- `POST /api/Platform/delete-host-role`
- `GET /api/Platform/get-host-modules?isActive=true`
- `GET /api/PlatformAccess/bootstrap`
- `GET /api/PlatformRolePermission/get-role-module-permissions/{hostRoleId}?isActive=true`
- `POST /api/PlatformRolePermission/save-role-module-permissions`

## Authentication and permissions

Send the existing bearer token. All three controllers require an authenticated
session. The new prefixes do not introduce a permission bypass or separate
permission calculation. UI code must continue resolving module and operation
IDs from the authenticated menu/access bootstrap; numeric IDs are environment
specific and must not be hard-coded.

## Request and response examples

The compatibility aliases deliberately keep legacy JSON property names until a
separately versioned contract migration is approved.

```http
GET /api/Platform/get-all-host-roles
Authorization: Bearer <token>
```

```json
{
  "isSucceeded": true,
  "message": "Host roles fetched successfully.",
  "errors": [],
  "data": []
}
```

```http
GET /api/PlatformRolePermission/get-role-module-permissions/7?isActive=true
Authorization: Bearer <token>
```

The response and all validation/error envelopes are the same as the matching
legacy endpoint. There is no polling, retry or cancellation protocol for these
synchronous administration calls.

## Persistence and deployment status

This slice changes no table and copies no data. Both prefixes reach the same
existing Host persistence records. Local route-contract tests and the Angular
API-client tests passed on 2026-10-10. Deployment has not been independently
verified, so local success is not deployment acceptance.

