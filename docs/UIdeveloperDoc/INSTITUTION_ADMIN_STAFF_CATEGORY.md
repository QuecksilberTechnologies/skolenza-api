# Institution administrator staff category

## Behavior

Both institution-creation entry points use the same registration handler:

- self-registration
- Host administrator creation

The API creates the institution's initial staff account with
`StaffCategoryId = 4` (`Administrative`). This is trusted server-side behavior.
The institution registration requests do not accept `StaffCategoryId`, and the
Angular self-registration and Host create-institution forms must not show a
staff-category field for this account.

`EmployeeTypeId` remains the employment arrangement and is currently created
as the tenant-specific onboarding `Permanent` type. It is independent of the
staff category.

## Persistence

`skolenza."Employee"."StaffCategoryId"` is a nullable `smallint` constrained to
the shared values 1–5. Existing initial administrator rows are backfilled to 4.
The column remains nullable for legacy and ordinary staff-creation paths until
their dedicated category selection contract is migrated; institution
administrators are always assigned 4 by the API.

## Validation status

Local validation on 2026-10-11:

- both existing initial administrators: `StaffCategoryId = 4`
- locked Host/self registration regression: 14 passed
- Angular self/Host institution form regression: 70 passed
- Release API build completed as part of the test run

Deployment verification remains separate.
