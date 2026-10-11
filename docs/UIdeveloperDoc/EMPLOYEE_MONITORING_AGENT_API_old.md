# Employee monitoring agent API handoff

**Date:** 3 October 2026  
**Implementation status:** backend and Windows agent source implemented; focused local tests 10/10 pass; database script NOT APPLIED; authenticated HTTP, installed-agent and deployed acceptance PENDING. Angular was not authorized and was not changed.

## Behavior and security boundary

The monitoring agent is separate from biometric `EmployeeDeviceEnrollment`. Tenant administrators use the authenticated `EMP_DEVICES` leaf-module permission flow. The UI must dynamically resolve current `ModuleId` and `OperationId` through the existing authenticated menu/permission response; numeric examples must never be hardcoded.

Installed-agent runtime routes do not use an employee JWT. They require the opaque `X-Agent-Credential` issued once at registration. The server hashes this credential and derives TenantId and EmployeeId from the active enrollment. Runtime payload Tenant/Employee IDs are neither accepted nor trusted.

## Administration sequence

### 1. Configure Tenant policy

`POST /api/Employee/Monitoring/policy` with bearer JWT and resolved `EMP_DEVICES` create/update operation.

```json
{
  "moduleId": 0,
  "operationId": 0,
  "minimumCaptureIntervalSeconds": 300,
  "maximumCaptureIntervalSeconds": 600,
  "heartbeatIntervalSeconds": 60,
  "delayedAfterSeconds": 180,
  "unreachableAfterSeconds": 600,
  "offlineRetentionDays": 3,
  "maximumOfflineBytes": 2147483648,
  "imageQuality": 70,
  "captureAllMonitors": true
}
```

Validation: capture minimum is at least 60 seconds; maximum is not below minimum; heartbeat is at least 15 seconds; delayed is greater than heartbeat; unreachable is greater than delayed; retention/bytes are positive; quality is 1–100.

### 2. Register an installed agent

`POST /api/Employee/Monitoring/agent/register` with bearer JWT and resolved `EMP_DEVICES` create operation.

```json
{
  "moduleId": 0,
  "operationId": 0,
  "employeeId": "tenant-encoded-employee-id",
  "deviceName": "EMP-LAPTOP-01"
}
```

Representative success data:

```json
{
  "agentInstanceId": "019d0000-0000-7000-8000-000000000000",
  "agentCredential": "returned-only-once",
  "policy": {
    "minimumCaptureIntervalSeconds": 300,
    "maximumCaptureIntervalSeconds": 600,
    "heartbeatIntervalSeconds": 60,
    "delayedAfterSeconds": 180,
    "unreachableAfterSeconds": 600,
    "offlineRetentionDays": 3,
    "maximumOfflineBytes": 2147483648,
    "imageQuality": 70,
    "captureAllMonitors": true
  }
}
```

Never log or redisplay `agentCredential`. If lost, a future revoke/re-enroll API is required; credential recovery is deliberately unsupported.

### 3. Show connectivity status

`GET /api/Employee/Monitoring/agent/status?moduleId=...&operationId=...` with bearer JWT and resolved `EMP_DEVICES` view operation.

Statuses: `NeverConnected`, `Online`, `UploadFailure`, `Delayed`, `Unreachable`, `Inactive`. `Unreachable` means the API has not received a heartbeat; it cannot distinguish PC-off, network-down and stopped agent.

## Runtime API

All calls send `X-Agent-Credential`.

- `GET /api/Employee/Monitoring/runtime/configuration` returns current policy.
- `POST /api/Employee/Monitoring/runtime/heartbeat` body:

```json
{
  "agentVersion": "1.0.0",
  "pendingCaptureCount": 4,
  "lastCaptureDateTime": "2026-10-03T10:15:00Z",
  "lastSuccessfulUploadDateTime": "2026-10-03T10:10:00Z",
  "lastErrorCode": "UPLOAD_FAILED"
}
```

- `POST /api/Employee/Monitoring/runtime/captures` is `multipart/form-data`:

```text
captureId: UUIDv7 generated once and retained across retries
capturedAtUtc: UTC timestamp
monitorNumber: zero-based monitor index
checksumSha256: uppercase/lowercase hexadecimal SHA-256
screenshot: image/jpeg or image/webp
```

The `(MonitoringAgentId, CaptureId)` unique key makes retries idempotent. Server time-window validation uses the Tenant policy's offline retention. Server storage keys are generated from authenticated Tenant/Employee identity. The database stores metadata only.

Representative errors use the existing Skolenza error envelope: `401` invalid/inactive agent credential, `403` denied admin permission, validation failure for invalid policy/file/type/time/checksum/employee identifier, and conflict/database errors through centralized middleware.

## Persistence and deployment

Apply `database-scripts/AddEmployeeMonitoringAgent.sql` only to an approved target after backup/change review. Tables: `EmployeeMonitoringPolicy`, `EmployeeMonitoringAgent`, `EmployeeScreenCapture`. Binary files use the configured `EmployeeMonitoringStorage` provider. Current provider is filesystem for development; production durable object storage remains PENDING.

No polling UI was implemented. A future admin screen may poll the status endpoint at a UI-approved interval. Screenshot list/view/download/delete APIs, policy work-hour scheduling, agent revoke/credential rotation, signed installer, code signing, silent enterprise deployment and production S3-compatible storage are PENDING and must not be represented as complete.

## Tested versus deployed

- Solution build: PASS locally.
- Focused `EmployeeMonitoring` tests: PASS 10/10.
- Locked backend regressions: recorded in the linked scenario report.
- Local database script application: NOT RUN.
- Real agent capture/upload with authenticated Tenant data: NOT RUN.
- Deployed API/agent: NOT DEPLOYED.

See [scenario report](../testing/employee/monitoring-agent/2026-10-03.md).
