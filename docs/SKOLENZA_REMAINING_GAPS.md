# Skolenza remaining gaps

Last reviewed: 2026-10-11 (local development environment)

## Completed naming alignment

The active Angular source, API runtime source, current seed/deployment SQL and
live `SkolenzaDB` schema contain no remaining `Tenant` or `Employee` business
names. Historical `_old.md` references, forward migrations, rollback scripts
and recoverable backups retain legacy terms only where they describe or reverse
the original names.

## Open gaps

### 1. Environment-dependent automation coverage

- The latest complete backend run reported 701 passed, 0 failed and 99 skipped.
- Skipped cases include opt-in PostgreSQL probes, authenticated live API flows,
  worker/concurrency scenarios and environment-controlled integration tests.
- A skipped test is not accepted as a pass. Each test must be enabled with its
  documented prerequisite and executed before the corresponding scenario can
  be marked complete.

### 2. Institution-registration end-to-end acceptance

- Host-created and self-registered Institution flows have passing focused code
  regression coverage.
- A fresh authenticated browser-to-API-to-database run of both flows is still
  required after the completed naming migration.
- The final check must confirm the Institution, initial Campus, initial
  Administrative StaffCategory, StaffEmploymentType, account, role,
  subscription and permission records without partial persistence.

### 3. APIs awaiting an approved implementation contract

The Angular application currently reports these capabilities as unavailable
instead of sending invalid or nonexistent requests:

- staff-specific permission read/write;
- staff asset assignment;
- staff insurance.

Their request models, permission operations, persistence rules and expected UI
behavior still require an approved contract before implementation. Existing
role-level or unrelated endpoints must not be substituted.

### 4. Build and dependency warnings

- The Release build completes with zero errors, but existing compiler warnings
  remain, including nullability and malformed XML-documentation warnings.
- NuGet reports known advisories for the currently referenced AutoMapper,
  MailKit and Microsoft.OpenApi versions.
- Package upgrades require focused regression testing because these packages
  affect mapping, email and Swagger/OpenAPI behavior.

### 5. Deployment acceptance

- Current validation is local. It does not prove that a testing or production
  server has the same API build, Angular build, seed data or database schema.
- Apply the current scripts in deployment order, run schema assertions, then
  repeat authenticated smoke and registration checks on the target server.

### 6. SMTP provider IP authorization

- The AxionPro email-template catalogue and Host default SMTP configuration are
  now copied into local `SkolenzaDB`.
- MailKit reached the configured Brevo server, but authentication from the local
  machine was rejected with `525 5.7.1 Unauthorized IP address`.
- The approved local/testing-server outbound IP must be authorized in Brevo,
  followed by a real delivery test. Database presence alone is not treated as
  successful mail delivery.

## Current verified baseline

| Check | Result |
| --- | --- |
| Angular complete suite | 2,466 passed, 0 failed |
| Backend complete run | 701 passed, 0 failed, 99 skipped |
| .NET Release build | 0 errors; existing warnings remain |
| Angular TypeScript check | Passed |
| Active Angular legacy-name audit | 0 files |
| API runtime legacy-name audit | 0 files |
| Current non-migration SQL legacy-name audit | 0 files |
| Live DB legacy-name audit | 0 tables, columns, routines, constraints or indexes |

Detailed naming-migration evidence is recorded in
[`testing/reconstruction/angular-api-sql-naming-alignment/2026-10-11.md`](testing/reconstruction/angular-api-sql-naming-alignment/2026-10-11.md).
