# Employee country-driven identity — working flow and data ownership

## Current status

- Feature implementation: available in the current backend and Angular source.
- Catalogue model: data-driven; the UI must not hard-code Aadhaar, PAN, NINO,
  Passport or any other country document.
- Runtime read implementation: Entity Framework queries.
- Local cleanup: **COMPLETE**. Both obsolete functions were removed from
  `workforcedb_local_20260928`, and their definitions were removed from the
  project source on 2026-09-30.
- Render cleanup: **PENDING**. Render PostgreSQL remained suspended and closed
  the SSL connection, so its database was not changed.

## Business flow

1. Employee creation stores the employee's selected `CountryId` in `Employee`.
2. The Identity screen sends the selected encoded `EmployeeId` to
   `GET /api/Employee/Sensitive/get`.
3. The backend validates the authenticated tenant request, uses the existing
   employee-data access pipeline, decodes the employee ID, and loads the
   tenant-scoped employee.
4. The backend uses the employee's persisted `CountryId`. The client-supplied
   `CountryNationalityId` is retained only as a legacy request field and does
   not decide eligibility.
5. Active `CountryIdentityRule` rows select the documents configured for that
   country. The query joins the active document and category masters and
   left-joins any saved employee identity value.
6. Angular renders exactly the rows returned by the API. For example, a UK
   employee receives NINO/Passport configuration and must not receive Indian
   Aadhaar/PAN configuration.
7. On save, `POST /api/Employee/Sensitive/create` checks permission and employee
   access again, verifies that every submitted document is allowed for the
   employee's persisted country, uploads an optional file, and saves the
   employee-specific values in one transaction. An existing active row for the
   same employee and document is updated; it is not duplicated.
8. If persistence fails, the database transaction is rolled back and files
   uploaded during that request are removed.
9. Delete uses `DELETE /api/Employee/Sensitive/delete`, soft deletes the saved
   `EmployeeIdentity` row, and removes its stored file after the database commit.

## API contract

### Read identity catalogue and saved values

`GET /api/Employee/Sensitive/get`

Query inputs:

| Field | Requirement | Purpose |
| --- | --- | --- |
| `EmployeeId` | Optional for self; required when viewing another employee | Tenant-salted encoded employee identifier |
| `CountryNationalityId` | Required legacy field | Compatibility only; persisted `Employee.CountryId` controls the response |
| `ModuleId` | Required | Resolve dynamically through the authenticated permission/menu flow |
| `OperationId` | Required | Resolve the View operation dynamically |

The response contains country code/name, category, document master ID/code/name,
description, mandatory flag, saved value/status/dates, and per-row completion.
The response envelope also contains the aggregate completion percentage.

### Save employee identity values

`POST /api/Employee/Sensitive/create` using `multipart/form-data`.

Each `Identities` item carries encoded `EmployeeId`,
`IdentityCategoryDocumentId`, `IdentityValue`, document code, optional file,
optional effective dates, plus dynamically resolved permission context. The
server rejects a document that is not configured for the employee's country.

The same route is the create/update upsert contract. The employee and document
master pair identifies an existing row. Verified or edit-locked rows can only be
changed through the existing admin/verification permission behavior.

### Delete a saved identity

`DELETE /api/Employee/Sensitive/delete`

Illustrative request body (permission IDs must be resolved dynamically):

```json
{
  "employeeIdentityId": 91,
  "moduleId": 100,
  "operationId": 4
}
```

Success response:

```json
{
  "isSucceeded": true,
  "message": "Identity deleted successfully.",
  "data": true,
  "errors": []
}
```

The API returns validation/not-found/forbidden errors for an invalid ID, missing
record, cross-employee access, or a verified/edit-locked record.

## Tables used by this feature

The complete runtime flow uses **6 tables**. Four are catalogue/geography
masters and two are employee/business tables.

| Table | Type | Read/write behavior | Data owned by this flow |
| --- | --- | --- | --- |
| `Country` | Seeded geography master | Read | Country ID, ISO code and name used by the employee and country rule |
| `Employee` | Transaction/business | Read; created by Employee flow | Target employee, tenant ownership and persisted `CountryId` |
| `IdentityCategory` | Seeded identity master | Read | Category code/name/description, for example government or tax identity |
| `IdentityCategoryDocument` | Seeded identity master | Read | Document code/name/description, uniqueness and active state |
| `CountryIdentityRule` | Seeded identity mapping | Read | Country-to-document mapping, mandatory flag and active state |
| `EmployeeIdentity` | Transaction/business | Read and write | Employee value, optional file metadata, verification/edit flags, effective dates and audit fields |

### Where data is inserted

- Feature setup/seed inserts master data into `Country`, `IdentityCategory`,
  `IdentityCategoryDocument`, and `CountryIdentityRule`.
- Employee onboarding inserts the employee and selected country into `Employee`.
- Identity submission inserts employee-specific data only into
  `EmployeeIdentity`; the three identity catalogue tables are not modified by
  an employee save.
- An uploaded binary is stored through the configured file-storage service;
  only its generated file name/path is stored in `EmployeeIdentity`.

## Seed requirement

**4 of the 6 runtime tables require master seed data:**

1. `Country` — ISO country catalogue; seeded before identity mappings.
2. `IdentityCategory` — identity classification masters.
3. `IdentityCategoryDocument` — supported document masters.
4. `CountryIdentityRule` — allowed/mandatory document mappings per country.

`Employee` and `EmployeeIdentity` must not receive fabricated seed employees or
identity numbers. They are populated through real application workflows. The
canonical identity seed currently expects 249 countries, 4 identity categories,
12 document masters and 260 country-document rules. Counts must be verified
again after Render resumes before recording them as the current production state.

## Relationships

```text
Country (1) ────────< CountryIdentityRule >──────── (1) IdentityCategoryDocument
   ^                                                        |
   |                                                        v
Employee (1) ──────< EmployeeIdentity             IdentityCategory
```

Required foreign keys:

1. `Employee.CountryId -> Country.Id`
2. `CountryIdentityRule.CountryId -> Country.Id`
3. `CountryIdentityRule.IdentityCategoryDocumentId -> IdentityCategoryDocument.Id`
4. `IdentityCategoryDocument.IdentityCategoryId -> IdentityCategory.Id`
5. `EmployeeIdentity.EmployeeId -> Employee.Id`
6. `EmployeeIdentity.IdentityCategoryDocumentId -> IdentityCategoryDocument.Id`

## Legacy objects pending removal

The following PostgreSQL functions were superseded by the current EF read path:

- `skolenza."GetCountryIdentityDocument"(integer)`
- `skolenza."GetEmployeeIdentityByCountryRule"(bigint, integer, boolean)`

Their definitions have been removed from
`skolenza.application/DTOS/SPFunctions.txt`. Both exact signatures were dropped
from the active local database after confirming zero external dependencies.
Current runtime source does not call them. The suspended Render database has not
been changed.

If Render is resumed later, perform this controlled sequence there:

1. Confirm Render API and PostgreSQL service are active.
2. Re-run repository references and `pg_proc` signature/dependency checks.
3. Preserve the four identity tables and all foreign keys listed above.
4. Confirm the project still contains no legacy function definitions.
5. Execute guarded `DROP FUNCTION IF EXISTS` statements for the exact signatures.
6. Verify both functions are absent and all six required tables still exist.
7. Run backend identity tests/build and Angular identity unit/Playwright tests.
8. Update the scenario report with actual local/deployed evidence. Do not mark
   the cleanup passed until database verification succeeds.

## Known failure diagnosis

- India documents shown for a non-India employee: verify that Angular sends the
  selected encoded `EmployeeId`; backend eligibility must use the persisted
  employee country.
- Empty document list: verify active `CountryIdentityRule`, document and
  category rows for the employee's country.
- Save rejected as not configured: submitted document master is absent or
  inactive in the employee-country rule.
- Completion remains zero: verify an active, non-soft-deleted
  `EmployeeIdentity` row exists for the returned document master.
- Database SSL connection closes unexpectedly: check Render service state
  before changing schema or declaring DB tests passed.

## Verification evidence

The latest completed feature evidence is recorded in
[`docs/testing/employee/country-identity/2026-09-30.md`](../testing/employee/country-identity/2026-09-30.md).
The table audit and deferred-cleanup status are recorded in
[`docs/testing/employee/identity-table-usage/2026-09-30.md`](../testing/employee/identity-table-usage/2026-09-30.md).
