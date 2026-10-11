# Device HTTPS onboarding runbook

## Purpose

This is the single operational runbook for onboarding, resetting, recovering,
and verifying a physical biometric device over Skolenza HTTPS polling. Keep
this file updated when the device flow changes.

This runbook covers the HTTPS flow only. MQTT/MQTTS settings, broker
credentials, and CA certificates are not part of it.

For Tenant runtime settings, local Web UI/API access, physical System-menu
PIN, and a **live remote gateway replacement** that preserves the active URL
until confirmation, use
[TenantDeviceRuntimeSettingsUiHandoff.md](TenantDeviceRuntimeSettingsUiHandoff.md).
The normal rotate flow in this runbook remains the technician/manual recovery
flow and invalidates the previous gateway URL immediately.

## Core model

The device always makes an **outbound HTTPS POST** to Skolenza. Skolenza does
not open an inbound connection to the device's private LAN address.

There are two distinct routes:

| Situation | Host/Admin API | URL placed in the physical device |
| --- | --- | --- |
| Brand-new, unassigned inventory device | `POST /api/TenantDeviceConfiguration/issue-bootstrap-url` | Temporary `/api/initial/{serial}/{token}` URL |
| Existing assigned device, or a factory-reset device whose Tenant device/configuration records already exist | `POST /api/TenantDeviceConfiguration/rotate-https-ingress-token` | Normal `/device-gateway/{token}` URL |

Never interchange these flows. The bootstrap API rejects an occupied/assigned
device; rotating a normal gateway URL invalidates the previous normal URL.

## Roles

| Role | Responsibility |
| --- | --- |
| Host Admin | Maintains device inventory, creates/changes the physical Tenant-device assignment, and issues a bootstrap or HTTPS gateway URL. It cannot read, update, delete, or apply runtime configuration after assignment. |
| Tenant/authorized device administrator | Owns the complete assigned-device configuration: creates, reads, updates, and deletes it; rotates the normal gateway URL; applies runtime configuration; and reboots the device. |
| Technician | Connects device to the network and enters the complete returned URL on the physical device. |
| Physical device | Polls the public Skolenza HTTPS endpoint. It never receives an unsolicited Internet connection. |

## Tenant privacy boundary

`DeviceMaster` is Host inventory/catalogue data. It can identify the supplier,
model, or serial of a physical device and must not be displayed in a Tenant or
client-facing screen/API response. Tenant-facing device screens should use only
the Tenant's own `deviceCode` and `deviceName`.

The API suppresses `deviceMasterName`, `deviceMasterModelNo`, and
`deviceMasterSNo` for Tenant Employee responses. Host Admin responses retain
those fields for internal inventory support.

### Identifier boundary

Host Admin may use the raw `DeviceMasterId` only while selecting inventory for
the initial Tenant-device assignment. Once a `TenantDevice` exists, its
database ID and its `TenantDeviceConfiguration` database ID are never exposed
through the public device APIs. The API returns the existing field names
(`id`, `tenantDeviceId`) as opaque, salted `IIdEncoderService` tokens, and all
subsequent device/configuration requests must send those tokens back. This is
the same identifier-protection pattern used by Employee handlers.

For example, do not place a numeric `1` in a URL or request body. Use the
opaque value returned by the preceding create/list response instead. The
server decrypts it only after it has resolved the authenticated Tenant scope.

| Action after assignment | Identifier sent by UI/API client |
| --- | --- |
| Read or delete a Tenant device | `GET`/`DELETE .../get-by-id/{tenantDeviceToken}` or `.../delete/{tenantDeviceToken}` |
| Create a connection configuration | `tenantDeviceId: "{tenantDeviceToken}"` |
| Read or delete a configuration | `GET`/`DELETE .../get-by-id/{configurationToken}` or `.../delete/{configurationToken}` |
| Update a configuration / rotate gateway URL | `id` or `tenantDeviceConfigurationId: "{configurationToken}"` |
| Apply runtime configuration or reboot | `tenantDeviceId: "{tenantDeviceToken}"` |

For an already-created record after deployment, call the relevant `get-all`
endpoint once and take the opaque `id`/`tenantDeviceId` from that response.
Do not reuse the old numeric ID from an earlier Postman request or URL.

## Required production configuration

The configuration record must be valid before a normal gateway URL can be
generated:

```text
CommandTransport           = 4 (HTTPS)
ServerHost                 = skolenza-api.onrender.com
ServerUrl                  = https://skolenza-api.onrender.com
ServerPath                 = /device-gateway
ServerPort                 = 443
HeartbeatIntervalSeconds   = 10 to 3600
```

The DeviceMaster/Inventory record must be active, support HTTPS, and have the
same serial number as the physical device. A factory reset does not change the
device serial number.

## Configuration ownership after assignment

Once Host Admin assigns an inventory device to a Tenant, connection and
runtime configuration belongs to that Tenant. The Tenant role needs the
`TENANT_DEVICE_CONFIGURATION` module permissions appropriate to the action:
Add, View, Update, and Delete. With those permissions, it can manage the
transport, server values, heartbeat, feature toggles, HTTPS gateway rotation,
runtime command settings, and reboot.

Host users are deliberately denied configuration read, create, update, delete,
runtime-configuration, and reboot endpoints. Their device responsibilities are
inventory/assignment plus issuing a bootstrap or HTTPS gateway URL. This avoids
exposing a Tenant's operational configuration to Host users.

For a normal HTTPS gateway URL, Host calls the existing rotate endpoint with
the opaque `tenantDeviceId` returned by its assigned-device list; it does not
need or receive a configuration ID. A Tenant calls that same endpoint with its
opaque `tenantDeviceConfigurationId` from the configuration screen. Exactly
one identifier is accepted per request.

## Flow A — existing assigned device or factory-reset device

Use this flow when `TenantDevice` and `TenantDeviceConfiguration` already
exist. This is the normal recovery flow after a physical device is reset.

### 1. Confirm configuration

Open the Device Connectivity / Tenant Device Configuration record. Confirm all
values in **Required production configuration** above. The physical device
heartbeat should match `HeartbeatIntervalSeconds` in this record.

### 2. Generate a fresh normal gateway URL

```http
POST /api/TenantDeviceConfiguration/rotate-https-ingress-token
Authorization: Bearer {authorized-user-token}
Content-Type: application/json
```

```json
{
  "tenantDeviceConfigurationId": "{configuration-id-token}",
  "tenantId": "{selected-tenant-id}",
  "moduleId": 49,
  "operationId": 2
}
```

Host URL issuance uses the same endpoint, but sends the device token instead:

```json
{
  "tenantDeviceId": "{tenant-device-id-token}",
  "tenantId": "{selected-tenant-id}",
  "moduleId": 49,
  "operationId": 2
}
```

`49` and `2` are the currently observed **Device Connectivity / Update** IDs
in the active Render environment. Do not assume these numbers apply to a
different database; obtain the mapping from its Module Operations API/UI.

Success returns `data.gatewayUrl` exactly once:

```text
https://skolenza-api.onrender.com/device-gateway/{opaque-token}
```

Security rules:

- Copy the full URL immediately.
- Never place it in tickets, source code, logs, browser storage, or a shared
  screenshot. The token is a bearer secret.
- If it is exposed or lost, rotate again and install the new URL. The old URL
  stops working.

### 3. Technician physical-device setup

First connect Ethernet/Wi-Fi with working Internet and DNS. Then open:

```text
Comm set → Server
```

| Device field | Value |
| --- | --- |
| Server Req | `Yes` |
| Use domainNm | `Yes` |
| DomainNm | Paste the **complete** `gatewayUrl` from step 2; keep `https://`, path, and token; no spaces/newlines. |
| Server IP | `0.0.0.0` / unused while domain name is selected |
| SerPortNo | `443` |
| Heartbeat | Match `HeartbeatIntervalSeconds` saved in Skolenza |
| Server approval | `No`, unless a site-specific policy requires otherwise |

Save/apply. A restart normally is not required. If no activity appears after
two heartbeat periods, reboot once and continue with verification.

### 4. Verify the device connection

Wait for at least two heartbeat periods and allow for public-service cold
start. Then read the configuration from the **production API**, not from an
unrelated local database:

```http
GET /api/TenantDeviceConfiguration/get-by-id/{configurationId}
Authorization: Bearer {authorized-user-token}
```

For the currently observed Render permission mapping:

```text
https://skolenza-api.onrender.com/api/TenantDeviceConfiguration/get-by-id/{configuration-id-token}?tenantId={selected-tenant-id}&moduleId=49&operationId=4
```

Check response fields:

```text
lastHeartbeatDateTime
lastSuccessfulConnectionDateTime
lastConnectionError
```

Successful recent heartbeat timestamps and an empty/null connection error mean
the HTTPS setup is complete.

Render Logs may be searched with `device-gateway` or `POST`, but are not the
source of truth: the gateway deliberately avoids logging the bearer URL/token.
Use the configuration health fields above for confirmation.

Tenant login does not rotate or otherwise change the physical device URL. Do
not generate another gateway URL unless the existing one has been exposed or
is intentionally being replaced. The technician keeps the same complete URL
in the device while verification takes place.

## Flow B — first-time unassigned device bootstrap

Use this only before assigning the physical device to a Tenant.

### 1. Inventory precondition

The DeviceMaster/Inventory record must be:

```text
Active          = Yes
Supports HTTPS  = Yes
Occupied        = No
Serial number   = physical device serial
```

Do not create/assign a `TenantDevice` or `TenantDeviceConfiguration` first.
If the device is already occupied, the bootstrap endpoint correctly rejects it.
Do not manually alter only the occupied flag; use the normal unassign/delete
workflow if an unused test record must be removed.

### 2. Host Admin issues temporary URL

```http
POST /api/TenantDeviceConfiguration/issue-bootstrap-url
Authorization: Bearer {host-user-token}
Content-Type: application/json
```

```json
{
  "deviceMasterId": 1,
  "lifetimeMinutes": 120,
  "moduleId": 48,
  "operationId": 22
}
```

In the current Render environment, `48` / `22` represents **Device
Provisioning / Create**. Re-check in another environment rather than
hard-code it in a generic frontend.

The response supplies `deviceSerialNumber`, `initialGatewayUrl`, a
`heartbeatIntervalSeconds` value (currently 20), and `expiresDateTime`.

The URL has this form:

```text
https://skolenza-api.onrender.com/api/initial/{serial}/{opaque-token}
```

It is short-lived and shown once.

### 3. Technician enters temporary URL

Use the same **Comm set → Server** instructions as Flow A, except:

```text
DomainNm  = complete initialGatewayUrl
Heartbeat = value returned by the bootstrap API (currently 20)
```

Set port `443`, save, and wait for the first device poll.

### 4. Complete normal onboarding

After the bootstrap poll:

1. Create and assign the `TenantDevice` to its Tenant location.
2. Create the HTTPS `TenantDeviceConfiguration` using the required production
   settings.
3. Apply approved runtime configuration through the typed API/UI.
4. Skolenza queues a protected `setdevinfo` response that gives the device its
   normal permanent `/device-gateway/{token}` address.
5. Verify the normal gateway heartbeat using the configuration health fields.

## Permission ID quick reference — current Render deployment

Use only the permission pair appropriate to the operation. `moduleId` and
`operationId` go in the body for POST requests and query string for GET
requests.

| Operation | Module | `moduleId` | `operationId` |
| --- | --- | ---: | ---: |
| Read configuration / health | Device Connectivity / View | 49 | 4 |
| Update configuration | Device Connectivity / Update | 49 | 2 |
| Rotate normal HTTPS URL | Device Connectivity / Update | 49 | 2 |
| Apply runtime config or reboot | Device Connectivity / Update | 49 | 2 |
| Create configuration | Device Connectivity / Add | 49 | 1 |
| Delete configuration | Device Connectivity / Delete | 49 | 3 |
| Issue initial bootstrap URL | Device Provisioning / Create | 48 | 22 |

## Troubleshooting sequence

1. **`401 Unauthorized`** — renew/login with the correct Host or Tenant
   Bearer token.
2. **`400 The request is invalid` on protected GET** — add the correct
   `moduleId` and `operationId` query parameters.
3. **Bootstrap says active/unassigned device required** — device is already
   occupied; use Flow A, or intentionally unassign the empty test record
   before using Flow B.
4. **No heartbeat after two intervals** — compare complete DomainNm value,
   port `443`, device DNS/Internet, and heartbeat value. Do not rotate the URL
   repeatedly while diagnosing.
5. **Production health record not found after a locally generated URL** —
   verify that local API and Render API use the same production database. A
   token generated into a different database cannot authenticate at Render.

## Current verification checkpoint

The current production configuration read has confirmed:

```text
CommandTransport         = 4 (HTTPS)
ServerHost               = skolenza-api.onrender.com
ServerUrl                = https://skolenza-api.onrender.com
ServerPath               = /device-gateway
ServerPort               = 443
HeartbeatIntervalSeconds = 15
```

Next live check: set/confirm the physical device heartbeat at `15`, then
refresh the production configuration after approximately two minutes and
inspect the three health fields in **Flow A, step 4**.
