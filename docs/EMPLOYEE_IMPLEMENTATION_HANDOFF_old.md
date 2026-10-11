# Employee implementation: authoritative continuation record

Updated: 2026-09-12. Overall status: WIP. Production deployment: NOT COMPLETE.

## Authorization and invariants

The user authorized all Employee tabs, CRUD, permissions, shared completion,
tenant-wide defaults for five operational tabs, country identity/statutory/compliance,
bulk compatibility, input/response DTO documentation, tests and production release.
Continue without requesting "next" or repeating implementation permission requests.
Preserve the existing permission pipeline, constants, regions and business behavior.
Verified information must not remain employee-editable. Admin configuration must not
be confused with employee self-service. Do not invent country laws or spouse limits.

## Mandatory verification rule (user-confirmed 2026-09-13)

Never deliver an Employee feature, SQL migration/seed or release as complete without
executing its relevant automated tests. SQL must pass on the isolated PostgreSQL
fixture on first execution and rerun. Report exact pass/fail/skip counts; skipped or
unavailable execution is not a pass. Compilation alone is insufficient. If runtime
or database access is unavailable, mark the item UNTESTED/BLOCKED rather than ready.
The observed Module seed FK failure (`FK_Module_ParentModule`, Module Id 80) remains
a failing regression until corrected and verified through actual database execution.

## Workspace

- Backend: C:/SkolenzaCodeBase/QuecksilberTechnologies
- Angular: C:/latestSkolenzaUI/skolenza-app
- Current analysis: docs/currentEmployeemodule.md
- DTO inventory: docs/EmployeeModuleApiDtoContracts.md
- Bulk history: docs/AI_ASSISTED_BULK_IMPORT_REFERENCE.md

## Acceptance ledger

| Requirement | Status | Evidence / remaining acceptance |
| --- | --- | --- |
| Endpoint/DTO inventory | WIP | Existing analysis and DTO documents; final contracts must match implemented behavior |
| Existing percentage characterization | PASS | 16 focused tests passed in prior run; not DB or end-to-end coverage |
| Shared percentages across tab pages/dashboard | WIP | New 13-section implementation exists; legacy pages still use other calculators; applicability and country rules unfinished |
| Verified implies locked | WIP | Status repository paths changed; direct CRUD and document mutations still require audit/enforcement/tests |
| Bulk section status | WIP | Seven total areas (Overview, Bank, Contact, Experience, Identity, Education, Dependent); unsupported sections and atomic failure handling unfinished |
| Five operational tenant defaults | PASS (code/test) | Persistent setting, admin endpoint, permission-pipeline integration and isolated DB coverage implemented |
| Country identity | PASS (code/test) | Selected-employee read, nationality/origin CountryId resolution and active rule validation implemented |
| Statutory/compliance/master seed | PENDING | Verify schemas and authoritative sources before seeding; preserve existing configured rules |
| Country dependent policy | PENDING | No tested rule resolution for spouse-limit example |
| EmployeeBulk compatibility | WIP | Prior base import acceptance exists; current integration and full regression not complete |
| Angular | WIP | data binding and numeric tab mapping edited; hardcoded mapping must reuse existing constants; build not verified |
| Production release | PENDING | Fresh backup, migrations, release build, deployment and authenticated live acceptance required |

## Corrections to earlier claims

Earlier documents mark some profile changes COMPLETE based on compilation/unit tests.
Those claims do not establish production acceptance. The 13-section denominator and
assignment-presence percentages are provisional and require reconciliation with existing
page formulas and optional/applicable sections. Leave/Files were omitted, not implemented.
There are seven supported verification areas including Overview, not seven plus Overview.
No country seed, operational default API, or production deployment has occurred in this task.
The prior full isolated suite had 10 failures and 7 skips; never describe it as green.

## Continuation order

1. Verify country/master schemas, existing values and seed tooling; prepare idempotent seed.
2. Repair country identity owner selection and validate allowed documents on writes.
3. Implement operational defaults through existing permission pipeline and persisted settings.
4. Enforce verified/edit restrictions across create/update/delete/document paths.
5. Reconcile one completion contract across individual tabs, settings and dashboard.
6. Complete country/statutory/compliance rule resolution, management and meaningful DB tests.
7. Repair Angular toolchain, build and test UI; run relevant bulk regressions.
8. Record exact request/response contracts, test results, release artifact and deployment evidence.

Record every change and blocker below. Do not store credentials or employee identity values.

## Execution checkpoint: country validation and seed

- The historical `database-scripts/SeedEmployeeCountryIdentityCatalog.sql` was consolidated into `database-scripts/production-seed/05-dependent-master/001-employee-identity-catalog.sql`; the original run was applied twice to
  isolated PostgreSQL `127.0.0.1:55439/skolenza_bulk_test`. First run inserted one
  missing mapping; second inserted zero. Existing configured mandatory flags preserved.
- Seed establishes India/Aadhaar and UAE/Emirates ID catalogue mappings only.
  Sources: https://uidai.gov.in/en/my-aadhaar/about-your-aadhaar and
  https://u.ae/en/information-and-services/visa-and-emirates-id/emirates-id.
  Residency is not represented by Employee.CountryId, so no mandatory residency
  assumption, salary contribution rate, or spouse limit was invented.
- Identity GET accepts optional encoded EmployeeId; defaults to authenticated actor.
  Checks employee data scope and tenant ownership; persisted employee.CountryId now
  supplies the document lookup instead of client CountryNationalityId.
- Identity CREATE validates document eligibility through active country/category/document
  mappings, value length, and effective date range. Null expiry now remains null.
- Compliance precedence explicitly ranks tenant/state specificity because PostgreSQL
  DESC otherwise places null first. Added effective-date/ID deterministic tie-breakers.
- New DB tests: initial run one pass (identity tenant/inactive rejection), one failure
  exposed missing EF schema mapping for existing ComplianceTypeMaster/ComplianceRule.
  Added explicit skolenza table mapping and jsonb mapping; rerun PASSED 2/2, zero skips.
- Backend build before mapping/test additions passed: zero errors, 583 warnings.
- Angular package manager is Bun (`bun.lock`), not npm. Global npm executable exists;
  npm ci failed because this is not an npm-lockfile project. Bun frozen install PASSED.
  Do not repeat earlier incorrect claim that npm is necessarily broken.
- UI AGENTS.md references .codex/docs files which are absent; inspect available local
  instructions before any further UI edits.

## Execution checkpoint: operational defaults and mutation audit

- Angular development build PASSED using `bun run build --configuration development`.
- Added `TenantEmployeeSectionDefault` entity/mapping and idempotent SQL migration;
  applied to isolated DB only. Missing setting means employee self-service edit OFF.
- Added POST `/api/Employee/section-defaults`: tenant Admin, existing EMP_LIST grant
  and active Update operation required. `{moduleId, operationId, isEditAllowed}`
  sets all five operational section defaults atomically for existing/future employees.
  Response is ApiResponse<List<EmployeeSectionDefaultResponseDTO>> with moduleCode/isEditAllowed.
- Existing EmployeeTenantPermissionBehavior now consults defaults before operational
  employee commands. Reads remain subject to existing scope/role checks. Admin workflows
  remain separate. Per-employee overrides and Manager self-service need further review.
- Latest completed combined run: 19/19 passed (16 characterization + 3 PostgreSQL tests).
- Bank update now compares record owner with requested employee and validates tenant/data
  scope. Bank delete also validates scope. Both reject non-admin mutation of locked/verified rows.
- Education update and Dependent/Experience update received scope and lock checks.
  Contact update/delete, Education delete and Dependent delete reject locked/verified rows
  after their existing self-owner checks. These still need consistent admin-flow review.
- Three new Bank delete rejection tests added. Combined run PASSED 22/22, zero skips.
- Subsequent changes add Overview update, Experience document/row deletion locks and
  verified-safe completion metadata for legacy sections. The existing Bank percentage
  characterization now expects verified data to be locked while keeping 99-percent formula.
  These subsequent changes have a focused test rerun in progress.
- Compliance create versioning now refuses to shorten a global/different-state fallback
  rule when creating a tenant version. Full compliance CRUD is still incomplete; the
  existing ComplianceRuleController/update is incorrectly wired to leave balance and must
  be repaired with permission protection before release. Do not expose unguarded handlers.
- No target DB writes, deployment, statutory rule seed or all-country compliance implementation
  is claimed. Operational API HTTP acceptance and complete direct-mutation coverage remain WIP.

## Latest production-gap fixes

- ComplianceRuleController no longer routes `/update` to leave-balance logic. It is now
  authenticated and exposes create/update ComplianceRule commands.
- UpdateComplianceRule request/response DTOs now carry the rule identity, JSON payload,
  priority, effective dates and active state. The handler validates tenant ownership,
  date range and persists audit fields through the existing repository/unit-of-work.
- `ApplyBulkImportMigrations.ps1` now includes the Employee operational-default and
  country-identity seed scripts in the standard release migration sequence. Both scripts
  are idempotent; no target production database was executed from this workspace.
- Full solution build after these fixes: succeeded, zero errors. Existing dependency,
  nullable and XML documentation warnings remain and are recorded as non-blocking debt.

## Latest continuation: status API and UI consistency

- Fixed existing UnitOfWork.CompilanceRuleRepository getter which threw NotImplementedException;
  it now constructs the existing repository with context/logger.
- Status edit/verify pipeline now resolves expected module from TabInfoType for the seven
  supported sections, matching Angular PROFILE_TABS and tab action grants. Insurance lacks
  persisted verification fields and is rejected rather than falsely accepted.
- Bulk section update requires tenant Admin, validates unsupported/duplicate sections before
  writes, starts a transaction, rejects unsuccessful section changes and rolls back on failure.
- Angular bulk save now omits absent sections. It reuses existing PROFILE_TABS constants.
  Unsupported section toggles are disabled; verified section edit toggle is disabled.
- Added Angular route permission mapping for POST Employee/section-defaults to EMP_LIST/Update.
- Focused backend rerun PASSED 23/23, zero skips. Angular route/interceptor focused suite
  PASSED 31/31. Angular development build PASSED; existing unrelated compiler warnings remain.
- CONFIRMED USER DECISION: Employee.CountryId means nationality/origin country.
  Identity catalogue follows this field. Work/residence jurisdiction must be resolved separately
  for statutory/compliance eligibility. Never silently repurpose Employee.CountryId.
- Discovered GetEmployeeProfileStatusQuery had no namespace, bypassing the namespace-gated
  Employee permission behavior despite carrying a permission DTO. Added the existing
  EmployeeBase.Handlers namespace; pipeline characterization now includes this query.
- Added PostgreSQL verification-lock test. Initial fixture failed because account type
  'Savings' violates DB allowed values ('salary','current','saving'); corrected test fixture
  to 'saving'. The DB constraint was preserved. Rerun pending at this checkpoint.
- Completion repository now propagates database failure to existing middleware instead of
  silently reporting a successful empty profile.
- Build after the namespace, status-pipeline, and compliance repository fixes PASSED with
  0 errors and 10 warnings.
- Production deployment remains PENDING: no target backup/migration execution, release
  publish, service restart, authenticated production smoke test, or rollback verification
  was performed from this session. Local isolated seed/migration execution is not production.

## Final local release validation

- `dotnet test skolenza.automationtests/skolenza.automationtests.csproj --no-build
  --filter FullyQualifiedName~Employee`: 217 passed, 0 failed, 30 skipped (the skipped
  cases require external/database fixtures and are not reported as passes).
- Full solution build after the final Compliance DTO/controller changes: succeeded with
  0 errors. Existing package vulnerability, nullable and XML-comment warnings remain.
- Release migration runner includes both new Employee scripts and validates each file
  before execution. Production execution, backup, service restart and live smoke remain
  operational deployment steps, not executable against this workspace without a target.
- Country identity seed now covers India (Aadhaar/PAN), Pakistan (CNIC), USA (SSN),
  UAE (Emirates ID) and China (Resident Identity Card). All mappings use existing
  Country codes and remain non-mandatory catalogue entries; no statutory/legal
  requirement was invented. A fresh API Release publish was generated at
  `artifacts/employee-release/publish` on 2026-09-12.
- The historical `database-scripts/EmployeeProductionSeed.sql` was consolidated into `database-scripts/production-seed/05-dependent-master/001-employee-identity-catalog.sql`.
  It creates the operational-default table and all identity catalogue/rule mappings
  in one advisory-locked transaction. The migration runner now executes this single
  bundle instead of separate Employee seed files.
