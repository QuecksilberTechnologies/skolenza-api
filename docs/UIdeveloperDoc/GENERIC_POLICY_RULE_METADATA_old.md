# Generic Policy rule metadata and tenant execution contract

## Outcome

Tenant Policy authoring is schema-driven. `GET /api/TenantPolicy/lookups` now returns
`ruleDefinitions` in addition to the existing categories, statuses, rule types, document types and
Attendance location scopes. The UI filters rule types by the selected Policy Type's stable category
code and renders the predefined settings. Users no longer create arbitrary JSON keys.

Authentication and permission behavior is unchanged. The endpoint uses the authenticated Tenant
Policy permission pipeline. UI clients must continue resolving ModuleId and OperationId through the
authenticated menu tree; numeric examples must never be persisted or hard-coded.

## Code-first lookup response

Each rule definition contains:

- `categoryCode` and `ruleTypeCode`: stable values used across environments;
- `isRequired`, `allowMultiple`, `order`;
- setting `code`, label, datatype, required/default/minimum/maximum/help metadata;
- stable option codes and JSON values;
- conditional visibility/required dependencies.

Database IDs remain internal foreign keys. All metadata seed joins resolve
`PolicyCategory.CategoryCode`, `PolicyRuleType.RuleTypeCode`, setting codes and option codes.

Every Policy Type response includes its stable `categoryCode`. After a user selects a Policy Type,
the authoring UI filters rule definitions directly by that code and then intersects the result with
the stable `ruleTypeCode` catalogue. It does not derive the category through environment-specific
numeric IDs and it does not fall back to showing every rule when category metadata is unavailable.
Numeric IDs remain only as internal foreign-key values in create/update payloads.

## Save validation

`POST /api/TenantPolicy` and the Draft update endpoint validate:

1. the selected rule type is allowed for the Policy Type's category;
2. non-repeatable rules are not duplicated;
3. required rule settings are present;
4. unknown setting keys are rejected;
5. JSON values match BOOLEAN/INTEGER/DECIMAL/STRING/CODE datatypes;
6. numeric ranges and enumerated options are valid.

For the Leave `ACCRUAL` rule, `annualEntitlement` is the authoritative yearly total.
`amountPerCycle` is derived as `annualEntitlement / cycleCount`, where MONTHLY = 12,
QUARTERLY = 4 and YEARLY = 1. The Angular field is read-only and recalculates whenever either
input changes. The API independently recomputes the value to six decimal places and rejects a
different client-supplied value. A leave ledger must credit the remaining entitlement in the final
cycle, so rounding never changes the exact annual total.

Example `ruleConfiguration` values:

```json
{
  "annualEntitlement": 20,
  "frequency": "MONTHLY",
  "amountPerCycle": 1.666667,
  "prorateNewJoiner": true
}
```

The persisted `PolicyRule.RuleConfiguration` remains JSONB-compatible JSON. This preserves existing
versions while making new authoring deterministic. Module adapters, such as the typed Attendance
configuration, remain responsible for performing domain calculations.

## Assignment and unassignment

- `POST /api/TenantPolicy/assignments` assigns a Published version to one or more employees.
- `DELETE /api/TenantPolicy/assignments/{assignmentId}` deactivates a manual assignment without
  deleting history.
- `GET /api/TenantPolicy/versions/{versionId}/assignments` lists assignments.
- `GET /api/TenantPolicy/resolve` previews the effective policy for an employee and date.
  Each resolved policy includes its ordered `rules` collection with stable `ruleTypeCode`,
  rule name, order and validated configuration JSON so module adapters can execute the
  returned contract without relying on numeric rule-type IDs.

Assignments use employee/version IDs because they reference tenant transaction rows. Seed/master
selection uses stable codes. The UI already exposes assignment/unassignment in the Policy Version
workspace.

## Employee exceptions and effective resolution

- Exception creation accepts the same tenant-salted encoded employee identifier returned by the
  Employee APIs. UI clients must not convert or expose the internal numeric employee key.
- An approved, active exception inside its effective date range has precedence over a manual
  assignment and ordinary applicability for the same published version.
- A resolved exception returns `resolutionSource: "EXCEPTION"`, the normal ordered rules and an
  `exception` object containing the override JSON and its user-entered reason.
- Direct employee assignment returns `resolutionSource: "MANUAL_ASSIGNMENT"`; applicability
  continues to return `APPLICABILITY`.

## Exact version workspace read

`GET /api/TenantPolicy/{policyId}/versions/{versionId}` returns the exact tenant-owned version named
in the route, including its lifecycle status, rules, applicability and typed Attendance
configuration. Version workspaces must use this route; `GET /api/TenantPolicy/{policyId}` remains
the policy/current-version read used by the definition list and editor.

## Tables and responsibilities

| Table | Responsibility | Seeded |
| --- | --- | --- |
| PolicyCategory | Stable high-level module category | Yes, by code |
| PolicyType | Tenant's named policy type under a category | Tenant workflow |
| PolicyStatus | Version lifecycle catalogue | Yes, by code |
| PolicyRuleType | Stable rule behavior catalogue | Yes, by code |
| PolicyDocumentType | Stable policy document catalogue | Yes, by code |
| Policy | Tenant policy identity | Tenant workflow |
| PolicyVersion | Effective, immutable lifecycle version | Tenant workflow |
| PolicyRule | Version rule and validated configuration JSON | Tenant workflow |
| PolicyApplicability | Include/exclude employee and organizational targeting | Tenant workflow |
| PolicyAssignment | Explicit employee/version binding | Tenant workflow |
| PolicyException | Effective-dated employee override | Tenant workflow |
| PolicyDocument | Version document metadata and storage key | Tenant workflow |
| PolicyApprovalStage | Tenant/category approval route | Tenant workflow |
| PolicyApprovalHistory | Immutable approval decisions | Runtime history |
| PolicyAcknowledgement | Employee view/acknowledgement evidence | Runtime history |
| PolicyChangeAudit | Policy/version/rule audit trail | Runtime history |
| AttendancePolicyVersionConfiguration | Typed Attendance execution configuration | Tenant workflow |
| PolicyCategoryRuleType | Allowed rule types per stable category | Yes, by codes |
| PolicyRuleSettingDefinition | Field schema and validation per rule type | Yes, by codes |
| PolicyRuleSettingOption | Stable select options per setting | Yes, by codes |
| PolicyRuleSettingDependency | Conditional field behavior | Yes, by codes |

## Removal decision

No table was removed in this change. Repository and DbContext references prove that legacy/module
policy tables still have active consumers. Removing them without transaction-data and API migration
would regress Leave, Insurance, Attendance or allowance flows. A table becomes removable only after
its runtime references, foreign keys and retained production rows are all zero or migrated and the
corresponding module regression tests pass.

## Deployment status

- Backend source/build: annual-entitlement post-change verification is recorded in the linked test report.
- Angular production build: annual-entitlement post-change verification is recorded in the linked test report.
- Local and Render PostgreSQL: Accrual annual-entitlement metadata applied and reconciled.
- Render API deployment of the updated backend: not performed by this database migration.
