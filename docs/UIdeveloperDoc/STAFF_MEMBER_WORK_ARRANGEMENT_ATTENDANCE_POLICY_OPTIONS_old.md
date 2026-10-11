# Employee Work Arrangement: Attendance Policy Options API

## Purpose

The Add/Edit Work Arrangement screen needs an Attendance policy dropdown. This endpoint reads the authenticated Tenant's generic Policy Framework and returns the exact Published policy version effective on the Work Arrangement start date.

The lookup does not use seeded numeric category IDs or tenant-generated Policy Type IDs. It follows the relationship from the stable seeded `PolicyCategory.CategoryCode = ATTENDANCE` value through `PolicyType`, `Policy`, and `PolicyVersion`.

## Authentication and permission

- Bearer token: required.
- Trusted `TenantId` and logged-in employee: resolved from the token by the existing tenant-request validation flow.
- `ModuleId` and `OperationId`: not accepted and not checked for this read-only dropdown endpoint, per the approved requirement.
- Missing/invalid authentication returns HTTP `401` through the standard middleware response.

The existing Work Arrangement Create/Update/Delete APIs retain their existing permission and employee-data-access behavior.

## Route

```http
GET /api/EmployeeWorkArrangement/attendance-policy-options
```

### Query parameters

| Field | Type | Required | Rule |
| --- | --- | --- | --- |
| `effectiveOn` | `date` (`yyyy-MM-dd`) | No | UI sends Work Arrangement `EffectiveFrom` automatically. If omitted, API uses the server's current UTC date. |
| `search` | `string` | No | Case-insensitive partial match against Policy name/code and Policy Type name/code. Leading/trailing spaces are ignored. |

Copyable request:

```http
GET /api/EmployeeWorkArrangement/attendance-policy-options?effectiveOn=2026-10-01&search=hybrid
Authorization: Bearer <access-token>
```

There is no request JSON, FormData, upload, polling, retry, cancellation, Excel, or CSV contract for this read-only GET.

## UI call order

1. Open Add/Edit Work Arrangement.
2. If the form has an `Effective From` value, call this endpoint with that date automatically.
3. If the form has no date yet, call without `effectiveOn`; the API uses today's UTC date.
4. Never ask the user to remember or manually type the API lookup date.
5. Render `displayName`; preserve both `policyId` and `policyVersionId` in client state.
6. Re-call the endpoint when `Effective From` changes. Clear a previously selected option if it is absent from the new response.
7. If `data` is empty, show the API message and prevent a policy-dependent submit.

Recommended empty-state text returned by the API:

```text
No published attendance policy is available for the selected effective date. Please create and publish an Attendance policy first.
```

## Server-side selection rules

The endpoint applies all of these rules:

- authenticated `TenantId` on `PolicyType`, `Policy`, and `PolicyVersion`;
- `PolicyCategory.CategoryCode = ATTENDANCE` and category active;
- Policy Type active and not soft deleted;
- Policy active and not soft deleted;
- Policy Version active;
- Policy Status active and `StatusCode = PUBLISHED`;
- `PolicyVersion.EffectiveFrom <= effectiveOn`;
- `PolicyVersion.EffectiveTo` is null or `>= effectiveOn`.

If overlapping Published versions of one Policy match the date, the highest `VersionNumber`, then highest `PolicyVersionId`, wins deterministically. Draft, Under Review, Approved, Archived, inactive, deleted, other-tenant, other-category, expired and not-yet-effective records are excluded.

`IsCurrent` is deliberately not the sole date filter. A historical or future Work Arrangement must resolve the Published version valid on its own start date.

## Success response

```json
{
  "isSucceeded": true,
  "message": "Attendance policy options retrieved successfully.",
  "data": [
    {
      "policyId": 105,
      "policyVersionId": 318,
      "policyCode": "INDIA_HYBRID_ATTENDANCE",
      "policyName": "India Hybrid Attendance Policy",
      "policyTypeId": 9,
      "policyTypeCode": "HYBRID_ATTENDANCE",
      "policyTypeName": "Hybrid Attendance",
      "versionNumber": 2,
      "effectiveFrom": "2026-07-01",
      "effectiveTo": null,
      "displayName": "India Hybrid Attendance Policy - v2"
    }
  ],
  "errors": []
}
```

## No configured/effective Attendance policy

HTTP `200`:

```json
{
  "isSucceeded": true,
  "message": "No published attendance policy is available for the selected effective date. Please create and publish an Attendance policy first.",
  "data": [],
  "errors": []
}
```

## Validation and authentication errors

An omitted `effectiveOn` is valid and uses today's UTC date. A malformed supplied date uses the standard model-validation error path:

```json
{
  "isSucceeded": false,
  "message": "The request is invalid.",
  "data": null,
  "errorCode": "VALIDATION_ERROR"
}
```

Missing token was locally observed as HTTP `401`:

```json
{
  "IsSucceeded": false,
  "Message": "The request is not authenticated.",
  "Data": null,
  "Errors": [],
  "ErrorCode": "UNAUTHORIZED"
}
```

## Tables and persistence

Read only:

- `PolicyCategory`
- `PolicyType`
- `Policy`
- `PolicyVersion`
- `PolicyStatus`

The endpoint creates, updates, or deletes no database rows and stores no files.

## Work Arrangement create/update integration

Create and Update now persist the exact generic Policy version selected by the dropdown. Send `policyVersionId`; do not send the legacy `attendancePolicyId`.

Illustrative create payload:

```json
{
  "moduleId": 24,
  "operationId": 1,
  "employeeId": "<encoded-employee-id>",
  "policyVersionId": 318,
  "primaryTenantLocationId": 5,
  "workMode": 3,
  "hybridType": 2,
  "minimumOfficeDaysPerWeek": 3,
  "minimumOfficeDaysPerMonth": 12,
  "maximumWFHDaysPerMonth": 2,
  "effectiveFrom": "2026-09-24",
  "effectiveTo": null,
  "isActive": true
}
```

The numeric permission IDs above are illustrative. Resolve them from the authenticated menu/permission flow.

Before saving, the API checks that `policyVersionId` belongs to the authenticated Tenant and resolves through an active, non-deleted Attendance Policy Type and Policy to an active `PUBLISHED` version whose effective window contains `effectiveFrom`. A missing, Draft, expired, future, inactive, deleted, wrong-category, or other-tenant version returns the standard validation response with:

```json
{
  "isSucceeded": false,
  "message": "Select a published Attendance policy version that is effective on the Work Arrangement start date.",
  "data": null,
  "errors": [],
  "errorCode": "VALIDATION_ERROR"
}
```

`hybridType` is mandatory only when `workMode` is Hybrid. For non-Hybrid modes, send `hybridType: null`.

The schema migration is additive:

- new writes use nullable `EmployeeWorkArrangement.PolicyVersionId` with a foreign key to `PolicyVersion`;
- legacy `AttendancePolicyId` becomes nullable and remains for historical compatibility;
- no guessed automatic backfill is performed between the legacy and generic policy systems.

Apply `database-scripts/AddPolicyVersionToEmployeeWorkArrangement.sql` before deploying the updated API.

## Verification status — 2026-09-24

- API project build: PASS, 0 errors; existing repository warnings remain.
- Employee Work Arrangement focused backend tests: PASS, 9/9.
- Angular form schema tests: PASS, 9/9.
- Angular production build: PASS; existing bundle-budget warnings remain.
- Local HTTP without token: PASS, HTTP 401 with standard `UNAUTHORIZED` response.
- Additive DB migration: PASS on the configured target; nullable bigint column, FK, index and nullable legacy column verified.
- Authenticated create/read-back: not run because matching API/UI builds have not yet been deployed.
- Deployed verification: not run.

Scenario evidence: [initial implementation — 2026-09-23](../testing/employee-work-arrangement/attendance-policy-options/2026-09-23.md); [permission-pipeline regression — 2026-09-24](../testing/employee-work-arrangement/attendance-policy-options/2026-09-24.md); [PolicyVersion persistence — 2026-09-24](../testing/employee-work-arrangement/policy-version-persistence/2026-09-24.md).
