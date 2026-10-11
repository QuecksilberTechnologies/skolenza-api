# Employee device credentials — Angular handoff

## New controller and endpoint catalog

### `TenantCardMasterController` — Host only

This is the new Host-side physical-card inventory controller. A Tenant admin cannot create, price, edit, deactivate, or delete a physical card; they can only bind an already-issued available card to an employee device enrollment.

| Endpoint | What it does | Who calls it |
| --- | --- | --- |
| `POST /api/TenantCardMaster/create` | Adds one purchased physical card to the selected Tenant’s inventory. Encrypts card number and calculates landed cost. | Host user with `HOST_TENANT_CARD_INVENTORY` Create permission |
| `GET /api/TenantCardMaster/get-by-id/{opaqueCardId}` | Returns one masked-card inventory record. | Authorized Host user |
| `GET /api/TenantCardMaster/get-all` | Returns paged inventory; filters: `search`, `cardStatus`, `isActive`, `pageNumber`, `pageSize`. | Authorized Host user |
| `POST /api/TenantCardMaster/update` | Updates invoice/procurement details of an unassigned card. | Authorized Host user |
| `POST /api/TenantCardMaster/update-status` | Activates/deactivates an unassigned card. | Authorized Host user |
| `DELETE /api/TenantCardMaster/delete/{opaqueCardId}` | Soft-deletes an unassigned card. | Authorized Host user |

All Card Master calls include the Host-selected opaque `tenantId`, `moduleId`, and `operationId`. Card number is never returned; response contains only `maskedCardNumber`.

### `EmployeeDeviceEnrollmentController` — Tenant employee/device credential APIs

The controller already existed for employee-device mapping; its API contract is now changed to opaque identifiers and server-owned enrollment IDs. `Employee.Id` is decrypted only in the backend and becomes the physical device `enrollid`.

| Endpoint | What it does | UI call point |
| --- | --- | --- |
| `POST /api/EmployeeDeviceEnrollment/create` | Creates mapping after employee/location/device eligibility checks, saves working windows, queues baseline device user. | Add employee to device |
| `GET /api/EmployeeDeviceEnrollment/get-by-id/{opaqueEnrollmentId}` | Reads one safe enrollment view plus credential queue statuses. | Detail / refresh |
| `GET /api/EmployeeDeviceEnrollment/get-all` | Paged device enrollment list; optional employee/device/location/status filters. | Device-user list |
| `POST /api/EmployeeDeviceEnrollment/update` | Updates validity period and working access windows only. It rejects an `isActive` change; employee/device cannot be swapped here. | Edit schedule |
| `POST /api/EmployeeDeviceEnrollment/update-status` | Enables/disables the enrolled device user and queues `enableuser`; the response includes `userActivationCommandStatus`. | Enable / disable user |
| `DELETE /api/EmployeeDeviceEnrollment/delete/{opaqueEnrollmentId}` | Queues full device-user removal and soft-deletes mapping. | Remove employee from device |
| `POST /api/EmployeeDeviceEnrollment/face/upsert` | Multipart JPEG/PNG face upload; queues protected face command. | Upload employee photo |
| `POST /api/EmployeeDeviceEnrollment/pin/upsert` | Queues a write-only 4–12 digit device PIN. | Set/reset device PIN |
| `POST /api/EmployeeDeviceEnrollment/card/bind` | Binds one active Available Host-issued card and queues card enrollment. | Assign card |
| `POST /api/EmployeeDeviceEnrollment/credential/remove` | Queues removal of exactly Face, Card, or PIN. | Remove one credential |

Every enrollment endpoint uses the existing `EMP_DEVICES` Tenant permission module through `moduleId` and `operationId`. A user can act only on employee data permitted by the established employee data-access behavior.

### Credential type dropdown

Do not hard-code credential values in Angular. Before rendering the **Remove credential** dropdown, call `GET /api/device-ddl-options/employee-device-credentials`. It returns one field named `credentialType` with values `1 = Face biometric`, `2 = Access card`, and `3 = Device PIN`; submit that selected value as `credentialType` to `POST /api/EmployeeDeviceEnrollment/credential/remove`.

### Other new selector APIs (audit complete)

| UI selector | API | Field returned | Notes |
| --- | --- | --- | --- |
| Weekly working-access day | `GET /api/device-ddl-options/employee-device-access-windows` | `dayOfWeek` | Monday-first values `1` through `7`; use in every `accessWindows[]` row. |
| Credential removal type | `GET /api/device-ddl-options/employee-device-credentials` | `credentialType` | Use only for credential removal. |
| Card procurement tax basis | `GET /api/device-ddl-options/tenant-card-inventory` | `taxTreatment` | Use on Card Master create/update. It describes invoice facts; it does not calculate tax. |
| Card inventory lifecycle filter | `GET /api/device-ddl-options/tenant-card-inventory` | `cardStatus` | Use for Card Master `get-all` filter only. Lifecycle transitions remain server-controlled. |
| Purchase currency | `GET /api/Enum/get-all-currencies` | existing currency provider DTO | Reuse the platform currency provider; do not duplicate an incomplete foreign-currency list in DeviceDdl. |

## Non-negotiable identity rule

Angular receives and sends opaque encrypted IDs only. For every device command, the API decrypts `employeeId` to the global database `Employee.Id` (`bigint`) and sends that numeric value as the vendor `enrollid`. This supports ID values beyond `50,000,001`; no tenant-local employee-ID range or client-side mapping is used. The raw `Employee.Id`, `TenantDeviceId`, device master information, device serial, PIN, card number, and face image are never returned to Angular.

The enrollment mapping remains necessary even though `enrollid` equals `Employee.Id`: it records the Tenant, physical Tenant device, location, active status, working-access windows, card assignment, credential queue state, and audit trail.

## Screen flow

1. Select employee from the existing employee list (opaque `employeeId`) and a device from the Tenant device list (opaque `tenantDeviceId`).
2. Submit `POST /api/EmployeeDeviceEnrollment/create` with `employeeId`, `tenantDeviceId`, optional validity dates, and `accessWindows`. The API verifies the employee’s active, attendance-enabled assignment to that device’s location. It then queues `setuserinfo`; `admin` is forced to `0` (User) by the server.
3. Refresh with `GET /api/EmployeeDeviceEnrollment/get-by-id/{opaqueEnrollmentId}` or `get-all`. Use `faceDeploymentStatus`, `cardDeploymentStatus`, `pinDeploymentStatus`, plus `faceCommandStatus`, `cardCommandStatus`, `pinCommandStatus`, and `userActivationCommandStatus` for UI state. `Queued` means accepted into the durable command queue—not a successful device acknowledgement. These live states come from the durable `DeviceCommand` row and become `Completed` or `Failed` after the device response is recorded.
4. Use the selected face-file control only with `POST /api/EmployeeDeviceEnrollment/face/upsert` as multipart form data: `enrollmentId`, `faceImage` (JPEG/PNG, 2 MB max). The source image is converted to a protected queue payload; only a SHA-256 hash is retained. A future physical-device face-capture flow can use this same enrollment record and queue state.
5. Use `POST /api/EmployeeDeviceEnrollment/pin/upsert` with `enrollmentId` and a 4–12 digit `pin`. Never display a stored PIN; the API does not store one.
6. For a card, show only an active, Available Host-issued card from Tenant card inventory and call `POST /api/EmployeeDeviceEnrollment/card/bind` with `enrollmentId`, `tenantCardId`. The API decrypts it internally, sends it in a protected queue payload, and changes inventory state to Assigned.
7. To remove an individual credential call `POST /api/EmployeeDeviceEnrollment/credential/remove` with `enrollmentId` and `credentialType` (`Face`, `Card`, `Pin`). Deleting the enrollment queues deletion of the full device user and returns an assigned card to Available.

## Request shape

```json
{
  "employeeId": "opaque-employee-id",
  "tenantDeviceId": "opaque-device-id",
  "accessEffectiveFromDateTime": "2026-09-08T00:00:00Z",
  "accessEffectiveToDateTime": null,
  "accessWindows": [
    { "dayOfWeek": "Monday", "startLocalTime": "09:00:00", "endLocalTime": "18:00:00", "isActive": true }
  ],
  "isActive": true,
  "moduleId": 0,
  "operationId": 0
}
```

`accessWindows` is the professional business schedule model: multiple local-time windows per day are supported, and it is not forced into the vendor’s ambiguous numeric `access_times` field. Use the existing work-arrangement/pattern APIs for WFH/hybrid policy. An employee who never uses a biometric device does not need an enrollment or card; their WFH/hybrid records work independently.

## Queue and log behaviour

Every device mutation is persisted in the existing per-device command queue with `ProtectPayload=true`. HTTPS devices receive it on their next outbound gateway poll; MQTTS devices can use the existing authorized manual dispatch endpoint. No background service opens a LAN connection to a device. Search device command/application logs using the server-owned employee ID only on server tools, or use the returned enrollment credential status in Angular. Do not surface queue payloads, device URL tokens, or credential values.

### Real-time HTTPS example

At 09:00, a Tenant admin saves a face image:

1. Angular posts multipart data to `POST /api/EmployeeDeviceEnrollment/face/upsert`; it receives an enrollment response whose `faceDeploymentStatus` is `Queued`.
2. The API saves a protected `setuserinfo` command in the existing queue. It does **not** contact `192.168.x.x`.
3. At 09:00:15, the HTTPS device calls its configured `/device-gateway/{opaqueToken}` URL. The gateway atomically returns the next queue command.
4. The device executes `setuserinfo` and posts its vendor response. The existing device command response record stores the redacted reply; command status becomes `Completed` or `Failed`.
5. Angular polls the enrollment `get-by-id` endpoint after the normal device heartbeat / command-status refresh. `faceCommandStatus` becomes `Completed` or `Failed`; do not mark the face as device-confirmed merely because the submit request was accepted.

For MQTTS, steps 1–2 are identical. The Tenant may use the existing authorized `POST /api/TenantDeviceConfiguration/dispatch-mqtts-now` action to publish the next already queued command; it does not accept a raw command payload.

### Angular API checklist

| UI action | Endpoint | Required inputs | Expected UI state |
| --- | --- | --- | --- |
| Load enrollments | `GET /api/EmployeeDeviceEnrollment/get-all` | filters, module/operation | paginated opaque IDs only |
| Add device user | `POST /api/EmployeeDeviceEnrollment/create` | employeeId, tenantDeviceId, access windows | baseline user command queued |
| Change access windows | `POST /api/EmployeeDeviceEnrollment/update` | same employee/device IDs, windows | saved business schedule |
| Enable/disable user | `POST /api/EmployeeDeviceEnrollment/update-status` | enrollment ID, isActive | `enableuser` queued; show `userActivationCommandStatus` until Completed/Failed |
| Upload face | `POST /api/EmployeeDeviceEnrollment/face/upsert` | multipart enrollmentId, faceImage | Face `Queued` |
| Set/reset PIN | `POST /api/EmployeeDeviceEnrollment/pin/upsert` | enrollmentId, PIN | PIN `Queued` |
| Bind issued card | `POST /api/EmployeeDeviceEnrollment/card/bind` | enrollmentId, tenantCardId | Card `Queued`, card Assigned |
| Remove one credential | `POST /api/EmployeeDeviceEnrollment/credential/remove` | enrollmentId, credential type | protected delete command queued |
| Remove full device user | `DELETE /api/EmployeeDeviceEnrollment/delete/{id}` | opaque ID, module/operation | full device-user delete queued |

Every endpoint above requires the normal `moduleId` and `operationId`. The employee-device feature uses `EMP_DEVICES`; the Host inventory feature uses `HOST_TENANT_CARD_INVENTORY`. The backend permission behaviors compare the supplied module to these server-owned codes before handler execution.

## Host card inventory data model

`TenantCardMaster` is Host-only inventory. It stores encrypted card number plus keyed lookup hash, lifecycle (`Available`, `Assigned`, `Blocked`, `Lost`, `Returned`, `Retired`), invoice/supplier metadata, actual unit price, shipping, customs duty, and the invoice’s CGST/SGST/IGST/foreign-tax amounts. `LandedCost` is server-calculated from those actual values.

Host inventory endpoints are `POST /api/TenantCardMaster/create`, `GET /api/TenantCardMaster/get-by-id/{opaqueId}`, `GET /api/TenantCardMaster/get-all`, `POST /api/TenantCardMaster/update`, `POST /api/TenantCardMaster/update-status`, and `DELETE /api/TenantCardMaster/delete/{opaqueId}`. Every call requires the encrypted selected `tenantId` plus the Host Card Inventory module/operation permission. An assigned card cannot be edited, deactivated, or deleted until it has been unbound from its employee enrollment.

For an MP supplier sending cards to Delhi, record the invoice’s actual interstate treatment and IGST amount; a same-state supply records the invoice’s CGST and SGST. Imports/foreign procurement retain currency, foreign tax and customs duty. The API must not guess a tax rate or “cheapest” source. GST treatment depends on the documented place of supply and invoice facts. [CBIC IGST Act](https://cbic-gst.gov.in/hindi/IGST-bill-e.html) and [CBIC FAQ](https://cbic-gst.gov.in/faq.html) provide the governing reference.

## Deployment prerequisite

Apply [TenantCardMaster_EmployeeDeviceCredential_Upgrade.sql](/C:/SkolenzaCodeBase/QuecksilberTechnologies/database-scripts/TenantCardMaster_EmployeeDeviceCredential_Upgrade.sql) before deploying this API version. The script intentionally refuses to migrate an old enrollment whose device has no location, rather than creating an unsafe location-less mapping.
