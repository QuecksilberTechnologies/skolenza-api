# Secure Device Initial Configuration — Implementation Report

> **Current implementation note:** Tenant runtime configuration has expanded
> beyond the original bootstrap report. For the authoritative typed settings,
> permission, queue-retention, transport-resolution, dropdown, and testing
> contract, see
> [TenantDeviceRuntimeSettingsUiHandoff.md](TenantDeviceRuntimeSettingsUiHandoff.md).
> Older `apply-runtime-configuration` examples below are retained as history;
> new Tenant UI work must use the typed section endpoints in that handoff.

## Scope completed

This change implements a secure, outbound-HTTPS device provisioning flow for the
AiFace-style physical device. The server never opens a connection to a private
device LAN address. The physical device initiates every connection.

The requested serial-only URL was deliberately strengthened. A URL such as
`/api/initial/{SNo}` alone is not safe: anyone who knows or guesses a serial
could impersonate the device. The implemented initial route is:

```text
POST /api/initial/{serial-number}/{opaque-64-hex-token}
```

The raw token is generated once, shown once to the Host operator, and only its
SHA-256 hash is stored in the database. Tenant, location, and device IDs are
never placed in the route.

## Endpoints

| Route | Caller | Purpose |
|---|---|---|
| `POST /api/TenantDeviceConfiguration/issue-bootstrap-url` | Host provisioning role | Creates a temporary initial device URL for an unassigned HTTPS-capable physical device. Default heartbeat is 20 seconds. |
| `POST /api/initial/{serial}/{token}` | Physical device only | Receives device-initiated bootstrap polling. Invalid serial/token/payload gets a non-descriptive 404. |
| `POST /api/TenantDeviceConfiguration/apply-runtime-configuration` | Tenant Admin role | Queues typed `setdevinfo` settings through the device's outbound HTTPS connection. |
| `POST /api/TenantDeviceConfiguration/reboot` | Tenant Admin role | Queues reboot through the outbound HTTPS connection. |
| `POST /device-gateway/{token}` | Physical device only | Existing normal per-Tenant HTTPS gateway. |

The generic `POST /api/device-commands/submit` endpoint now rejects all
device-global administration, configuration, destructive, upgrade, and
file-write commands. Callers therefore cannot bypass the typed Tenant-admin
flow with a raw vendor JSON payload. At present the typed runtime operations
cover heartbeat, volume, local-WebServer disable, optional local password
rotation, and reboot. Further firmware settings must first receive a typed,
field-validated Tenant-admin operation before they are exposed.

## Device controller inventory and exact API contracts

All **user-facing** routes below require the normal Skolenza Bearer access
token. `moduleId` and `operationId` are also mandatory because the API checks
the logged-in user's operation permission. For a Tenant user, `tenantId` is
derived from the JWT; the UI must not trust a tenant ID supplied by the
browser.

### 1. Existing `TenantDeviceConfigurationController` secure actions

Base route: `POST /api/TenantDeviceConfiguration/*`. The secure actions are
added to the existing controller alongside its configuration CRUD endpoints;
they deliberately accept typed fields rather than arbitrary device JSON.

| Route | Who can call it | JSON input | What it does / why |
|---|---|---|---|
| `POST /issue-bootstrap-url` | Host user with `HOST_INITIAL_DEVICE_CONFIGURATION` + Add operation | `{ "deviceMasterId": 1, "lifetimeMinutes": 120, "moduleId": 123, "operationId": 456 }` | Creates a temporary, one-use bootstrap URL for an active, unassigned HTTPS-capable physical device. Returns `initialGatewayUrl`, device serial, fixed first heartbeat `20`, and expiry. The raw secret URL is returned only once. |
| `POST /apply-runtime-configuration` | Tenant Admin with `TENANT_DEVICE_CONFIGURATION` + Update/Edit operation | `{ "tenantDeviceId": 4, "moduleId": 123, "operationId": 456, "currentWebServerPassword": "current-device-password", "heartbeatIntervalSeconds": 20, "volume": 8, "disableLocalWebServer": true, "newWebServerPassword": "optional-new-password", "rebootAfterApply": true }` | Queues protected `setdevinfo`. It sets the device heartbeat, optional volume, disables local WebServer, optionally changes the local password, and—on first apply—switches the device to its normal per-Tenant opaque gateway URL. It returns only command IDs/tracking IDs, never a password or gateway secret. |
| `POST /reboot` | Tenant Admin with `TENANT_DEVICE_CONFIGURATION` + Update/Edit operation | `{ "tenantDeviceId": 4, "moduleId": 123, "operationId": 456 }` | Queues a reboot through the outbound HTTPS device channel. It does not contact the device IP directly. |

`123` and `456` above are examples only. After the seed script runs, Angular
must send the actual Module and Operation IDs supplied by the user's permitted
menu/operation metadata; `0` is rejected.

Validation performed by `apply-runtime-configuration`:

- `tenantDeviceId > 0` and belongs to the logged-in Tenant;
- current local WebServer password: 4–128 characters;
- heartbeat: 10–3,600 seconds; requested default: **20**;
- volume, when sent: 0–15;
- `disableLocalWebServer` must be `true`;
- optional replacement password: 8–128 characters;
- existing Tenant device configuration must be HTTPS, port `443`, and path
  `/device-gateway`.

### 2. Existing `DeviceGatewayController` initial device action

This controller is **not** for Angular or Postman. It is hidden from API
explorer, rate-limited, and is callable only by the device:

```text
POST /api/initial/{deviceSerialNumber}/{opaque64HexToken}
Content-Type: application/json
Body: the vendor's normal device polling JSON, for example { "cmd": "gettime", ... }
```

Its route token is a 256-bit secret, not a location/tenant ID. The API stores
only its SHA-256 hash. Bad content type, body, serial, or token returns an
uninformative `404`. On a valid first poll it gives the device any queued
initial `setdevinfo` command; that command supplies the normal Tenant gateway
URL. The first successful normal gateway poll revokes all bootstrap URLs for
that physical device.

### 3. Existing device controllers whose behaviour was tightened

| Controller / base route | Existing responsibility | Behaviour after this work |
|---|---|---|
| `TenantDeviceConfigurationController` — `/api/TenantDeviceConfiguration` | CRUD for a Tenant device's transport / gateway configuration. Routes are `create`, `get-by-id/{id}`, `get-all`, `update`, `rotate-https-ingress-token`, and `delete/{id}`. | Now Tenant-only and checks the module code `TENANT_DEVICE_CONFIGURATION`; Host users cannot use it to change a tenant runtime configuration. On create/update, UI should save HTTPS settings: server URL `https://skolenza-api.onrender.com`, path `/device-gateway`, port `443`, heartbeat `20`. |
| `TenantDeviceController` — `/api/TenantDevice` | Physical installation record: Tenant, location, DeviceMaster, device code/name and status. | It remains the physical-device registration/assignment controller. Create the Tenant device before its configuration. This is separate from changing device firmware/runtime settings. |
| `DeviceMasterController` — `/api/DeviceMaster` | Host-maintained device-model catalog and capabilities. | Host must mark the model as HTTPS-capable before a bootstrap URL can be generated. Capability flags remain the source of protocol support; this work does not falsely enable MQTT/MQTTS. |
| `DeviceCommandController` — `/api/device-commands/submit` | Legacy generic vendor-command queue endpoint. | It cannot submit device-global configuration, reboot, destructive cleanup, upgrade, or write-file commands. It therefore cannot bypass the typed Tenant-admin routes above. |
| `DeviceGatewayController` — `/device-gateway/{token}` | Normal ongoing outbound HTTPS device polling. | Continues to deliver queued commands and now revokes an initial provisioning route when the device has successfully switched to the normal gateway. |

### Recommended Angular screens

1. **Host / Initial Device Provisioning:** select an unassigned HTTPS-capable
   DeviceMaster, issue and copy the one-time URL, show its expiry. Do not store
   or display it again.
2. **Tenant Admin / Device configuration:** create/read/update the HTTPS
   `TenantDeviceConfiguration`; then show a separate runtime form containing
   heartbeat, volume, local-WebServer disable, optional password rotation, and
   reboot.
3. Do **not** add an Angular screen for device-IP, raw `setdevinfo`,
   `writefile`, MQTT credentials, or arbitrary vendor JSON.

## Operational flow

```text
Host Admin
  -> issues temporary bootstrap URL (20-second heartbeat)
  -> operator enters it in Device > Server > Use domain name = Yes
  -> device POSTs outbound HTTPS to /api/initial/{serial}/{token}

Host Admin
  -> creates/assigns TenantDevice

Tenant Admin
  -> creates HTTPS TenantDeviceConfiguration
     ServerUrl = https://skolenza-api.onrender.com
     ServerPath = /device-gateway
     ServerPort = 443
  -> applies typed runtime settings from Angular

Server
  -> queues encrypted-at-rest setdevinfo command on bootstrap route
  -> device receives normal gateway URL in the command response
  -> device switches to /device-gateway/{new opaque token}
  -> first normal poll revokes every active bootstrap URL for that device
```

For later updates, the device is already on its normal gateway. The same typed
endpoint queues heartbeat, volume, local-WebServer disable, optional new
WebServer password, then optional reboot in strict device order.

## Tenant-admin request contract

`apply-runtime-configuration` accepts only typed settings, not arbitrary vendor
JSON:

```json
{
  "tenantDeviceId": 0,
  "moduleId": 0,
  "operationId": 0,
  "currentWebServerPassword": "enter-current-device-password-in-ui",
  "heartbeatIntervalSeconds": 20,
  "volume": 8,
  "disableLocalWebServer": true,
  "newWebServerPassword": "optional-new-device-password",
  "rebootAfterApply": true
}
```

Validation rules:

- heartbeat: 10 to 3,600 seconds;
- volume: 0 to 15 when supplied;
- local WebServer disable is mandatory for this secure flow;
- current device WebServer password: 4 to 128 characters;
- optional replacement password: 8 to 128 characters;
- target must be an active HTTPS-capable Tenant device with HTTPS / port 443 /
  `/device-gateway` configuration.

The request body is never logged. The command payload is encrypted before it is
stored in `DeviceCommand`, decrypted only immediately before device delivery,
and sensitive JSON fields are redacted from `DeviceMessageLog`.

The optional replacement WebServer password is intentionally **not retained**
after queueing. A later configuration change requires the current password to
be entered again through the authorized Angular UI. This avoids making the
application database a long-term store of a local-device management password.

## Authorization model

The consolidated module seed is `database-scripts/production-seed/04-access-and-host/001-modules-operations-two-host-admins.sql`. It contains:

- `HOST_INITIAL_DEVICE_CONFIGURATION`: issue initial bootstrap URLs only.
- `TENANT_DEVICE_CONFIGURATION`: read/create/update/delete Tenant device
  configuration, rotate its normal gateway URL, apply runtime settings, and
  reboot.
- `HOST_DEVICE_SETUP` and `TENANT_DEVICE_SETUP`: Host device catalogue and
  Tenant physical-device setup, respectively.

### Seeded menu metadata

Module codes are stable permission contracts and are not UI labels. The seed
uses the following administrator-facing names and routes:

| Scope | Module code | UI label | Route | Priority |
|---|---|---|---|---:|
| Host | `HOST_DEFAULT_EMAIL_CONFIG` | Platform Email Defaults | `/app/default-email-config` | 410 |
| Host | `HOST_TENANT_EMAIL_CONFIG` | Tenant Email Administration | `/app/tenants/tenant-email-config` | 420 |
| Host | `HOST_EMAIL_TEMPLATE` | Email Templates | `/app/email-templates` | 430 |
| Host | `HOST_DEVICE_SETUP` | Device Catalogue | `/app/device-masters` | 510 |
| Host | `HOST_INITIAL_DEVICE_CONFIGURATION` | Device Provisioning | `/app/device-masters` (permission only; no duplicate menu item) | 520 |
| Tenant | `TENANT_EMAIL_CONFIG` | Email Settings | `/app/tenant-email-config` | 410 |
| Tenant | `TENANT_DEVICE_SETUP` | Installed Devices | `/app/tenant-devices` | 510 |
| Tenant | `TENANT_DEVICE_CONFIGURATION` | Device Connectivity | `/app/tenant-device-configurations` | 520 |

Priority is ascending within the same scope and parent. The existing Common
menu tree treats a negative priority as terminal, so the existing Sign out
item with priority `-1` remains last. The seed does not alter Common-menu
rows.

Each active Add, View/Read, Update/Edit, and Delete mapping receives
the module's route, the operation icon (falling back to the module icon), an
operation priority of 10/20/30/40, and an action-specific remark. This makes
permission administration readable without changing access grants.

The code verifies the **module code**, not merely a caller-provided module ID.
This prevents a user with permission to some unrelated module from borrowing
that module's operation ID to configure a device.

Do not grant `TENANT_DEVICE_CONFIGURATION` to ordinary Tenant employees. Grant
it only to the Tenant Admin role through existing role-permission management.
Likewise grant the Host module only to approved provisioning roles.

## Structural alignment and duplication removal

The device feature now follows the established Employee feature structure rather
than introducing a parallel application pattern:

- `TenantDeviceConfigurationPermissionBehavior` follows the existing Employee
  module-code → authenticated context → stored-procedure permission flow. It
  uses `HostRuntimePermissionValidator` for the Host bootstrap action and the
  existing Tenant stored procedure for Tenant actions.
- The secure actions are in the existing
  `TenantDeviceConfigurationController`; device-only bootstrap polling is in
  the existing hidden `DeviceGatewayController`. No extra controller remains.
- Commands, handlers, validation, and the runtime DTOs are grouped with the
  existing `TenantDevice` / `TenantDeviceConfiguration` code. The shared
  device-command interface file owns the initial-provisioning contract.
- Normal and bootstrap HTTPS polling use one documented payload parser. Normal
  gateway URL issuance uses one documented validation helper. Token generation,
  hashing, response protection, and vendor payload validation remain centralized
  in the existing device constants/catalogue.
- The previous duplicate controller files, handler files, standalone DTO file,
  and standalone interface file were removed. New and changed device sections
  have XML summaries and `#region` groupings consistent with the surrounding
  feature files.

## Database work

Run these scripts once, in order, after taking a database backup and before
deploying the matching API build:

1. `database-scripts/AddSecureInitialDeviceConfiguration.sql`
2. `database-scripts/production-seed/04-access-and-host/001-modules-operations-two-host-admins.sql`

The first script adds:

- `DeviceCommand.IsSensitivePayload`;
- `DeviceInitialProvisioning`, containing token hashes, expiry, heartbeat,
  audit identity, and bootstrap connection timestamps;
- credential enum support for the existing `DeviceCredential` table.

Neither script was run by this implementation. They are intentionally
idempotent where safe; role assignment remains an explicit administrator
decision.

### SQL execution sheet

Run only the following two files for this device feature, in the stated order:

| Order | SQL file | Database changes |
|---:|---|---|
| 1 | `database-scripts/AddSecureInitialDeviceConfiguration.sql` | Adds `DeviceCommand.IsSensitivePayload`; creates `DeviceInitialProvisioning` (hashed bootstrap secret, expiry, 20-second heartbeat, connection/audit timestamps); expands existing device-credential type validation for future local-WebServer password support. |
| 2 | `database-scripts/production-seed/04-access-and-host/001-modules-operations-two-host-admins.sql` | Consolidated idempotent seed for email, device, employee-password, and dashboard modules. It includes `HOST_DEVICE_SETUP`, `TENANT_DEVICE_SETUP`, `HOST_INITIAL_DEVICE_CONFIGURATION`, and `TENANT_DEVICE_CONFIGURATION`, with active Add/View/Read/Update/Edit/Delete mappings. |

The module rows and operation mappings are **prepared in SQL but not yet run
against RenderDB**. The seed deliberately does **not** grant either module to a
role automatically. After script 2, use the normal role-permission UI/SQL to
grant:

- Host provisioning role: `HOST_INITIAL_DEVICE_CONFIGURATION` → Add;
- Tenant Admin role only: `TENANT_DEVICE_CONFIGURATION` → View/Read,
  Add, Update/Edit, Delete as required.

`RemoveTenantProfileAddress.sql` is a separate earlier Tenant-address change;
it is **not** part of this device deployment and must not be rerun for this
feature.

## Local Postman / LAN security

A public cloud API cannot block a laptop already on the same LAN from calling a
device IP address. The server-side controls implemented here stop **Skolenza
API** bypasses, not arbitrary LAN traffic.

The queued `setdevinfo` command sets `use_webserver` to `0`, requests the
device to disable its local WebServer, and is delivered through HTTPS. Confirm
that behavior on the exact firmware before relying on it. Defense in depth
still requires:

1. put devices on a separate VLAN/management network;
2. firewall the device management port from employee/user subnets;
3. use a strong device-local WebServer password while provisioning;
4. do not expose device IPs with router port forwarding;
5. revoke/reissue an initial URL if it was copied to an unintended party.

`webserver_pwd` is based on the observed device `getdevinfo` field. Verify the
exact firmware's `setdevinfo` acceptance on a non-production device before
using a password rotation in production.

## Code change record

| Area | Change |
|---|---|
| Domain/EF | Added `DeviceInitialProvisioning` entity, DbSet, indexes, and FK mapping. |
| Persistence | Added secure bootstrap service; added protected command payload encryption and audit redaction; added bootstrap polling support. |
| API | Added secure actions to the existing `TenantDeviceConfigurationController` and initial polling to the existing hidden `DeviceGatewayController`. |
| Authorization | Uses the existing Employee-style module-code and stored-procedure permission pipeline. Tenant configuration CRUD is Tenant-only; Host retains only initial bootstrap issuance. |
| Commands | Generic raw device-global administration/configuration commands are blocked. Typed runtime endpoints queue only approved vendor fields. |
| Seed | Added Host and Tenant modules with active CRUD operation mappings. |
| Tests | Added opaque-token validation tests. |

## Verification evidence

- `dotnet build skolenza.api/skolenza.api.csproj --no-restore -clp:ErrorsOnly`
  completed successfully: **0 errors**.
- `dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-restore --filter "FullyQualifiedName~DeviceHttpsGatewaySecurityTests"`
  completed successfully: **5 passed, 0 failed**.

Existing solution warnings (package vulnerability advisories and pre-existing
nullable/XML documentation warnings) remain and are unrelated to this feature.

## Remaining rollout tasks

1. Run the two database scripts and assign the seeded permissions to the
   intended Host provisioning and Tenant Admin roles.
2. Deploy this API build with `DeviceGateway:PublicBaseUrl` set to the public
   HTTPS base URL. The current default is the requested Render URL.
3. Build Angular screens around the three authenticated controller endpoints;
   do not place direct device-IP or raw vendor JSON controls in the UI.
4. Use a test device to verify `setdevinfo` fields, especially
   `use_webserver` and `webserver_pwd`, against firmware `ai806_f06v_v5.16`.
5. Perform an end-to-end test: bootstrap -> assign TenantDevice -> create HTTPS
   configuration -> apply runtime settings -> observe normal gateway heartbeat
   -> confirm direct local WebServer is disabled.

