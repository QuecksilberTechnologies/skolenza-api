# Scenario test reports

- [Skolenza API product/schema rename and local DB connection — 2026-10-10](reconstruction/api-product-schema-rename/2026-10-10.md)

- [Render seed workbook export and transactional restore verification — 2026-10-09](seed-data/render-seed-workbook/2026-10-09.md)

- [Policy Draft replacement save — 2026-10-09](policy/draft-replacement-save/2026-10-09.md)
- [Policy name uniqueness and version guidance — 2026-10-09](policy/policy-name-uniqueness/2026-10-09.md)

- [Policy leave-type targeting — 2026-10-08](policy/leave-type-targeting/2026-10-08.md)

- [Professional Role Type access-persona names — 2026-10-04](role/professional-access-personas/2026-10-04.md)

- [Render policy types: nine missing categories and duplicate replay — 2026-10-03](policy/bulk-missing-types/2026-10-03.md)
- [Render policy definitions: realistic Draft rules and Permanent applicability — 2026-10-03](policy/bulk-draft-definitions/2026-10-03.md)

- [Render API-only restore — 2026-10-03](deployment/api-only-restore/2026-10-03.md)

- [Employee Bank sensitive-field encryption — 2026-10-03](employee/bank-sensitive-field-encryption/2026-10-03.md)

- [Employee Education create date mapping — 2026-10-03](employee/education-create-date-mapping/2026-10-03.md)

- [Employee monitoring Windows agent foundation — 2026-10-03](employee/monitoring-agent/2026-10-03.md)

- [Unified attendance channels — 2026-10-03](policy/unified-attendance-channels/2026-10-03.md)
- [Policy assignment applicability-safe mapping, bulk and export — 2026-10-03](policy/assignment-applicability-mapping/2026-10-03.md)
- [Policy assignment Download operation mapping — 2026-10-07](policy/assignment-download-operation/2026-10-07.md)

- [Tenant demo end-to-end readiness — 2026-09-28](tenant/demo-e2e/2026-09-28.md)
- [Tenant creation without implicit policy types — 2026-10-01](tenant/creation-policy-independence/2026-10-01.md)
- [Tenant registration actionable errors — 2026-10-01](tenant/registration-actionable-errors/2026-10-01.md)
- [Role Client type display — 2026-10-01](role/client-role-type-display/2026-10-01.md)
- [Employee contact relation and location cascade — 2026-10-01](employee/contact-relation-location/2026-10-01.md)
- [Four-country local-to-Render replacement — 2026-10-01](database/tenant-cleanup-geography-regulatory-seed/2026-10-01.md#authorized-four-country-local-to-render-replacement-follow-up): 164 tables reconciled, 58 MB; local gates 81/81 before and after, Render rollback probes 4/4.
- [Work-mode override missing location assignment — 2026-09-27](employee-work-mode-override/missing-location-assignment/2026-09-27.md)
- [Attendance punch flow — 2026-09-24](attendance/punch-flow/2026-09-24.md)
- [Subscription billing foundation — 2026-09-24](billing/foundation/2026-09-24.md)
- [Employee bulk invitation email smoke — 2026-09-24](bulk/employee-invitation-email/2026-09-24.md)
- [All durable bulk targets — live smoke test — 2026-09-19](bulk/all-live-smoke/2026-09-19.md)
- [Tenant Policy interactive HTML simulator — 2026-09-18](policy/interactive-html-simulator/2026-09-18.md)
- [Policy metadata source audit — 2026-10-02](policy/metadata-source-audit/2026-10-02.md)

Every tested scenario has a report grouped by module, scenario and test date.
Start with [the report template](SCENARIO_TEMPLATE.md). This is the continuing
test-evidence index; API integration instructions remain in
[UIdeveloperDoc](../UIdeveloperDoc/README.md).

```text
docs/
  testing/
    README.md
    SCENARIO_TEMPLATE.md
    employee/
      country-identity/
        2026-09-13.md
```

Add another module/scenario directory when that scenario is actually worked on.
Append a new run section for another run on the same date; use a new dated file
for a later run. Keep earlier failures and link the run that supersedes them.

## Reports

| Module / scenario | Report | Current recorded result |
| --- | --- | --- |
| Database / guarded legacy cleanup | [2026-09-16](database/legacy-cleanup/2026-09-16.md) | Verified backup; 27 legacy tables removed across two guarded phases; location smoke checks pass; accommodation table safely renamed with a temporary compatibility view. |
| Database / manual table removal reconciliation | [2026-09-16](database/manual-table-removal-reconciliation/2026-09-16.md) | Live inventory 128; 22 absent EF mappings quarantined; model mismatch zero, build and API startup pass. |
| Database / canonical production reset and seed | [2026-09-28](database/canonical-production-seed/2026-09-28.md) | Target backup verified; 163 tables reset; ordered seed passed locally and on target; only Deepesh Gupta and Sujeet remain, with zero tenant/employee data and synchronized identities. |
| Database / canonical production master expansion | [2026-09-29](database/canonical-production-seed/2026-09-29.md) | Seven-stage target run passed: 6 employee types, 3 plans, four-country compliance/statutory coverage, one runtime-secret SMTP default, two Host admins, zero tenants/employees and zero invalid foreign keys. |
| Database / worldwide country and compliance seed | [2026-09-29](database/worldwide-country-compliance/2026-09-29.md) | Eight-stage target run passed: 249 ISO countries, 1,018 compliance types, zero coverage gaps, 22 verified statutory types and zero invalid foreign keys. |
| Database / geography and regulatory reseed | [2026-09-30](database/geography-regulatory-reseed/2026-09-30.md) | Production DB reseed passed: 249 countries, 150 states, 12,458 districts, 231,016 localities, 1,018 compliance types, 22 statutory types and 6 country identity rules; 3 tenants and 6 locations preserved with zero checked orphans. |
| Database / local restore and current catalogue seed | [2026-09-30](database/local-render-exit-seed/2026-09-30.md) | Verified 28-Sep dump restored to local PostgreSQL; current geography, regulatory and 260-rule identity catalogues applied; 3 tenants/6 locations preserved and a verified post-seed portable dump created. |
| Database / replacement Render database restore | [2026-09-30](database/render-database-restore/2026-09-30.md) | Cleaned local archive restored to replacement Render PostgreSQL; 164 tables, full geography/regulatory/identity data, 276 foreign keys and 332 indexes reconciled with zero identity-rule orphans. |
| Tenant / create permission context diagnosis | [2026-09-29](tenant/create-permission-context/2026-09-29.md) | Failure traced to absent HOST_TENANT_CREATE operation mapping and generic missing permission-context validation; no product fix applied pending approval. |
| Tenant / creation without implicit policy types | [2026-10-01](tenant/creation-policy-independence/2026-10-01.md) | Obsolete Insurance/Leave policy-type auto-seeding removed from tenant registration; 6 focused transaction cases passed locally; deployed database acceptance pending. |
| Tenant / registration actionable errors | [2026-10-01](tenant/registration-actionable-errors/2026-10-01.md) | Four orphan tenant encryption keys caused the local failure and were removed; safe stage-specific backend errors, UI detail handling, 14 mocked cases, 2 rollback database cases, 44 Angular tests and production build passed locally. Deployed acceptance pending. |
| Tenant / initial location lifecycle | [2026-10-04](tenant/initial-location-lifecycle/2026-10-04.md) | Self and Host onboarding now create an initial TenantLocation transactionally; Host edits synchronize canonical/selected locations. Local command 15/15, PostgreSQL rollback 2/2 and protected Role 15/15 passed; deployed acceptance pending. |
| Tenant / intermittent verification email failure | [2026-09-30](tenant/resend-verification/2026-09-30.md) | Render's complete outbound CIDR ranges were authorized in Brevo; the deployed Host resend then returned success. Recurrence diagnostics now distinguish workstation SMTP tests from Render egress. |
| Holiday calendar / tenant-location refactor | [2026-09-16](holiday-calendar/tenant-location-refactor/2026-09-16.md) | Local contract/build suite 3/3 passed; coordinated target migration and deployed API acceptance pending. |
| Holiday calendar / target DB migration | [2026-09-22](holiday-calendar/tenant-location-refactor/2026-09-22.md) | Development-configured target table backed up and migrated; 15-column schema/FK verified, focused suite 3/3 passed; deployed API acceptance pending. |
| Holiday calendar / Tenant Policies child module | [2026-09-22](holiday-calendar/policy-child-module/2026-09-22.md) | Child Id 118 under parent Id 108, View/Add/Update/Delete mappings and 10 plan entitlements verified; holiday write APIs remain pending. |
| Holiday calendar / CRUD implementation | [2026-09-22](holiday-calendar/crud/2026-09-22.md) | Local API build, 11 focused tests and unauthenticated HTTP 401 checks on all 5 routes passed; authenticated CRUD and deployed verification pending. |
| Holiday calendar / import-export | [2026-09-22](holiday-calendar/import-export/2026-09-22.md) | Local build and 14 focused tests passed; target DB seed rerun yielded one Import and one Export mapping. Authenticated HTTP, isolated full-seed fixture and deployed checks pending. |
| Holiday calendar / 2026 Jabalpur data | [2026-09-22](holiday-calendar/live-2026-jabalpur/2026-09-22.md) | Tenant 8 location 5: 31 sourced rows across all 12 months, repeat insert 0, local unauthenticated API smoke and 14 focused tests passed; company approval and authenticated HTTP pending. |
| Holiday calendar / unique date | [2026-09-22](holiday-calendar/unique-date/2026-09-22.md) | Non-soft-deleted same-date rows, including inactive, now block create/import; partial unique index and rollback checks passed. Authenticated HTTP pending. |
| Holiday calendar / display constants | [2026-09-24](holiday-calendar/calendar-display-constants/2026-09-24.md) | Token-only color/status/priority API added; 4 Holiday contract tests and unauthenticated HTTP 401 passed; authenticated and deployed verification pending. |
| Holiday calendar / TechNova 2026 Local and Render seed | [2026-10-04](holiday-calendar/technova-2026-seed/2026-10-04.md) | Missing primary locations created by stable TenantCode; 31 holidays across 12 months seeded in Local and Render. Idempotency rerun inserted zero rows and 17 focused Holiday tests passed. |
| Location / City-to-Locality refactor | [2026-09-16](location/locality-refactor/2026-09-16.md) | Four-country postal seed applied (222,683 postal localities); 10/10 focused tests passed; updated deployed API acceptance pending. |
| Policy / generic framework schema | [2026-09-16](policy/framework-schema/2026-09-16.md) | Generic persistence and scope-1 policy module hierarchy applied to target DB; endpoint implementation remains a separate phase. |
| Policy / API foundation | [2026-09-16](policy/api-foundation/2026-09-16.md) | Local EF/API/permission/lifecycle contract implemented; focused result and deployed acceptance are recorded in the report. |
| Policy / target DB migration | [2026-09-16](policy/target-db-migration/2026-09-16.md) | Target prerequisites, 11 policy tables, module hierarchy, 47 mappings and bulk master 1–14 verified; 29 focused tests passed. |
| Policy / durable bulk import | [2026-09-16](policy/bulk-import/2026-09-16.md) | Authenticated Policy Type preview/confirm/worker and DB reconciliation pass; definitions, assignments, retry and cancellation remain pending. |
| Policy / deployed real-data business flow | [2026-09-16](policy/live-business-flow/2026-09-16.md) | Type CRUD, draft/version lifecycle, ordered approval, publish, assignment, resolution, exception, acknowledgement, audit, clone and assignment bulk reconciled; document upload blocked by invalid Render S3 credentials. |
| Policy / deployed route smoke | [2026-09-16](policy/deployed-route-smoke/2026-09-16.md) | Live commit and 37 deployed operations verified; authenticated business-flow acceptance needs tenant credentials. |
| Policy / Attendance location-scope lookup | [2026-09-26](policy/attendance-location-scope-lookup/2026-09-26.md) | Backend enum-derived Policy lookup and UI Policy Definition integration implemented; local verification is recorded in the report, authenticated/deployed acceptance pending. |
| Policy / exact Draft version edit | [2026-10-08](policy/draft-version-edit/2026-10-08.md) | Edit now loads the exact Draft version selected in the list when an older Published version remains current; Angular 8/8, production build and localhost browser verification passed. |
| Policy / Resolve encoded employee ID | [2026-09-26](policy/resolve-encoded-employee-id/2026-09-26.md) | Resolve now accepts the globally salted encoded Employee API identifier; 17 backend and 4 Angular focused tests passed locally, authenticated HTTP pending. |
| Policy / Audit actor display name | [2026-09-27](policy/audit-display-name/2026-09-27.md) | Audit API now returns tenant-scoped `changedByName` and nullable `versionNumber`; backend contract and UI fallback tests pass locally, authenticated/deployed acceptance pending. |
| Dashboard / Super-Admin widgets | [2026-09-27](dashboard/super-admin-widgets/2026-09-27.md) | Fresh role-gated Dashboard controller exposes nine independent widget endpoints; local build/contract verification recorded, authenticated/deployed smoke pending. |
| Dashboard / role-based data | [2026-10-04](dashboard/generic-persona-widgets/2026-10-04.md) | Trusted RoleType-code configuration and independent widgets cover Tenant Administrator, People Manager, Workforce User and External User; placeholder data is explicitly marked and deployed acceptance is pending. |
| Role / Client type display | [2026-10-01](role/client-role-type-display/2026-10-01.md) | Constants-backed authenticated `type-options` API now supplies Add/Edit/filter without permission IDs; backend 9/9, protected tenant registration 14/14, Angular 69/69 and production build passed locally. Authenticated/deployed HTTP pending. |
| Role / stable role-type code | [2026-10-04](role/role-type-stable-code/2026-10-04.md) | Constants-backed stable codes added additively to the Role Type options contract; local verification recorded in the report and deployed response verification pending. |
| Employee / China and USA identity options | [2026-09-13](employee/country-identity/2026-09-13.md) | China created; identity GET failed with 500 before fix. USA creation blocked by missing country option. Local suite 18 passed, 5 skipped; post-fix live acceptance pending. |
| Employee / country-driven identity catalogue | [2026-09-30](employee/country-identity/2026-09-30.md) | Production catalogue has 249 Passport baselines and 260 total rules, including UK NINO. Selected employee ID now reaches the API, which resolves persisted country; dynamic UI, unit/build/lint and sequential Chrome scenarios pass locally. UI deployment pending. |
| Employee / identity table usage audit | [2026-09-30](employee/identity-table-usage/2026-09-30.md) | All four identity tables remain required. Two obsolete functions were removed locally and from project definitions; Render cleanup remains pending while its database is suspended. |
| Employee / legacy identity function removal | [2026-09-30](employee/legacy-identity-function-removal/2026-09-30.md) | Two obsolete functions removed from the local database and project definitions with no CASCADE; all four identity tables and 260 rules preserved. Build and 20 executed focused tests passed; Render cleanup pending. |
| Employee / profile CRUD, validation and percentage | [2026-09-30](employee/profile-crud/2026-09-30.md) | Central percentage rules and confirmed Education, Insurance and Identity defects fixed locally; focused backend and Angular verification recorded, authenticated deployed CRUD pending. |
| Employee / profile completion consistency | [2026-10-10](employee/profile-completion-consistency/2026-10-10.md) | Backend Employee list, profile summary, Bank and country-driven Identity calculations share mandatory-aware completion rules; local verification recorded, authenticated/deployed acceptance pending. |
| Employee / contact relation and location cascade | [2026-10-01](employee/contact-relation-location/2026-10-01.md) | Token-only relation API with Owner; initial contact/edit/add PostgreSQL rollback verification; UAE seed and schema applied locally; 81 combined backend/DB/isolated HTTP tests, 66 Contact UI and 69 protected Role UI tests, production build/lint passed. Running-product browser and deployed acceptance pending. |
| Employee / get-all assigned roles | [2026-09-13](employee/get-all-assigned-roles/2026-09-13.md) | Response contract/build pass locally; isolated DB and deployed response verification pending. |
| Employee / Reset Password module consolidation | [2026-09-23](employee/reset-password-module-consolidation/2026-09-23.md) | Module 38 and all checked dependencies removed from the Development DB; Operation 21, tenant entitlement and role grant moved to `EMP_LIST`; focused suite 3/3 passed. |
| Employee Work Arrangement / Attendance policy options | [2026-09-23](employee-work-arrangement/attendance-policy-options/2026-09-23.md) | Token-only effective-date dropdown endpoint added; build, 4 new tests, 23 relevant regressions and local unauthenticated HTTP 401 passed; authenticated data and deployed verification pending. |
| Employee Work Arrangement / Attendance policy permission regression | [2026-09-24](employee-work-arrangement/attendance-policy-options/2026-09-24.md) | Diagnosed deployed `FORBIDDEN` as a global Employee permission-pipeline interception; exact token-only query exception added and 6 focused tests passed locally; corrected deployment verification pending. |
| Employee Work Arrangement / PolicyVersion persistence | [2026-09-24](employee-work-arrangement/policy-version-persistence/2026-09-24.md) | Dropdown-to-save contract migrated to exact generic `PolicyVersionId`; backend 9/9, form schema 9/9, Angular stores 18/18 and production build passed; configured target DB migration verified; deployed authenticated acceptance pending. |
| Employee Work Arrangement / location consistency | [2026-09-24](employee-work-arrangement/location-consistency/2026-09-24.md) | Primary-assignment coverage, work-mode/location-type and date-window overlap validation implemented; focused tests and migration/API acceptance recorded in the report. |
| Employee Work Arrangement / Client Site location type | [2026-09-26](employee-work-arrangement/client-site-location-type/2026-09-26.md) | Angular/backend enum drift and affected Development data corrected; 38 focused tests and production build passed; authenticated HTTP create remains to be captured. |
| Employee Work Arrangement / clear validation errors | [2026-09-26](employee-work-arrangement/clear-validation-errors/2026-09-26.md) | Employee, location, assignment flags, location type, and effective-date failures now return actionable messages; soft-deleted rows are excluded; build and 29 focused tests passed locally. |
| Employee Work Arrangement / date overlap | [2026-09-26](employee-work-arrangement/date-overlap/2026-09-26.md) | Create, update, and activation share one inclusive date-overlap validator with conflicting ranges in the error; local verification is recorded in the report. |
| Employee Work Arrangement / encoded employee ID update | [2026-09-27](employee-work-arrangement/update-encoded-employee-id/2026-09-27.md) | Arrangement responses now expose the tenant-salted employee ID string across create/update/status/get/list; focused contract and regression tests passed locally. |
| Employee Work Location / update, deactivate and delete | [2026-09-26](employee-work-location/update-status-delete/2026-09-26.md) | Read responses now return tenant-salted encoded employee IDs, fixing edit; CRUD contracts, dependency guards, build, 15 Angular tests, 30 backend tests and full backend suite passed locally. |
| Employee work configuration / centralized validation | [2026-09-27](employee-work-configuration/centralized-validation/2026-09-27.md) | Shared arrangement, pattern and override rules replace repeated mode, location, date-window and text-limit logic; 142 backend and 27 Angular tests plus production build passed locally. |
| Bulk / existing-module operation cleanup | [2026-09-13](bulk/module-operation-cleanup/2026-09-13.md) | Target DB cleanup complete: zero bulk modules/orphan entitlements; 22 functional Import/Export mappings. Isolated seed rerun and 36 focused tests passed. |
| Module / seed metadata and operation cleanup | [2026-09-14](module/module-operation-seed/2026-09-14.md) | Dashboard seeds removed; source contract covers metadata completion and Add/Create cleanup. Isolated PostgreSQL verification remains blocked because the local fixture is unavailable. |
| Module / duplicate Export operation cleanup | [2026-09-23](module/duplicate-export-operation-cleanup/2026-09-23.md) | Duplicate Export Id 14 and all checked dependencies removed; canonical Id 23 retained and permissions migrated; focused suite 2/2 passed. |
| Module / Tenant Attendance Policies removal | [2026-09-23](module/tenant-attendance-policies-removal/2026-09-23.md) | Module Id 67, four mappings, tenant entitlements, plan mapping and role grants removed from Development DB; production seed definition removed; focused suite 2/2 passed. |
| Tenant entitlements / Tenant Admin permission sync | [2026-09-15](tenant-entitlements/admin-permission-sync/2026-09-15.md) | Local build and command wiring pass; disposable PostgreSQL behavior test skipped because its required environment is unavailable; deployed acceptance pending. |
| Database / tenant cleanup and geography-regulatory seed | [2026-10-01](database/tenant-cleanup-geography-regulatory-seed/2026-10-01.md) | Render tenant graph removed (3 tenants, final count zero); 249-country catalogue, four-country detailed geography, 1,018 compliance types and 22 statutory types verified; legal rule rows pending authoritative sources. |
| Tenant Email Template / CRUD and seed | [2026-09-24](tenant-email-template/crud-and-seed/2026-09-24.md) | Tenant table/data, module child, CRUD permission flow, four mappings, plan and enabled-tenant entries implemented; configured target seed verified; authenticated HTTP pending. |
| Subscription / public plan deployment drift | [2026-09-30](subscription/public-plan-deployment-drift/2026-09-30.md) | Local GET returned restored plan data with HTTP 200; deployed Swagger still exposes an older POST/authenticated contract and returns 405 to Angular. Corrected deployment is pending authenticated push. |
| Module / singular master parent hierarchy | [2026-09-15](module/singular-master-parent-hierarchy/2026-09-15.md) | Final focused canonical leaf, EmployeeType, permission, and source-contract suite: 51 passed, 4 DB-fixture skips; isolated PostgreSQL execution and deployed menu verification remain pending. |

## Existing evidence kept at its original location

- [Render smoke checks, 2026-09-13](../RENDER_SMOKE_TEST_2026-09-13.md)
- [Automation test commands and historical results](../../skolenza.automationtests/README.md)

These links preserve previous evidence; they do not claim a new test run.

## Status rules

- **PASS:** the stated assertion was executed and matched the expected result.
- **FAIL:** the assertion was executed and did not match.
- **BLOCKED:** a required prerequisite prevents the scenario from being executed.
- **SKIPPED:** the test runner explicitly skipped a test; include its reason.
- **NOT RUN / PENDING:** planned verification has not been executed.

Label each result with its environment. A local PASS does not establish deployed
acceptance. Do not reconstruct a missing payload or raw response as if it was
captured. Mark illustrative examples and code/seed expectations explicitly.

Record exact test names, counts, command, commit/build when known, and links to
sanitized logs or reports. Redact secrets and sensitive personal data before
saving evidence. Documentation-only work does not require repeating passed tests.
## Latest holiday report

- [Employee work pattern validation — 2026-09-26](employee-work-pattern/validation/2026-09-26.md)
- [Employee work pattern response identity — 2026-09-27](employee-work-pattern/response-employee-identity/2026-09-27.md)
- [Employee work pattern arrangement flow — 2026-09-27](employee-work-pattern/arrangement-flow/2026-09-27.md)
- [Employee work mode override approval and overlap — 2026-09-26](employee-work-mode-override/approval-overlap/2026-09-26.md)

- [Holiday rename and Icon field — 2026-09-23](holiday-calendar/rename-to-holiday/2026-09-23.md)
 - [Tenant email queue runtime resolution — 2026-09-24](tenant-email-template/queue-runtime-resolution/2026-09-24.md)
# Tenant verification resend

- [2026-09-29 SMTP rejection and actionable error](tenant/resend-verification/2026-09-29.md)
- [2026-09-30 intermittent Render outbound-IP authorization](tenant/resend-verification/2026-09-30.md)
# Policy generic rule metadata

- [2026-10-02 — code-driven rule schema, Angular authoring and Render metadata migration](policy/generic-rule-metadata/2026-10-02.md)
- [2026-10-08 — Leave annual entitlement and derived accrual amount](policy/annual-entitlement-accrual/2026-10-08.md)
- [2026-10-07 - publication integrity, clone lifecycle and archived assignment state](policy/publication-integrity/2026-10-07.md)
- [2026-10-08 - retired legacy policy API, model, table and module hard delete](policy/retired-legacy-hard-delete/2026-10-08.md)
