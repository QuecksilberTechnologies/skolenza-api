# Tenant Policy leave-type targeting

## Behavior

A Leave-category Policy Type such as `ANNUAL_LEAVE` remains the versioned policy container. Its
covered operational Leave Types come from the tenant `LeaveType` master. Entitlement, accrual,
carry-forward, sandwich, limit and approval rules are independently targeted to one or more covered
Leave Types. Applicability rows are also targeted, allowing Maternity/Paternity eligibility without
excluding an employee from Casual or Earned Leave.

## Authentication and permission discovery

All routes use bearer authentication and the existing Tenant Policy permission pipeline. Resolve
`moduleId` and `operationId` dynamically from the authenticated menu; numeric IDs must not be
hard-coded.

## Lookup

`GET /api/TenantPolicy/lookups?moduleId={resolved}&operationId={resolved}` now includes:

```json
{
  "data": {
    "leaveTypes": [
      { "id": 1, "name": "Casual Leave" },
      { "id": 2, "name": "Earned Leave" }
    ]
  }
}
```

## Create/update request

`POST /api/TenantPolicy` and `PUT /api/TenantPolicy/{policyId}/versions/{versionId}` use the same
complete replacement contract:

```json
{
  "moduleId": 0,
  "operationId": 0,
  "policyTypeId": 10,
  "policyCode": "ANNUAL_LEAVE_2026",
  "policyName": "Annual Leave 2026",
  "effectiveFrom": "2026-01-01",
  "changeSummary": "Initial version",
  "leaveTypeIds": [101, 102, 103, 104, 105],
  "rules": [
    {
      "policyRuleTypeId": 2,
      "ruleName": "Casual Leave entitlement",
      "ruleOrder": 1,
      "ruleConfiguration": "{\"quantity\":6,\"unit\":\"DAY\"}",
      "leaveTypeIds": [101]
    },
    {
      "policyRuleTypeId": 4,
      "ruleName": "Casual Leave lapses at year end",
      "ruleOrder": 2,
      "ruleConfiguration": "{\"enabled\":false}",
      "leaveTypeIds": [101]
    },
    {
      "policyRuleTypeId": 3,
      "ruleName": "Earned Leave monthly accrual",
      "ruleOrder": 3,
      "ruleConfiguration": "{\"frequency\":\"MONTHLY\",\"amountPerCycle\":0.5,\"prorateNewJoiner\":true}",
      "leaveTypeIds": [102]
    }
  ],
  "applicability": [
    {
      "applicabilityMode": 1,
      "genderId": 2,
      "priority": 100,
      "effectiveFrom": "2026-01-01",
      "leaveTypeIds": [104]
    }
  ]
}
```

IDs in this example are illustrative. The response returns `leaveTypes`, and every rule and
applicability row returns its `leaveTypeIds`.

## Resolution

`GET /api/TenantPolicy/resolve` accepts optional `leaveTypeId`. When provided, only versions that
cover that Leave Type, matching applicability rows and rules targeted to it are returned. Existing
callers that omit it retain the aggregate policy response.

## Validation and persistence

- Leave policy: at least one covered active tenant Leave Type is mandatory.
- Every Leave rule and applicability row must target covered Leave Types.
- Non-Leave policies reject leave-type targets.
- Multiple rows of the same rule type are supported for Leave policies.
- Persistence tables: `PolicyVersionLeaveType`, `PolicyRuleLeaveType`, and
  `PolicyApplicabilityLeaveType`.
- Clone copies all three mappings. Draft replacement rewrites them transactionally. Approval
  checksum includes them; published content remains immutable.

## Tested/deployed state

- Backend Release build: passed locally.
- Policy contract/schema/metadata tests: 39 passed.
- Angular focused form tests: 7 passed; production build passed.
- Local and Render schema/seed migration: applied and verified on 2026-10-08.
- API/UI binary deployment: pending.

