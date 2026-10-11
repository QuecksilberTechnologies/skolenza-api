# Skolenza Code Flow Reference

Last updated: 2026-08-29

This document explains how a request travels through the Skolenza codebase. The examples focus on the current Tenant Device module because it demonstrates the complete Controller -> MediatR -> Authorization -> Handler -> Repository -> Entity -> Response pattern.

## 1. Generic API code path

```text
HTTP request
  |
  v
Controller action                         skolenza.api/Controllers
  |
  | mediator.Send(command/query)
  v
MediatR command or query                 skolenza.application/Features
  |
  v
Handler
  |- authenticated-user validation        ICommonRequestService
  |- role/module/operation validation     central permission validators
  |- request/business validation
  |- mapping                              AutoMapper MappingProfile
  |
  v
Repository / Unit of Work                skolenza.persistance/Repositories
  |
  v
WorkforceDbContext -> PostgreSQL         skolenza.persistance/Data/Context
  |
  v
Safe response DTO -> ApiResponse<T>
```

## 2. Startup and middleware flow

Entry point: `skolenza.api/Program.cs`

```text
Program.cs
  -> adds Application services
  -> adds Infrastructure services
  -> adds Persistence services
  -> configures JWT bearer authentication
  -> configures CORS, Swagger and SignalR
  -> maps controllers
  -> error middleware returns standard API error envelope
```

All controller endpoints marked with `[Authorize]` receive a validated JWT before their action runs. Authentication failures are returned in the standard Skolenza error shape.

## 3. Tenant-scoped authorization flow

Shared base handler:

```text
skolenza.application/Features/HostDeviceCmd/Handlers/TenantDeviceHandlers.cs
  TenantDeviceAccessHandlerBase.ResolveTenantScopeAsync(...)
```

```text
Request DTO contains
  TenantId (encrypted string)
  ModuleId / OperationId (normal Host user when required)
  |
  v
ValidateAuthenticatedRequestAsync()
  |
  +-- Host user
  |     -> HostRuntimePermissionValidator.ValidateAsync(...)
  |     -> Super Admin Host: central bypass supports missing ModuleId/OperationId
  |     -> normal Host: central module/operation permission validation
  |     -> HostTenantIdentifierProtector.Decrypt(encrypted TenantId)
  |
  +-- Tenant employee
        -> ValidateTenantUserRequestAsync()
        -> ValidateTenantPermissionAsync(...)
        -> Tenant ID is taken from the trusted login/token scope
  |
  v
TenantDeviceAccessScope
  TenantId, ActorId, TenantEncryptionKey
```

The response uses the same trusted `TenantEncryptionKey` to encrypt `TenantId` again. Raw Tenant IDs are never returned from these endpoints.

## 4. DeviceMaster code flow

Main files:

```text
Controller: skolenza.api/Controllers/HostDevice/DeviceMasterController.cs
Handler:    skolenza.application/Features/HostDeviceCmd/Handlers/DeviceMasterHandlers.cs
DTO:        skolenza.application/DTOS/Host/DeviceManagementDTOs.cs
Repository: skolenza.persistance/Repositories/DeviceManagementRepositories.cs
Entity:     skolenza.domain/Entity/DeviceMaster.cs
Context:    skolenza.persistance/Data/Context/WorkforceDbContext.cs
```

### Get all DeviceMaster records

```text
GET /api/DeviceMaster/get-all
  -> DeviceMasterController.GetAll(filter)
  -> GetAllDeviceMastersQuery(filter)
  -> GetAllDeviceMastersQueryHandler.Handle(...)
  -> ValidateHostUserRequestAsync()
  -> DeviceMasterRepository.GetPagedAsync(filter)
  -> WorkforceDbContext.DeviceMasters
       where IsSoftDeleted = false
       optional: search, device type, IsActive, IsOccupied
  -> AutoMapper: DeviceMaster -> DeviceMasterResponseDTO
  -> ApiResponse<List<DeviceMasterResponseDTO>>
```

### Create DeviceMaster

```text
POST /api/DeviceMaster/create
  -> CreateDeviceMasterCommand
  -> CreateDeviceMasterCommandHandler
  -> validate required values including SNo
  -> reject duplicate device code / company + model combination
  -> map DTO to DeviceMaster
  -> set IsOccupied = false
  -> set AddedById and AddedDateTime
  -> DeviceMasterRepository.AddAsync()
  -> UnitOfWork.SaveChangesAsync()
  -> safe DeviceMaster response
```

### Update / deactivate / delete protections

```text
Update DeviceMaster
  -> reject when IsOccupied = true

Deactivate or delete DeviceMaster
  -> reject when IsOccupied = true
  -> reject when a live TenantDevice references it
  -> reject when a TenantDeviceConfiguration references its TenantDevice
```

`SNo` and `IsOccupied` must exist in the PostgreSQL `skolenza."DeviceMaster"` table because EF maps both fields.

## 5. TenantDevice code flow

Main files:

```text
Controller: skolenza.api/Controllers/HostDevice/TenantDeviceController.cs
Handler:    skolenza.application/Features/HostDeviceCmd/Handlers/TenantDeviceHandlers.cs
DTO:        skolenza.application/DTOS/Host/DeviceManagementDTOs.cs
Repository: skolenza.persistance/Repositories/DeviceManagementRepositories.cs
Entity:     skolenza.domain/Entity/TenantConfigurationEntities.cs (TenantDevice)
```

### Create TenantDevice

```text
POST /api/TenantDevice/create
  -> TenantDeviceController.Create(dto)
  -> CreateTenantDeviceCommand(dto)
  -> CreateTenantDeviceCommandHandler.Handle(...)
  -> ResolveTenantScopeAsync(dto)
  -> TenantDeviceValidation.Validate(dto)
  -> ValidateReferencesAsync(...)
       Tenant active and not soft-deleted
       Location active, not soft-deleted, belongs to Tenant
       DeviceMaster active and not soft-deleted
  -> ValidateUniqueDeviceCodeAsync(...)
  -> UnitOfWork.BeginTransactionAsync()
  -> get DeviceMaster as tracked entity
  -> reject if DeviceMaster.IsOccupied = true
  -> map DTO -> TenantDevice
  -> assign TenantId from trusted scope
  -> set AddedById / AddedDateTime / IsSoftDeleted = false
  -> set DeviceMaster.IsOccupied = true
  -> UnitOfWork.SaveChangesAsync()
  -> UnitOfWork.CommitTransactionAsync()
  -> reload scoped TenantDevice with display references
  -> encrypt TenantId in TenantDeviceResponseDTO
```

### Update TenantDevice

```text
POST /api/TenantDevice/update
  -> UpdateTenantDeviceCommandHandler
  -> resolve trusted Tenant scope
  -> load only the requested Tenant's live device
  -> validate Tenant/location/master/device code
  -> if IsActive changes true -> false, reject active employee enrollments
  -> transaction begins
  -> if DeviceMasterId changed:
       old DeviceMaster.IsOccupied = false
       replacement must be active, not deleted, not occupied
       replacement DeviceMaster.IsOccupied = true
  -> map editable allocation fields
  -> set UpdatedById / UpdatedDateTime
  -> save + commit
  -> return encrypted-Tenant response
```

### TenantDevice status and delete

```text
POST /api/TenantDevice/update-status
  -> deactivate blocked when active employee enrollments exist
  -> reactivate revalidates Tenant, location and DeviceMaster
  -> sets UpdatedById / UpdatedDateTime

DELETE /api/TenantDevice/delete/{id}
  -> live employee enrollment check
  -> existing configuration check
  -> transaction begins
  -> soft-delete TenantDevice and set inactive
  -> set SoftDeletedById / SoftDeletedDateTime
  -> release DeviceMaster.IsOccupied = false
  -> save + commit
```

## 6. TenantDeviceConfiguration code flow

Main files:

```text
Controller: skolenza.api/Controllers/HostDevice/TenantDeviceConfigurationController.cs
Handler:    skolenza.application/Features/HostDeviceCmd/Handlers/TenantDeviceHandlers.cs
Repository: skolenza.persistance/Repositories/DeviceManagementRepositories.cs
Entity:     skolenza.domain/Entity/TenantConfigurationEntities.cs (TenantDeviceConfiguration)
```

### Create configuration

```text
POST /api/TenantDeviceConfiguration/create
  -> CreateTenantDeviceConfigurationCommandHandler
  -> ResolveTenantScopeAsync(dto)
  -> validate TenantDeviceId, enum values, ports, heartbeat, JSON configuration
  -> verify parent TenantDevice is active and belongs to trusted Tenant
  -> verify no configuration already exists for that device
  -> map request DTO -> TenantDeviceConfiguration
  -> set AddedById / AddedDateTime
  -> repository AddAsync + SaveChangesAsync
  -> reload with TenantDevice and DeviceMaster display values
  -> encrypt parent TenantId in response
```

### Update configuration

```text
POST /api/TenantDeviceConfiguration/update
  -> resolve trusted Tenant scope
  -> load only configuration whose parent device belongs to Tenant
  -> validate requested parent device and one-configuration-per-device rule
  -> map editable connection fields
  -> preserve runtime telemetry fields
       LastHeartbeatDateTime
       LastSyncDateTime
       LastAttendanceReceivedDateTime
       LastSuccessfulConnectionDateTime
       LastFailedConnectionDateTime
       LastConnectionError
  -> set UpdatedById / UpdatedDateTime
  -> save and return safe response
```

### Delete configuration

```text
DELETE /api/TenantDeviceConfiguration/delete/{id}
  -> resolve trusted Tenant scope
  -> load scoped configuration
  -> TenantDeviceConfigurationRepository.Remove(entity)
  -> SaveChangesAsync()
  -> physical row is hard-deleted
```

The configuration is separate from allocation: a Tenant device can be assigned first and configured later.

## 7. Employee enrollment dependency flow

```text
Create/Update EmployeeDeviceEnrollment
  -> EmployeeDeviceEnrollmentRepository.IsEligibleTenantDeviceAsync(...)
  -> TenantDevice must be active and not soft-deleted
  -> TenantDeviceConfiguration must exist
  -> TenantDeviceConfiguration.IsEnrollmentEnabled must be true
  -> enrollment is allowed
```

If this eligibility check fails, no employee enrollment is created or updated.

## 8. Mapping flow

All mapping profiles are in:

```text
skolenza.application/Mappings/MappingProfile.cs
```

Important mapping protections:

- Request DTOs do not overwrite entity audit values.
- Tenant-device request DTOs do not overwrite the trusted Tenant ID.
- Entity navigation properties are ignored during request mapping.
- Configuration request mapping ignores telemetry fields.
- Tenant ID is manually encrypted only after response mapping.

## 9. Error flow

```text
ValidationErrorException / NotFoundException / ConflictException / UnauthorizedAccessException
  -> API error middleware
  -> standard HTTP status + Skolenza response envelope

Unexpected exception
  -> HTTP 500
  -> ErrorCode: INTERNAL_SERVER_ERROR
  -> check API server exception log and DB schema alignment
```

## 10. Debugging order

When an endpoint fails:

1. Check HTTP status and error envelope in Swagger/Angular.
2. Check the running API console output for the original exception.
3. Verify the JWT user type and supplied module/operation values.
4. For Tenant APIs, verify that `tenantId` is encrypted and was generated with the correct key flow.
5. Verify active/non-soft-deleted Tenant, location, master and parent records.
6. Verify the PostgreSQL table has every field mapped in the EF entity/context.
7. Check the repository query for Tenant ownership and soft-delete conditions.

## 11. Constants reference

Main file:

```text
skolenza.application/Constants/AppConstants.cs
```

Handlers do not hardcode client-facing success, validation, conflict, or not-found messages. They use `AppConstants` so the API returns consistent response text and error codes.

| Constant group | Used for | Device-management examples |
|---|---|---|
| `AppConstants.ErrorCodes` | Standard error code returned by API middleware. | `VALIDATION_ERROR`, `NOT_FOUND`, `CONFLICT`, `INTERNAL_SERVER_ERROR` |
| `AppConstants.ErrorMessages` | Exception messages thrown by handlers. | `DeviceMasterNotFound`, `InvalidDeviceMaster`, `TenantDeviceNotFound`, `TenantDeviceConfigurationAlreadyExists`, `TenantDeviceEnrollmentInUse` |
| `AppConstants.SuccessMessages` | Success message placed in `ApiResponse<T>`. | `DeviceMasterCreated`, `TenantDeviceUpdated`, `TenantDeviceConfigurationDeleted` |

Example usage:

```text
Invalid master selected
  -> throw ValidationErrorException(AppConstants.ErrorMessages.InvalidDeviceMaster)
  -> API middleware returns validation error envelope

Existing configuration found
  -> throw ConflictException(AppConstants.ErrorMessages.TenantDeviceConfigurationAlreadyExists)
  -> API middleware returns conflict error envelope
```

When adding a new reusable message or error code, add it to `AppConstants` first and use the constant from the handler. Do not duplicate a literal message across handlers.

## 12. Enum reference

Device-management enum file:

```text
skolenza.domain/Entity/DeviceManagementEnums.cs
```

| Enum | Values / purpose | Used by |
|---|---|---|
| `DeviceType : short` | `Face`, `Fingerprint`, `Card`, `FaceFingerprint`, `FaceCard`, `MultiBiometric`, `AccessControl`, `Other` | `DeviceMaster.DeviceType`, create/update DTO, list filter, response display name |
| `DeviceCommunicationType : short` | `Http`, `Https`, `WebSocket`, `Tcp`, `CloudApi`, `PushSdk` | `TenantDeviceConfiguration.CommunicationType`, create/update DTO and list filter |

Enum data flow:

```text
Swagger/Angular numeric enum value
  -> DTO enum
  -> MappingProfile converts enum to short for entity/database
  -> PostgreSQL smallint
  -> MappingProfile converts short back to response enum
```

The configuration handler validates communication type with `Enum.IsDefined(...)` before saving it.

## 13. Endpoint documentation and Swagger

Swagger setup:

```text
skolenza.api/Program.cs
  -> AddSwaggerGen(...)
  -> loads skolenza.api.xml
  -> IncludeXmlComments(...)
```

Each controller/action should have XML documentation (`/// <summary>` and, where useful, `/// <remarks>`). Swagger reads these comments and exposes the endpoint purpose and request contract.

### DeviceMaster endpoints

Controller: `skolenza.api/Controllers/HostDevice/DeviceMasterController.cs`

| Method | Route | Command/query handler |
|---|---|---|
| `POST` | `/api/DeviceMaster/create` | `CreateDeviceMasterCommandHandler` |
| `GET` | `/api/DeviceMaster/get-by-id/{id}` | `GetDeviceMasterByIdQueryHandler` |
| `GET` | `/api/DeviceMaster/get-info-by-sno/{sNo}` | `GetDeviceMasterInfoBySNoQueryHandler` |
| `GET` | `/api/DeviceMaster/get-all` | `GetAllDeviceMastersQueryHandler` |
| `POST` | `/api/DeviceMaster/update` | `UpdateDeviceMasterCommandHandler` |
| `POST` | `/api/DeviceMaster/update-status` | `UpdateDeviceMasterStatusCommandHandler` |
| `DELETE` | `/api/DeviceMaster/delete/{id}` | `DeleteDeviceMasterCommandHandler` |

### TenantDevice endpoints

Controller: `skolenza.api/Controllers/HostDevice/TenantDeviceController.cs`

| Method | Route | Command/query handler |
|---|---|---|
| `POST` | `/api/TenantDevice/create` | `CreateTenantDeviceCommandHandler` |
| `GET` | `/api/TenantDevice/get-by-id/{id}` | `GetTenantDeviceByIdQueryHandler` |
| `GET` | `/api/TenantDevice/get-all` | `GetAllTenantDevicesQueryHandler` |
| `POST` | `/api/TenantDevice/update` | `UpdateTenantDeviceCommandHandler` |
| `POST` | `/api/TenantDevice/update-status` | `UpdateTenantDeviceStatusCommandHandler` |
| `DELETE` | `/api/TenantDevice/delete/{id}` | `DeleteTenantDeviceCommandHandler` |

### TenantDeviceConfiguration endpoints

Controller: `skolenza.api/Controllers/HostDevice/TenantDeviceConfigurationController.cs`

| Method | Route | Command/query handler |
|---|---|---|
| `POST` | `/api/TenantDeviceConfiguration/create` | `CreateTenantDeviceConfigurationCommandHandler` |
| `GET` | `/api/TenantDeviceConfiguration/get-by-id/{id}` | `GetTenantDeviceConfigurationByIdQueryHandler` |
| `GET` | `/api/TenantDeviceConfiguration/get-all` | `GetAllTenantDeviceConfigurationsQueryHandler` |
| `POST` | `/api/TenantDeviceConfiguration/update` | `UpdateTenantDeviceConfigurationCommandHandler` |
| `DELETE` | `/api/TenantDeviceConfiguration/delete/{id}` | `DeleteTenantDeviceConfigurationCommandHandler` |

### Tenant plan-entitlement synchronization endpoint

Controller: `skolenza.api/Controllers/Tenant/TenantController.cs`

| Method | Route | Command/handler |
|---|---|---|
| `POST` | `/api/Tenant/sync-active-plan-entitlements` | `SynchronizeTenantPlanEntitlementsCommandHandler` |

Request DTO: `SynchronizeTenantPlanEntitlementsRequestDTO`

```json
{
  "tenantId": "encrypted-tenant-id",
  "moduleId": 0,
  "operationId": 0
}
```

Flow:

```text
encrypted tenantId
  -> HostRuntimePermissionValidator
     -> Super Admin bypass OR current Host module/operation permission
  -> HostTenantIdentifierProtector decrypts tenantId
  -> current active TenantSubscription and active SubscriptionPlan
  -> directly mapped active Module where scope is Tenant (all common and leaf states retained)
  -> active non-common ModuleOperationMapping for separately selected non-common Tenant leaf modules
  -> only missing TenantEnabledModule / TenantEnabledOperation rows are staged
  -> one UnitOfWork transaction commits the snapshot additions
  -> response contains encrypted tenantId and count-only sync result
```

The action is explicit and additive. It never removes or changes existing Tenant entitlement records, so a global `PlanModuleMapping` edit never affects an existing Tenant until an authorized Host user invokes this endpoint. The safe response includes the matching Module/Operation master fields and whether each row already existed. The success text comes from `AppConstants.SuccessMessages.TenantPlanEntitlementsSynchronizedSuccessfully`.

### Project-detail feature-pages endpoint

Controller: `skolenza.api/Controllers/ProjectDetail/ProjectDetailController.cs`

| Method | Route | Query/handler |
|---|---|---|
| `GET` | `/api/ProjectDetail/get-all?scope={1\|2\|3}` | `GetFeaturePagesQueryHandler` |

This is a public read-only endpoint by design: it deliberately does not run token or permission validation. Query parameter `scope` is optional: `1` returns Tenant records, `2` returns Host records, `3` returns Common-menu records, and an omitted value returns all three. It returns only active master records (`Module.TenantId == null`) without recursive `children` nesting. A top-level feature header contains flat `childHeaders`; a leaf such as Common-menu `Message` is attached to its nearest header in `operationalPages`, even when its `operations` array is empty. Only `isLeafNode: true` operational pages expose `operations`, sourced from active `ModuleOperationMapping` records with active Operation master details and configured icon/page metadata. The response provides semantic `moduleScope` values (Tenant `1`, Host `2`, Common `3`) together with `moduleScopeName`. Tenant Module rows, inactive Modules, inactive mappings, and inactive Operations are not returned.

Response DTOs:

```text
FeaturePageResponseDTO
  -> id, moduleCode, moduleName, displayName, urlPath, iconKey
  -> parentModuleId, isLeafNode, isModuleDisplayInUI, moduleScope, moduleScopeName
  -> childHeaders[] (top-level feature header only)
  -> operationalPages[] (nearest child/header only)
  -> operations[] (operational leaf page only)

FeaturePageOperationResponseDTO
  -> id, moduleOperationMappingId, operationName, operationType, remark
  -> iconKey, pageUrl, dataViewStructureId, pageTypeId
  -> isCommonItem, isOperational, priority
```

### Endpoint documentation checklist

When a new endpoint is added:

1. Add XML `summary` documentation to its controller action.
2. Describe the expected authorization scope and whether `TenantId` must be encrypted.
3. Name the request DTO and safe response DTO in the code flow document when it is a new module.
4. Make sure the controller route, MediatR command/query, handler, repository method, constants, and enums are traceable from this document.
