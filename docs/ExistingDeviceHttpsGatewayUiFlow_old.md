# Existing device — HTTPS gateway URL UI flow

## Purpose

This is the **technician recovery** UI contract for an already assigned and
configured biometric device that must be manually pointed to a fresh normal
HTTPS gateway. It is not the first-time/unassigned-device bootstrap flow.

For a live device that is already polling Skolenza, do not use this immediate
rotation flow: it invalidates the old URL. Use the seamless remote replacement
flow in [TenantDeviceRuntimeSettingsUiHandoff.md](TenantDeviceRuntimeSettingsUiHandoff.md),
which keeps the old URL valid until the device confirms the new one.

The UI has two technician-facing steps:

1. An authorized Skolenza user generates a replacement HTTPS gateway URL for
   the existing `TenantDeviceConfiguration`.
2. A technician copies that complete one-time URL into the physical device's
   **Comm set → Server** screen and verifies that the device starts polling
   Skolenza.

The generated URL is a **bearer secret**. Treat it like a password.

## Where to put the UI

Add a **Generate / rotate HTTPS gateway URL** action to the existing Tenant
Device Configuration detail/edit screen. Do not put it on the Device Master
catalogue or on the initial-device-provisioning screen.

### Privacy boundary

Device Master catalogue fields are Host-internal inventory data. A Tenant
screen must use `deviceCode` and `deviceName` only; it must not render or
otherwise expose `deviceMasterName`, `deviceMasterModelNo`, or
`deviceMasterSNo`, even if a legacy response contains them. Host Admin screens
may display those internal fields where they are needed for support.

The same boundary applies to identifiers: after inventory assignment, use the
opaque `id` and `tenantDeviceId` values returned by the API. Do not expose,
store, route with, or send a numeric TenantDevice or configuration database ID.
Only the initial Host inventory selection uses the raw `DeviceMasterId`.

For an existing configuration, the UI obtains its opaque configuration `id`
from the configuration list/detail response and uses that exact token for
`get-by-id`, update, delete, and gateway rotation. Likewise, the opaque device
`id` returned by the Tenant Device API is the value supplied as
`tenantDeviceId` when creating a configuration or submitting a runtime action.

Suggested button label:

```text
Generate HTTPS device URL
```

When the device already has a URL, a clearer destructive label is:

```text
Rotate HTTPS device URL
```

The button is available to an authorized Tenant device administrator. A Host
user may also issue the gateway URL as part of device provisioning by sending
the assigned-device token (`tenantDeviceId`), but Host users do not have access
to the Tenant's configuration read/edit/runtime screens after assignment.

Only show the action when all of the following are true:

- the user has the `TENANT_DEVICE_CONFIGURATION` operation permission;
- the configuration belongs to the currently selected Tenant;
- the device model supports HTTPS polling;
- `commandTransport` is `4` (**HTTPS**);
- `serverUrl` is an absolute `https://` URL;
- `serverPath` is exactly `/device-gateway` (a trailing slash is accepted by
  the API); and
- `serverPort` is `443` when a port is stored.

The API independently validates all of these conditions. The UI check is only
to prevent an avoidable click/error.

## Step 1 — generate the URL from the UI

### Confirmation dialog

Show this before the API call because a new URL immediately makes the previous
device URL unusable.

```text
Rotate HTTPS device URL?

The device will disconnect until a technician installs the newly generated URL
on the physical device. The new URL is shown once only and must not be shared.

[Cancel] [Generate URL]
```

### API request

```http
POST /api/TenantDeviceConfiguration/rotate-https-ingress-token
Authorization: Bearer {current-user-access-token}
Content-Type: application/json
```

The frontend must use the normal API environment base URL. It must not call a
device LAN IP or the returned gateway URL itself.

Request shape:

```json
{
  "tenantDeviceConfigurationId": "{configuration-id-token}",
  "tenantId": "{selected-tenant-id}",
  "moduleId": 123,
  "operationId": 456
}
```

| Field | UI source / rule |
| --- | --- |
| `tenantDeviceConfigurationId` | The opaque `id` token of the configuration record currently open in the screen. Never parse it as a number. |
| `tenantId` | Selected Host Tenant context. For a Tenant user, use the normal request scope behaviour already used by the configuration APIs. |
| `moduleId` | Current user's permitted module ID for `TENANT_DEVICE_CONFIGURATION`; never hard-code `123`. |
| `operationId` | The permitted Generate/Update operation ID; never hard-code `456`. |

Successful response:

```json
{
  "isSucceeded": true,
  "message": "HTTPS device gateway URL generated. It will not be shown again.",
  "data": {
    "gatewayUrl": "https://{public-skolenza-api}/device-gateway/{opaque-token}"
  },
  "errors": []
}
```

### One-time URL dialog

On a successful response, show a modal immediately. It must include:

- a selectable read-only field containing `data.gatewayUrl`;
- a **Copy URL** button using the browser clipboard API;
- the exact physical-device settings in the next section; and
- a warning that the URL cannot be retrieved from the API later.

Suggested modal content:

```text
HTTPS device URL generated

Copy the complete URL now. It is shown once only. After closing this dialog,
generate a new URL if another copy is required.

[ complete gateway URL                         ] [Copy URL]

Next: on the device, open Comm set → Server and paste the copied value into
DomainNm. Set port 443 and save.
```

Security requirements:

- Do not put `gatewayUrl` in a list/detail GET response, Redux/NgRx persisted
  state, local/session storage, telemetry, analytics, support tickets, or
  browser logs.
- Keep it only in component memory until the dialog closes or navigation
  occurs; then clear it.
- Do not write it to server logs. The server stores only a hash of the token.
- Do not mask the URL in the copy field: the technician needs the exact value.
  Prevent accidental screen sharing and provide the warning instead.
- If the user loses the URL, they must rotate it again. That invalidates the
  previously issued URL.

## Step 2 — physical device instructions shown by the UI

Show this checklist in the one-time dialog, with a print/copy option if useful.
The technician must paste the **entire** generated value, including `https://`,
`/device-gateway/`, and the opaque token. Do not add spaces or line breaks.

| Device screen: `Comm set → Server` | Required value |
| --- | --- |
| Server Req | `Yes` |
| Use domainNm | `Yes` |
| DomainNm | Paste the complete `gatewayUrl` from Step 1 |
| Server IP | `0.0.0.0` / leave unused when DomainNm is enabled |
| SerPortNo | `443` |
| Heartbeat | The saved configuration heartbeat. Use a value from `10` to `3600` seconds; the current field value must be copied to the instruction. |
| Server approval | `No` unless the particular device installation explicitly requires it |

Then the technician saves/applies the settings and keeps the device online.
No MQTTS host, CA certificate, MQTT username, or MQTT topic is needed for this
HTTPS flow.

## Verification state in the UI

The physical device makes outbound `POST` requests to:

```text
https://{public-skolenza-api}/device-gateway/{opaque-token}
```

The frontend must not make this call; only the physical device does. Refresh
the existing device-configuration health data after the device's configured
heartbeat period (allow extra time when the public service is cold-starting).

Show a small status panel with the existing fields:

- `lastHeartbeatDateTime`
- `lastSuccessfulConnectionDateTime`
- `lastFailedConnectionDateTime`
- `lastConnectionError`

Recommended states:

| State | UI message |
| --- | --- |
| Waiting | “URL generated. Waiting for the device to poll the HTTPS gateway.” |
| Connected | “HTTPS gateway activity received at {timestamp}.” |
| No activity after two heartbeat intervals | “No HTTPS poll received. Recheck the complete DomainNm URL, DNS, Internet access, port 443, and device Server settings.” |
| Failure reported | Show the non-secret error and a “Rotate URL again” recovery action. |

## API error handling

| Response | UI action |
| --- | --- |
| `401 Unauthorized` | Refresh/re-authenticate the user; do not retry silently. |
| `403 Forbidden` | Hide/disable the action for this user and show “You do not have permission to manage this device connection.” |
| Validation error: HTTPS polling not configured | Send the user to the configuration form and require HTTPS transport, public HTTPS server URL, `/device-gateway`, port `443`, and heartbeat `10–3600` before retrying. |
| Validation error: model does not support HTTPS polling | Show the device model capability message; do not offer this action. |
| `404` configuration not found | Refresh the configuration list and return to the list page. |
| Network/API failure | Leave the old device configuration UI unchanged. The operator may retry; do not pretend that the device is connected. |

## Explicitly out of scope

- Do not use `POST /api/TenantDeviceConfiguration/issue-bootstrap-url` for an
  existing assigned device. That endpoint is only for an **unassigned** device
  during initial provisioning.
- Do not expose raw vendor commands, device local-IP controls, or device local
  WebServer passwords in this screen.
- Do not use `http://` or port `8883` here. This UI flow is HTTPS over port
  `443`; it is separate from MQTT/MQTTS.
