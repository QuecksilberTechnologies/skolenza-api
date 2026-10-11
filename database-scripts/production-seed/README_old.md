# Canonical production reset and seed

Target database: `workforcedb_34hi_duis`, PostgreSQL schema `skolenza`.

Run the SQL files in the folder/name order below. Every file is complete and requires no text
replacement. Run with `ON_ERROR_STOP` enabled. Stop immediately if any file fails.

## One-command execution with logs

Use the included runner instead of opening the SQL files manually. Supply the SMTP key through the
process environment or the `-SmtpKey` parameter; the runner never writes it to a script or log:

The runner supports the built-in Windows PowerShell 5.1 as well as PowerShell 7.

```powershell
cd C:\SkolenzaCodeBase\QuecksilberTechnologies\database-scripts\production-seed
$env:SKOLENZA_SEED_SMTP_KEY = '<runtime SMTP key>'
.\Run-CanonicalProductionSeed.ps1 -Environment Development -PsqlPath 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
```

When PostgreSQL is installed under the standard `C:\Program Files\PostgreSQL` directory, the short
command also works because the runner detects `psql.exe` automatically:

```powershell
.\Run-CanonicalProductionSeed.ps1 -Environment Development
```

Check the configuration, backup and script paths without connecting or changing data:

```powershell
.\Run-CanonicalProductionSeed.ps1 -Environment Development -ValidateOnly
```

Every actual run creates `execution-logs/<yyyyMMdd-HHmmss>/` containing:

- `canonical-seed-combined.log`: output of the complete run;
- `01-clean.log` through `08-verification.log`: separate output for each stage;
- `canonical-seed-summary.csv`: status, exit code, start/end time and duration for every attempted
  stage.

The runner verifies the exact database name and required backup before execution. It uses
`ON_ERROR_STOP=1`, stops at the first failed stage, marks it `FAILED`, and does not run later stages.
Connection credentials are never written to the logs.

## What each folder does

| Order | Folder | Purpose | Main tables |
| --- | --- | --- | --- |
| 00 | `00-analysis` | Schema inventory and proof of seed coverage; never changes the database | 164-table coverage matrix, 278 foreign keys, identity inventory, pre/post row counts |
| 01 | `01-clean` | Deletes the approved baseline data and resets identities | All audited `skolenza` tables through one guarded `TRUNCATE ... RESTART IDENTITY CASCADE` |
| 02 | `02-parent-master` | Inserts independent shared lookup data needed by later scripts | employee/device/client types, three subscription plans, policy catalogues, UI catalogues, email templates, gender, industry, tender status |
| 03 | `03-geography` | Inserts the complete country catalogue, detailed location hierarchy and regulatory masters | 249 ISO countries; detailed geography for IN/CN/DE/US; universal compliance domains and verified statutory types |
| 04 | `04-access-and-host` | Inserts permissions/navigation and the two approved Host administrators | Module, Operation, ModuleOperationMapping, HostRole, HostRoleModuleAndPermission, HostUser |
| 05 | `05-dependent-master` | Inserts employee identity catalogues after countries exist and synchronizes all identities | IdentityCategory, IdentityCategoryDocument, CountryIdentityRule |
| 99 | `99-verification` | Read-only acceptance assertions | two expected hosts, zero tenant/employee rows, permission/geography counts |

The authoritative per-table answer is
[`00-analysis/SEED_COVERAGE_MATRIX.csv`](00-analysis/SEED_COVERAGE_MATRIX.csv). It lists every
current table, its target row count, seed status, responsible script, and why an empty table is
empty. `00-analysis/post-seed-row-counts.csv` is the direct target reconciliation captured after
the run.

1. `01-clean/001-reset-all-data.sql`
2. `02-parent-master/001-shared-master-data.sql`
3. `03-geography/000-iso-country-catalog.sql`
4. `03-geography/001-four-country-postal-catalog.sql`
5. `03-geography/002-country-regulatory-master.sql`
6. `04-access-and-host/001-modules-operations-two-host-admins.sql`
7. `05-dependent-master/001-employee-identity-catalog.sql`
8. `99-verification/001-verify-canonical-seed.sql`

The reset explicitly truncates all 163 audited base tables with `RESTART IDENTITY CASCADE`.
Consequently every identity sequence is reset before seed insertion. Seed scripts then synchronize
their sequences to the inserted master IDs.

Final business-data baseline:

- no Tenant rows;
- no Employee or tenant transaction rows;
- exactly two active Host users: Deepesh Gupta and Sujeet;
- one active Host-Super-Admin role with persisted module-operation grants;
- shared reference, module/operation, geography and identity catalogues;
- exactly three active subscription plans;
- one active default Brevo SMTP configuration supplied at execution time.

## Current coverage boundary

The database currently has 164 tables (the canonical run creates
`TenantEmployeeSectionDefault`, increasing the pre-run inventory of 163 by one). After the target
run, 27 tables contain canonical seed data and 137 are empty.

Empty does not always mean missing. Tenant, employee, policy-definition, attendance, device
installation, payroll, ticket, asset, billing-transaction and audit tables are intentionally empty
because the requested baseline contains no tenant business data.

The following pre-reset platform/configuration tables are **not yet canonical seed data** and need
an explicit business decision before they can be added safely:

- `PaymentGateway` — stores environment-variable references and runtime gateway configuration.
- `HostBillingConfiguration` and `BillingTaxRule` — contain seller/tax business configuration.
- `DeviceMaster` — contains the supported physical-device catalogue.
- `DefaultEmailConfig` is seeded from the runtime-only `SKOLENZA_SEED_SMTP_KEY`; the secret is not
  copied into source control or execution logs.
- `License` — requires a defined production licensing rule.

Twenty additional empty master-like tables had no authoritative rows in the backup. They are marked
`UNSEEDED_MASTER_NO_SOURCE` in the coverage matrix. Values will not be invented for them.

The verified pre-reset backup is stored outside this run directory at
`DBFullBACKUP/workforcedb_34hi_duis-before-canonical-seed-20260928.dump`.

## Dependency model

The live-schema audit found 163 base tables and 278 foreign keys. The complete machine-readable
inventory is in `00-analysis/table-row-counts.csv`, `00-analysis/foreign-key-dependencies.csv`, and
`00-analysis/identity-sequences.csv`.

The operational parent-to-dependent order is:

1. Shared parents: country and location types, policy/status/type catalogues, operations,
   subscription plans, industries and other lookup masters.
2. Geography: Country → State → District → Locality.
3. Access parents: Operation and parent Module → child Module → ModuleOperationMapping → HostRole →
   HostRoleModuleAndPermission → HostUser.
4. Tenant graph, intentionally left empty: Tenant → TenantProfile/Location/Subscription/Role/
   Department/Designation/EmployeeType → Employee → employee work, attendance, leave, device,
   payroll and policy-dependent tables.
5. Authentication dependents: Employee → LoginCredential → RefreshToken; HostUser → RefreshToken.
6. Policy graph: PolicyType → Policy → PolicyVersion → configuration/rules/documents/approval and
   employee assignment/exception/acknowledgement/audit tables.
7. Device graph: DeviceMaster → TenantDevice → configuration/commands/messages/enrolments.
8. Ticket, asset, payroll, billing and bulk-import transaction graphs, intentionally left empty.

Foreign keys remain enabled during all seed inserts. Therefore a missing parent or incorrect order
causes the run to stop rather than inserting orphaned data.

## Duplicate-file decision

Historical migrations are retained as migration evidence. The old consolidated access seed,
four-country geography seed, and four overlapping employee identity reset/seed files were removed
after the canonical run passed both local-clone and target verification. Targeted module seeds that
serve independent migration workflows remain in place; they are not substitutes for this complete
reset sequence.
