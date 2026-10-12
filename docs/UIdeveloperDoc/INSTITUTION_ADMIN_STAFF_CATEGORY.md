# Staff Category catalogue and institution administrator assignment

## Behaviour

`StaffCategory` is a global database-backed catalogue. Target IDs are stable: `1 ADMINISTRATIVE`, `2 TEACHING`, `3 NON_TEACHING`, `4 SUPPORT`, `5 SPECIALIST`.

Both institution-creation entry points use the same backend registration handler and assign `StaffCategoryId = 1` (`ADMINISTRATIVE`). Self-registration and Host institution creation do not accept or display a category selector.

StaffMember create and official update require an active category. `StaffEmploymentTypeId` remains the institution-owned employment arrangement and is independent.

## Options endpoint

`GET /api/Staff/Category/options`

Authentication: bearer token. This global lookup takes no `ModuleId`, `OperationId`, staff ID or query parameters. Angular excludes only this path from permission-ID injection; all StaffMember write endpoints keep their established permission flow.

Response example:

```json
{"isSucceeded":true,"message":"Staff categories retrieved successfully.","errors":[],"data":[{"id":1,"code":"ADMINISTRATIVE","label":"Administrative","description":"Institution leadership, administration and office management staff."}]}
```

## StaffMember contracts

Create and official-update bodies include `staffCategoryId`. Create, detail and paged
StaffMember responses return the persisted `staffCategoryId`; Angular may bind that value
directly when opening an existing StaffMember for edit. Example fragment:

```json
{"staffEmploymentTypeId":12,"staffCategoryId":2,"departmentId":4,"designationId":8}
```

Angular loads options through `StaffCategoriesAPI` and `LookupStore`; the StaffMember manage dialog binds `staffCategoryId`. Numeric values must not be copied into component constants.

## Persistence and migration

`skolenza."StaffCategory"` owns label, description, display order and active state. `skolenza."StaffMember"."StaffCategoryId"` has a check, index and restrictive FK.

The one-time legacy mapping is: `1→2`, `2→3`, `3 Technical→5 Specialist`, `4→1`, `5→4`. The migration detects an existing central catalogue so a second run does not remap target values.

## Validation status

Local on 2026-10-12: migration applied twice; five active rows, three existing StaffMember rows all Administrative, FK/check present. The StaffMember create/detail/list projection gap was subsequently corrected so the persisted category is returned. Release backend build and Angular production build passed. Locked post-change gates and dedicated contract tests are recorded in the scenario report. Deployment verification remains separate.
