# UI developer documents

- [Policy name uniqueness and next-version guidance](../testing/policy/policy-name-uniqueness/2026-10-09.md)

- [Policy exact Draft version edit verification](../testing/policy/draft-version-edit/2026-10-08.md)

- [Employee Monitoring Agent API](EMPLOYEE_MONITORING_AGENT_API.md) — Tenant policy,
  one-time PC-agent enrollment, heartbeat/disconnect status and offline screenshot synchronization.

- [Role Type Options API](ROLE_TYPE_OPTIONS_API.md) — authenticated, permission-ID-free
  backend catalogue for Role filters and Add/Edit dropdowns, including Client type 4.

- [Attendance Punch API](ATTENDANCE_PUNCH_API.md) — authenticated employee Mobile/Web check-in, check-out, idempotency, geofence and today status contract.

- [Full Policy With Example](fullPolicyWithExample.md) — single consolidated guide with table ownership, UI screens, applicability, lifecycle, IOCL and Dubai-to-India transfer, leave balances, diagrams, APIs and implementation gaps.

- [Tenant Policy Live Flow Simulator](policy-live-simulator/README.md) — shareable standalone folder; double-click start, live DDL, rule/applicability builder, all 37 policy endpoints, DB-table impact and safe local JSON files.

- [Host Catalogue Bulk Upload](HOST_CATALOGUE_BULK_UI_IMPLEMENTATION.md) — four
  Host Admin catalogue bulk screens, API lifecycle, Excel contracts and UI examples.
- [Host Card and Device Bulk Upload](../bulk-upload/HOST_CARD_DEVICE_IMPORT.md)
- [Tenant and Employee Bulk API Scenarios](../bulk-upload/BULK_API_SCENARIOS.md)
- [Tenant Device Permission Contract](TENANT_DEVICE_PERMISSION_CONTRACT.md) — exact
  menu-operation mapping and screen-load API sequence.
- [Host Tenant Verification Resend](HOST_TENANT_VERIFICATION_RESEND.md) — Host SMTP
  selection and actionable HTTP 502 provider-error contract.
- [Employee Profile Verification API](EMPLOYEE_PROFILE_VERIFICATION_API.md) — supported
  section identifiers, read-only rows and the update-bulk payload contract.
- [Employee list assigned roles](EMPLOYEE_GET_ALL_ASSIGNED_ROLES.md) — existing
  Employee get-all route with the `assignedRoles` response contract.
- [Employee Contact Relation and Location](EMPLOYEE_CONTACT_RELATION_LOCATION.md) —
  token-only enum relation catalogue, automatic initial contact row and
  Country/State/District/Locality cascade.
- [Employee country-driven identity](EMPLOYEE_COUNTRY_IDENTITY.md) — dynamic
  country document catalogue, API/UI flow, six-table ownership, four seeded
  masters, persistence behavior and deferred legacy-function cleanup.
- [Employee Reset Password](EMPLOYEE_RESET_PASSWORD.md) — Reset Password operation
  on `EMP_LIST`; standalone Module 38 removal and request contract.
- [Employee Work Arrangement Attendance Policy Options](EMPLOYEE_WORK_ARRANGEMENT_ATTENDANCE_POLICY_OPTIONS.md) — token-only, effective-date-based Published Attendance policy dropdown and empty-state contract.
- [Employee Work Location and Arrangement Validation](EMPLOYEE_WORK_LOCATION_ARRANGEMENT_VALIDATION.md) — effective-window overlap, primary assignment coverage, work-mode/location-type rules, and deployment migration.
- [Holiday](HOLIDAY.md) — location-based
  holiday persistence, date-only response contract, and rollout status.
- [Locality and Locality Type API](LOCALITY_AND_LOCALITY_TYPE_API.md) — Country/State/
  District/Locality lookup flow, City/Town/Village types, and TenantLocation payload.
Treat Module `PageName` and Operation `OperationName` as immutable identities during
seed and catalogue integration. Resolve permission IDs dynamically from the
authenticated menu; never hard-code the numeric examples in documentation.

## Mandatory document for every new API

Whenever a new API or endpoint is requested, add or update one handoff document in
this folder. Include:

1. Feature purpose, screen behavior and user actions.
2. Authentication plus dynamic ModuleId/OperationId discovery.
3. Every route, HTTP method and permission requirement.
4. Mandatory and optional fields with types and validation rules.
5. Copyable JSON, query-string, FormData and Excel/CSV examples as applicable.
6. Representative preview, success, progress and error response JSON.
7. Status/enum meanings, polling, retry, cancellation and report behavior.
8. Destination tables, persistence timing, isolation and retention behavior.
9. Exact automated test results and an explicit local/deployed status.

This handoff is part of the endpoint's completion checklist so another UI developer,
Claude or Codex can implement the frontend without guessing backend behavior.

- [Tenant Policy complete master guide — Hinglish](TENANT_POLICY_COMPLETE_MASTER_GUIDE_HINGLISH.md)
  — single consolidated reference covering concepts, 16 tables and dependencies,
  complete lifecycle/versioning, permissions, all 37 APIs, bulk flow, UI planning,
  examples and verification evidence.
- [Tenant Policy table/data-flow guide](TENANT_POLICY_TABLE_DATA_FLOW_HINGLISH.md)
  — exact properties, relationships and action-to-table write behavior.
- [Tenant Policy versioning and permissions](TENANT_POLICY_VERSIONING_PERMISSIONS_HINGLISH.md)
  — effective dates, Draft/Publish/Archive behavior and HR/Admin permission flow.
- [Tenant Policy endpoint catalogue](TENANT_POLICY_ENDPOINT_CATALOG.md) — all
  policy routes with copyable request/response examples and verification status.
- [Tenant Policy API flow — Hinglish](TENANT_POLICY_API_FLOW_HINGLISH.md) — zero-level
  business sequence, screen-to-API mapping and the purpose of all 37 routes.
- [Tenant Policy actual UI screen → API → table map](TENANT_POLICY_UI_SCREEN_TO_API_TABLE_MAP.md)
  — read-only Angular/backend code audit, each existing page/button, request/response,
  database effect, worked example and missing/broken UI flows.
- [Tenant Policy actual screenshots guide](TenantPolicyActualScreensGuide.docx)
- [Super-Admin Dashboard API](SUPER_ADMIN_DASHBOARD_API.md)
  — Word guide with UI screenshots, click-to-API/table explanation and verified gaps in red.

Latest Render API evidence: [2026-09-13 smoke test](../RENDER_SMOKE_TEST_2026-09-13.md).

Scenario test evidence is organized by module, scenario and date under
[docs/testing](../testing/README.md). Every tested scenario must have a report
there using the [scenario template](../testing/SCENARIO_TEMPLATE.md), with exact
local/live status, inputs, expected/actual results and outstanding checks.
Current Employee example: [China/USA identity options](../testing/employee/country-identity/2026-09-13.md).
# Tenant Policy accrual calculation

- [Leave annual entitlement and derived accrual amount](GENERIC_POLICY_RULE_METADATA.md#save-validation)
