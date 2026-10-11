# Tenant Email Template API

## Purpose

Tenant administrators manage their own copy of the central email-template catalogue. Every read and write is isolated by the authenticated token's `TenantId`; clients never send `TenantId`.

## Authentication and permission discovery

- Bearer token is required on every route.
- Resolve the current `TENANT_EMAIL_TEMPLATE` module and its operations through the existing authenticated menu/permission response.
- Send the resolved `moduleId` and matching `operationId`. Numeric IDs shown in examples are illustrative.
- The API verifies module code `TENANT_EMAIL_TEMPLATE` and the existing Tenant employee permission function.

Supported mappings are View, Add, Update and Delete. `update-status` uses Update permission.

## Routes

| Method | Route | Operation |
| --- | --- | --- |
| POST | `/api/TenantEmailTemplate/create` | Add |
| GET | `/api/TenantEmailTemplate/get-all` | View |
| GET | `/api/TenantEmailTemplate/get-by-id/{id}` | View |
| POST | `/api/TenantEmailTemplate/update` | Update |
| POST | `/api/TenantEmailTemplate/update-status` | Update |
| POST | `/api/TenantEmailTemplate/sync` | Add |
| DELETE | `/api/TenantEmailTemplate/delete/{id}` | Delete |

## Create JSON

```json
{
  "templateName": "Employee Welcome",
  "templateCode": "WELCOME_EMAIL",
  "subject": "Welcome to the organization",
  "body": "<p>Hello {{EmployeeName}}</p>",
  "fromEmail": "hr@example.com",
  "fromName": "HR Team",
  "ccEmail": null,
  "bccEmail": null,
  "category": "Employee",
  "languageCode": "en",
  "isActive": true,
  "permissionRequest": {
    "moduleId": 119,
    "operationId": 1
  }
}
```

Mandatory fields are `templateName`, `templateCode`, `subject`, `body`, and `permissionRequest`. `templateCode` must come from the existing constant-backed template-code options API; free-text codes are rejected by both Host and Tenant template CRUD. Supported values are `WELCOME_EMAIL`, `FORGOT_PASSWORD`, `BIRTHDAY_WISH`, `LEAVE_APPROVAL`, and `ACCOUNT_VERIFICATION`. The normalized code must also be unique inside the authenticated Tenant. Email fields are validated when supplied.

## List query

```http
GET /api/TenantEmailTemplate/get-all?PageNumber=1&PageSize=10&Search=welcome&Category=Employee&LanguageCode=en&IsActive=true&ModuleId=119&OperationId=4
Authorization: Bearer <access-token>
```

`Search`, `Category`, `LanguageCode`, and `IsActive` are optional. `PageNumber` defaults to 1 and `PageSize` defaults to 10 with a maximum of 100.

## Update JSON

Uses the create fields and adds `id`. Send the Update operation in `permissionRequest`.

```json
{
  "id": 12,
  "templateName": "Employee Welcome",
  "templateCode": "WELCOME_EMAIL",
  "subject": "Welcome",
  "body": "<p>Hello {{EmployeeName}}</p>",
  "category": "Employee",
  "languageCode": "en",
  "isActive": true,
  "permissionRequest": {
    "moduleId": 119,
    "operationId": 2
  }
}
```

## Status JSON

```json
{
  "id": 12,
  "isActive": false,
  "permissionRequest": {
    "moduleId": 119,
    "operationId": 2
  }
}
```

Delete requires the template to be inactive. The delete permission values are query parameters. There is no upload, FormData, Excel/CSV, polling, retry, or cancellation workflow in this API.

## Sync missing templates

```http
POST /api/TenantEmailTemplate/sync
Authorization: Bearer <access-token>
Content-Type: application/json
```

```json
{
  "permissionRequest": {
    "moduleId": 119,
    "operationId": 1
  }
}
```

Sync uses the existing Add permission. It reads active default templates and inserts only codes that are completely absent for the authenticated Tenant. An existing Tenant row blocks copying regardless of whether that row is active or inactive. A previously hard-deleted row is absent, so the next sync creates a fresh active row. Existing Tenant content is never updated or reactivated.

Example response:

```json
{
  "isSucceeded": true,
  "message": "Missing tenant email templates synchronized successfully.",
  "data": {
    "activeDefaultTemplateCount": 5,
    "existingTenantTemplateCount": 4,
    "insertedTemplateCount": 1,
    "insertedTemplateCodes": ["WELCOME_EMAIL"]
  },
  "errors": []
}
```

## Persistence

`TenantEmailTemplate` copies all scalar fields from `EmailTemplate` and adds required `TenantId`. Initial seed copies every existing Host template once to each existing Tenant. The unique index covers `(TenantId, TemplateCode)`. The table has an FK to `Tenant` with cascade delete.

## Mail delivery resolution

Template selection follows SMTP ownership:

| Active usable Tenant SMTP | Active matching Tenant template | SMTP used | Template used |
| --- | --- | --- | --- |
| Yes | Yes | Tenant | Tenant |
| Yes | No | Tenant | Host/default template with the same code |
| No | Either | Host default | Host/default template with the same code |

An inactive Tenant template behaves like a missing template. If the selected Host/default template is also missing or inactive, delivery returns `false` and records a warning; it does not send content from an unrelated template code. `SendTemplatedEmailUsingHostConfigAsync` always uses Host SMTP and the Host/default template because it is intended for pre-trust registration flows.

Queued delivery stores `TenantId`, stable `TemplateCode`, recipient and placeholder JSON. The worker claims one pending row atomically, then applies the same runtime resolution table above. A failed delivery clears its processing lease, records an error and increments `RetryCount`; after five attempts it is no longer selected automatically. Existing queue rows receive `TemplateCode` from their current central `TemplateId` during the database migration.

Queue processing applies to flows that call `QueueTemplatedEmailAsync`. Existing business handlers that call `SendTemplatedEmailAsync` continue to send immediately until each handler is explicitly migrated, because moving them changes when their current success or failure response is returned.

## Status

Implemented and built locally on 2026-09-24. The configured target DB was seeded and verified with module Id 119 under parent 46, four operation mappings, one active plan mapping, one enabled tenant module, four enabled tenant operations, and 10 copied rows. Constant-backed code validation and Tenant-to-Host delivery fallback are implemented locally. Authenticated deployed HTTP CRUD and live SMTP acceptance remain pending.
