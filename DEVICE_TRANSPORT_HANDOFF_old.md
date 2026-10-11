# Device transport handoff

`CommandTransport` is the transport-neutral configuration field. The legacy
`MqttTransport` field remains readable for old Angular clients, but new UI code
must send and filter by `CommandTransport`.

| Value | Transport | Adapter status |
| --- | --- | --- |
| 1 | MQTT | Active |
| 2 | MQTTS | Active |
| 3 | HTTP | Reserved for a LAN-only adapter |
| 4 | HTTPS polling | Active |
| 5 | WebSocket | Reserved; do not use for an Internet device |
| 6 | WSS | Reserved for a future secure WebSocket adapter |

## HTTPS configuration flow

1. Create or update the Tenant device configuration with:
   - `CommandTransport: 4`
   - `ServerUrl`: the public Skolenza API base URL, using `https`
   - `ServerPath: /device-gateway`
   - `ServerPort: 443`
   - `HeartbeatIntervalSeconds: 15` (or another value from 10 through 3600)
2. Call `POST /api/TenantDeviceConfiguration/rotate-https-ingress-token` with
   the usual authenticated Host/Tenant access fields and
   `tenantDeviceConfigurationId`.
3. Display `data.gatewayUrl` in a one-time copy dialog. Do not save it in
   browser storage, application state, logs, tickets, or configuration read
   models. It is a bearer secret and will not be returned by a later GET.
4. Configure that complete returned URL in the physical device's Server Domain
   Name field, with Server Request enabled and port 443. Confirm on a test
   device that this firmware preserves the URL path after the host name.

The device then makes `POST` requests to the URL. It sends its normal vendor
JSON, including `sn`. Skolenza validates both the 256-bit route token and the
serial-to-TenantDevice mapping. A valid poll receives either one queued vendor
command or a successful heartbeat acknowledgement.

## Security contract

- The browser and Skolenza API never call `http://<device-lan-ip>/api` for
  normal operations.
- A device request cannot execute a `cmd` supplied in its own payload. Only the
  previously authenticated Skolenza command queue can supply a command in the
  HTTPS response.
- Invalid token, serial, JSON, or content type receives a non-descriptive 404.
- The gateway is rate limited per source IP and accepts at most 128 KiB per
  request.
- Keep the device embedded WebServer on a private management VLAN, firewall its
  port 80 away from normal users, and rotate its local WebServer password. The
  HTTPS gateway does not make the local web API public.

## UI notes

- The existing command submission endpoint stays unchanged. It uses the
  configured transport automatically.
- Do not expose `HttpsIngressTokenHash`; it is server-only.
- MQTTS remains usable with `CommandTransport: 2`. WebSocket/WSS configuration
  values are accepted and persisted, but cannot queue commands until their
  transport adapter is implemented.
