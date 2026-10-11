# Skolenza Project Flow

Last updated: 2026-08-29

This document is a high-level guide to the current Skolenza backend flow. It is intended for development, testing, and client-demo reference; it does not contain credentials, tokens, encryption keys, or database connection details.

## 1. Solution structure

```text
Angular / Swagger / Device client
              |
              v
        skolenza.api
 Controllers, JWT, middleware, Swagger, SignalR
              |
              v
    skolenza.application
 DTOs, MediatR commands/queries, handlers, validation, mappings
              |
              v
       skolenza.domain
 Entities, enums, business data model
              |
              v
   skolenza.persistance + infrastructure
 EF Core context, repositories, Unit of Work, PostgreSQL, encryption,
 tokens, permission helpers, external services
```

| Project | Responsibility |
|---|---|
| `skolenza.api` | HTTP endpoints, authentication pipeline, Swagger and error middleware. |
| `skolenza.application` | Request/response DTOs, MediatR handlers, validations and application rules. |
| `skolenza.domain` | Entity classes and shared enums. |
| `skolenza.persistance` | `WorkforceDbContext`, repository implementations and Unit of Work transactions. |
| `skolenza.infrastructure` | JWT/token, encryption, common-request, permission and integration services. |

## 2. Common request flow

```text
Client request
  -> API controller
  -> JWT authentication
  -> MediatR command/query
  -> authenticated-user and permission validation
  -> handler validation/business rules
  -> repository / Unit of Work / PostgreSQL
  -> safe response DTO
  -> API response envelope
```

1. A client sends a request through Angular, Swagger, or an approved device client.
2. `[Authorize]` endpoints validate the JWT bearer token.
3. The controller sends the request DTO to a MediatR command or query.
4. The handler uses existing common-request and permission services to establish the authenticated actor and permitted scope.
5. The handler validates references, active/deleted state, ownership, and business rules.
6. Read operations use scoped repository queries. Write operations use `IUnitOfWork`; related changes that must succeed together are wrapped in one transaction.
7. The handler maps database entities to safe response DTOs. Sensitive values such as passwords, keys, hashes, and refresh tokens are not returned.

## 3. Tenant scope and permissions

### Host users

- Host requests use the existing `HostRuntimePermissionValidator` flow.
- A Super Admin Host can continue without `ModuleId` and `OperationId`.
- Other Host users must supply permitted `ModuleId` and `OperationId` values.
- For Tenant-scoped Host APIs, the requested `TenantId` is an encrypted string. The server decrypts it using the existing `HostTenantIdentifierProtector` / static-salt flow.

### Tenant employees

- Tenant employee requests obtain the authoritative Tenant scope from the authenticated token.
- Existing tenant role/module/operation permission validation is applied.
- A Tenant employee cannot use an encrypted Tenant ID to cross into another Tenant.

### Identifier rule

- Tenant IDs are sent and returned as encrypted strings where the endpoint is Tenant-scoped.
- Other entity IDs, such as `TenantDevice.Id`, `TenantLocationId`, and `DeviceMasterId`, remain numeric unless that API explicitly documents otherwise.
- Raw Tenant IDs are never exposed in Tenant-device API responses.

## 4. Tenant lifecycle overview

```text
Host creates Tenant
  -> Tenant root record
  -> Tenant profile
  -> initial Tenant location
  -> employee-code pattern
  -> email configuration
  -> Tenant users, roles, modules and operational configuration
```

The Tenant onboarding/update flows preserve audit fields and avoid exposing SMTP passwords, secret keys, password hashes, and refresh tokens in response payloads.

## 5. Device management flow

### DeviceMaster

`DeviceMaster` is the global catalog of device models managed by Host users.

Important fields:

- `SNo`: manufacturer serial/model identifier.
- `IsOccupied`: whether the master device is assigned to a live Tenant device.
- `IsActive` and `IsSoftDeleted`: lifecycle flags.

Key endpoints:

```text
POST   /api/DeviceMaster/create
GET    /api/DeviceMaster/get-by-id/{id}
GET    /api/DeviceMaster/get-info-by-sno/{sNo}
GET    /api/DeviceMaster/get-all
POST   /api/DeviceMaster/update
POST   /api/DeviceMaster/update-status
DELETE /api/DeviceMaster/delete/{id}
```

Rules:

1. A new master device is created with `IsOccupied = false`.
2. A master device can be allocated only when it is active, not soft deleted, and not occupied.
3. A master device cannot be updated, deactivated, or deleted when it is used by a live Tenant device or its configuration.

### TenantDevice

`TenantDevice` represents the physical assignment of one master device to a Tenant and location.

```text
Host/Tenant-authorized user
  -> encrypted Tenant ID is resolved
  -> validate Tenant, location and DeviceMaster
  -> create/update TenantDevice
  -> DeviceMaster.IsOccupied is maintained in the same transaction
```

Key endpoints:

```text
POST   /api/TenantDevice/create
GET    /api/TenantDevice/get-by-id/{id}
GET    /api/TenantDevice/get-all
POST   /api/TenantDevice/update
POST   /api/TenantDevice/update-status
DELETE /api/TenantDevice/delete/{id}
```

Tenant device rules:

- `TenantLocationId` must belong to the decrypted/current Tenant and be active and non-soft-deleted.
- The selected `DeviceMasterId` must be active, non-soft-deleted, and unoccupied.
- The device code is unique among live devices for the selected Tenant.
- Changing the assigned master releases the old master and occupies the replacement in one transaction.
- Deactivating or deleting a device is blocked while active/live employee enrollments exist.
- A Tenant device cannot be deleted while its connection configuration exists; delete the configuration first.
- Deleting a Tenant device is soft-delete and releases its master device's occupied state.

### TenantDeviceConfiguration

Connection and device-runtime configuration is intentionally separate from device allocation.

```text
Allocate TenantDevice first
  -> optionally create one TenantDeviceConfiguration later
  -> configure network, server and enrollment settings
  -> device/runtime services update telemetry fields
```

Key endpoints:

```text
POST   /api/TenantDeviceConfiguration/create
GET    /api/TenantDeviceConfiguration/get-by-id/{id}
GET    /api/TenantDeviceConfiguration/get-all
POST   /api/TenantDeviceConfiguration/update
DELETE /api/TenantDeviceConfiguration/delete/{id}
```

Configuration rules:

- A configuration belongs to exactly one `TenantDevice`.
- One live configuration is allowed per Tenant device.
- The parent Tenant device must be active and within the authorized Tenant scope.
- Port and heartbeat values, when supplied, must be positive.
- `Configuration` must contain valid JSON before it is stored in the `jsonb` column.
- Create/update DTOs do not accept runtime telemetry fields such as last heartbeat, last sync, or last connection error.
- Configuration deletion is a hard delete; the physical Tenant device remains allocated.

### Employee device enrollment

```text
TenantDevice is active
  + TenantDeviceConfiguration exists
  + IsEnrollmentEnabled = true
  -> EmployeeDeviceEnrollment is allowed
```

Enrollment searches use the master `SNo` and device code. A device without a configuration, or with enrollment disabled in its configuration, cannot accept a new employee device enrollment.

## 6. Data-state convention

Where a table has both flags:

```text
Usable record = IsActive = true AND IsSoftDeleted is not true
```

Where a table does not have `IsSoftDeleted`, active-only checks are used. Repository queries scope data to its Tenant and avoid returning soft-deleted records.

## 7. Audit convention

Create operations set:

```text
AddedById
AddedDateTime
```

Update operations set:

```text
UpdatedById
UpdatedDateTime
```

Soft-delete operations set:

```text
IsSoftDeleted = true
IsActive = false
SoftDeletedById
SoftDeletedDateTime
```

## 8. Error behavior

- Validation failures, invalid references, ownership failures, duplicate keys, and blocked lifecycle actions are returned through the standard API response/error middleware.
- A generic `INTERNAL_SERVER_ERROR` response means the server log should be checked for the underlying exception.
- For new database-backed fields, EF entity/context mappings and the actual PostgreSQL schema must remain aligned.

## 9. Development checklist

1. Make schema changes in PostgreSQL before using new mapped columns.
2. Update the domain entity, DTOs, `WorkforceDbContext`, mapping profile, repository and handler only where required.
3. Preserve existing central authentication, encryption and permission services; do not create duplicate security flows.
4. Validate Tenant ownership and active/non-soft-deleted states before writes.
5. Use a Unit of Work transaction for related state changes.
6. Run `git diff --check`.
7. Run `dotnet build Skolenza.sln --no-restore --disable-build-servers` after stopping any API process that locks the output DLLs.

