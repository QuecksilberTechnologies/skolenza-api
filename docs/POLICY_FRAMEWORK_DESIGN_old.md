# Generic Tenant Policy Framework

## Implementation status — 2026-09-16

Persistence, seed catalogue, EF mappings, permission-checked API foundation,
policy type CRUD/status, draft/version operations, rules/applicability, lifecycle,
employee resolution, assignments, exceptions, acknowledgements, document storage,
approval-stage administration/enforcement and audit read APIs are implemented
locally. Assignment, exception and acknowledgement read APIs are also available.
Focused policy and affected regression tests pass 61 with 3 environment skips.
Durable policy bulk import is implemented locally. PostgreSQL API integration
testing and deployed authenticated acceptance remain pending; they are
not recorded as complete.

## Scope and design guarantees

This phase creates the persistence model and permission catalogue. Controllers,
API contracts, background assignment evaluation and UI implementation are the
next phase. The design supports multi-tenant and multi-country policies without
storing an entire policy as an opaque JSON document.

Core guarantees:

- every business row carries `TenantId`;
- published behavior belongs to an immutable `PolicyVersion`;
- geography and organization applicability are explicit foreign keys;
- variable rule values use validated PostgreSQL `jsonb`;
- documents live in object storage while DB stores metadata and checksum;
- assignment, exception, approval, acknowledgement and audit evidence are retained;
- existing attendance, employee work-arrangement and holiday tables remain available;
- the existing durable bulk framework will be reused rather than creating another job engine.

## Tables and stored data

| Table | Responsibility | Representative stored data |
| --- | --- | --- |
| `PolicyCategory` | Shared high-level catalogue | `LEAVE`, `ATTENDANCE`, `TRAVEL`, `INSURANCE`, `CUSTOM` |
| `PolicyStatus` | Version lifecycle | Draft, Under Review, Approved, Published, Suspended, Archived, Rejected |
| `PolicyRuleType` | Valid rule families | Eligibility, Accrual, Sandwich, Limit, Overtime, Reimbursement |
| `PolicyDocumentType` | Attachment classification | Policy Document, Annexure, Legal Circular, Guide, Translation |
| `PolicyType` | Tenant policy-type catalogue | Tenant 78, code `PAID_LEAVE`, category Leave, currency INR |
| `Policy` | Stable policy identity | `IN-MH-PL-2027`, Maharashtra Paid Leave |
| `PolicyVersion` | Effective-dated immutable version | Version 2, Published, 2027-01-01 onward |
| `PolicyRule` | Ordered executable configuration | Monthly accrual and six-day carry-forward JSON objects |
| `PolicyApplicability` | Include/exclude targeting | India + Maharashtra + Mumbai + Permanent employees |
| `PolicyAssignment` | Resolved employee assignment | Employee 145 assigned to version 2 from joining date |
| `PolicyException` | Temporary employee override | Remote attendance allowed for employee 145 for seven days |
| `PolicyDocument` | Object-storage metadata | object key, MIME type, size, SHA-256, language, visibility |
| `PolicyApprovalStage` | Tenant approval configuration | HR review first; Compliance approval second |
| `PolicyApprovalHistory` | Immutable actions | Approved by user 18 at a timestamp with comments |
| `PolicyAcknowledgement` | Employee delivery evidence | Assigned, viewed and acknowledged timestamps |
| `PolicyChangeAudit` | Before/after technical audit | Entity, action, JSON snapshots, actor and correlation ID |

`PolicyRule.RuleConfiguration`, exception override data and audit snapshots are
`jsonb`. Searchable ownership, dates and applicability remain relational columns.

## Prerequisites reused

`Tenant`, `Country`, `State`, `District`, `Locality`, `TenantLocation`,
`EmployeeType`, `Department`, `Designation`, `Employee`, `Gender`, `Role`,
`EmployeeLocationAssignment`, `EmployeeWorkArrangement`, `EmployeeWorkPattern`,
`AttendancePolicy` and `Holiday` remain source masters.

Currency is stored as an ISO-4217 three-character code because the application
already uses the shared currency enum/provider. Time zone comes from
`TenantLocation.TimeZoneId`; policy dates are local calendar dates and event
timestamps are stored with time zone.

## Lifecycle and write sequence

1. Tenant registration creates the tenant and Tenant Admin through the existing flow.
2. Subscription entitlement sync enables the policy modules allowed by the plan
   and grants available operations to the Tenant Admin through the existing permission pipeline.
3. Tenant configures approval stages by category.
4. Tenant creates a `PolicyType`, for example Paid Leave under Leave.
5. Tenant creates one stable `Policy` identity.
6. System creates `PolicyVersion` 1 in Draft status.
7. Rules, applicability rows and documents are saved against that draft version.
8. Submit changes status to Under Review. Active mandatory stages are resolved
   from global and category-specific configuration in `StageOrder`. An approver
   must hold the stage's tenant role; one employee cannot approve a stage twice.
   `MinimumApprovals` must be reached before the next stage begins. A rejection
   closes that approval cycle; resubmission starts counting after that rejection.
9. Completion of every mandatory stage changes status to Approved. If no stage
   is configured, the existing direct approve transition remains available.
   Publish closes the previous current
   version, marks the approved version current and publishes it.
10. The evaluator resolves eligible employees and writes `PolicyAssignment` rows.
11. Published policy delivery creates `PolicyAcknowledgement` rows.
12. A future change creates version 2; version 1 is never overwritten.

## Applicability and precedence

An applicability row may target country, state, district, locality,
TenantLocation, EmployeeType, department, designation, employee, gender, work
arrangement and employment status. `ApplicabilityMode=1` includes; `2` excludes.

Resolution order is deterministic:

1. approved employee exception;
2. explicit employee applicability;
3. TenantLocation or Locality;
4. District, State and Country;
5. EmployeeType;
6. Department and Designation;
7. tenant-wide default.

Within the same level, lower numeric `Priority` wins. Exclusion wins over inclusion
at equal specificity and priority. Manual assignment is evaluated before rules.
Only rows effective on the evaluated local date
participate. A transaction such as leave calculation must persist the applied
`PolicyVersionId` so later policy changes do not rewrite history.

## Rule JSON examples

Leave rules:

```json
{
  "annualEntitlement": 18,
  "unit": "DAY",
  "accrualFrequency": "MONTHLY",
  "carryForwardLimit": 6,
  "sandwich": {
    "enabled": true,
    "includeHoliday": true,
    "includeWeeklyOff": true
  }
}
```

Attendance-channel rules:

```json
{
  "allowedChannels": ["WEB", "MOBILE", "BIOMETRIC"],
  "requireGpsForMobile": true,
  "requireGeoFenceForOffice": true,
  "manualAttendanceRequiresApproval": true
}
```

Travel limit:

```json
{
  "currencyCode": "USD",
  "dailyLimit": 180.00,
  "receiptRequiredAbove": 25.00,
  "allowedTravelClasses": ["ECONOMY", "PREMIUM_ECONOMY"]
}
```

## Document persistence

Binary content is not stored in PostgreSQL. Example metadata:

```json
{
  "storageProvider": "S3",
  "objectKey": "tenant/78/policies/paid-leave/version-2/policy-en.pdf",
  "originalFileName": "Paid-Leave-2027.pdf",
  "contentType": "application/pdf",
  "fileSizeBytes": 248320,
  "checksumSha256": "64-character SHA-256",
  "languageCode": "en",
  "isEmployeeVisible": true
}
```

The object is retained according to tenant retention settings. Soft deletion
hides metadata but does not silently remove evidence used by a published version.

## Covered scenarios

### India, location and EmployeeType

Mumbai permanent employees receive Maharashtra Paid Leave. Hyderabad contractors
receive Telangana Contract Leave. Country and state applicability rows select the
right version without changing employee records.

### USA office of the same tenant

A USA policy targets country USA and a state. Its holiday calendar and currency
are independent from India. The tenant remains the same; TenantId isolation still applies.

### Women-specific regional leave

The applicability row targets the applicable state/location and gender. Other
states do not receive it. A legal circular is stored as a PolicyDocument.

### Remote-only tenant without a device

An attendance policy allows Web/Mobile and does not require Biometric. Employees
do not need device enrollment. Attendance evaluation still records the published
policy version used.

### Hybrid employee

The base attendance policy permits Web/Mobile/Biometric. EmployeeWorkPattern says
which days are office or remote. Channel rules evaluate the day and location.

### Employee temporary freedom

An approved `PolicyException` permits remote attendance for a defined date range.
After `EffectiveTo`, resolution automatically returns to the base policy.

### Travel, accommodation and reimbursement

Policy rules carry currency, limits, evidence threshold and allowed class. Country,
EmployeeType and designation applicability can be combined.

### Insurance with dependants

Eligibility and limits are policy rules. Published-version assignment determines
employee eligibility; existing insurance enrollment/dependent tables can consume
the applied version when their APIs are rebuilt.

### Policy revision

Published version 1 remains immutable. Tenant clones it to Draft version 2,
changes rules, obtains approval and publishes with a future EffectiveFrom date.

### Bulk import

Import uses the existing upload, preview, confirm, worker and report pipeline.
Policy Type, Policy Definition and Assignment modules expose Import/Export. A row
must be validated for tenant ownership, code uniqueness, foreign-key scope,
effective dates and JSON schema before confirmation.

The three imports now use durable masters 12–14 and routes below
`/api/TenantPolicy/bulk/{target}`. Definitions create Draft version 1;
assignments require Published versions and create acknowledgement evidence. The
worker repeats authorization and row validation. Local build/contracts pass;
isolated PostgreSQL lifecycle and deployed acceptance remain release gates.

## Conflict and validation rules for the controller phase

- TenantId must come from authenticated context, never be trusted from payload alone.
- All referenced masters must belong to the same tenant where applicable.
- A published version cannot be edited or deleted.
- One policy can have only one active current version.
- Effective ranges for current/published versions must not overlap.
- Applicability geography must be hierarchical: State belongs to Country, etc.
- TenantLocation must match the supplied geography.
- Designation must belong to Department; employee must match targeted tenant.
- JSON must be validated against the selected RuleType schema version.
- Publish is blocked until required approval stages and documents are complete.
- Assignment and acknowledgement processing must be idempotent.
- A rejected or archived version cannot become current directly.

## Module and operation catalogue

Parent `TENANT_POLICIES` has seven leaf modules: Types, Definitions, Assignments,
Exceptions, Approvals, Acknowledgements and Audit. PageName values are immutable.
CRUD operations are reused. Existing Import/Export, Submit, Review, Approve,
Reject, Assign, Remove, Upload and Download operations are reused. Missing
Publish, Archive and Acknowledge operations are inserted once by normalized name.

Plan mappings are added for active subscription plans. TenantEnabledModule,
TenantEnabledOperation and Tenant Admin RoleModuleAndPermission rows are populated
by the existing entitlement-sync flow; the seed does not bypass that pipeline.
