# Institution ownership and campus options

The Host Institution create/edit form must obtain both option lists from the API. The Angular client must not keep a second hard-coded list.

## Endpoints

- `GET /api/InstitutionType/category-options`
- `GET /api/InstitutionType/campus-type-options`

Both return the standard `ApiResponse` envelope. Each `data` item has:

```json
{
  "id": 3,
  "code": "PRIVATE",
  "name": "Private",
  "description": "Privately owned and managed institution."
}
```

The ownership dropdown sends `name` as `profile.businessType`. The create and update handlers validate nonblank values against the backend constant list. The UI requires a selection; the API still accepts a blank value from older callers for backward compatibility.

The campus dropdown sends `id` as `initialLocation.locationType` or the Campus request `locationType`. Campus IDs deliberately reuse the existing `TenantLocationType` numeric values so attendance and location contracts stay compatible.

## Persistence contract

| UI value | Request property | Database column | PostgreSQL type |
|---|---|---|---|
| Institution ownership name | `profile.businessType` | `skolenza."TenantProfile"."BusinessType"` | `varchar(100)` |
| Campus type ID | `initialLocation.locationType` / `locationType` | `skolenza."Campus"."LocationType"` | `smallint` |

No schema migration is required. Create Institution persists both values through the existing transaction.

## Current lists

Ownership: Government, Government Aided, Private, Autonomous, Trust / Society, Public-Private Partnership, Other.

Campus type: Main Campus, Branch Campus, Academic Campus, Administrative Campus, Virtual / Remote Campus.

## Verification

- Backend option/type contract: 2/2 passed locally.
- Angular service, Institution form and Campus form: 34/34 passed locally.
- Angular production build: passed locally.
- Host registration locked gate: 14/14 passed locally.
- Configured PostgreSQL rollback probe: 0/2 because the local database does not contain subscription plan ID 2. This is an environment data prerequisite failure, not a persistence assertion pass.

