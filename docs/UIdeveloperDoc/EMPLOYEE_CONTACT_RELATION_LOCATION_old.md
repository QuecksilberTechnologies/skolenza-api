# Employee contact relation options, initial row and location cascade

## Purpose and screen behavior

Employee contact relations are now owned by the backend enum instead of an
Angular constant. The Add/Edit Contact relation dropdown, Employee Add/Edit
emergency relation dropdown and relation display labels all consume the same
authenticated catalogue. `Owner` is included as relation value `16`.

When an employee is created through the existing Employee create workflow, the
same transaction also creates one editable `EmployeeContact` placeholder. It
copies only the employee full name and selected `CountryId`. Relation, contact
type, phone/email, state, district, locality and address fields remain null.
The employee can complete that row later or create additional contact rows.

The Contact form location order is:

```text
Country -> State -> District -> Locality
```

Changing a parent clears every dependent selection and loads only the next
level. Edit mode loads all levels needed by the saved IDs.

## Authentication and permissions

`GET /api/Employee/Contact/relation-options` requires a bearer token. It accepts
no `ModuleId`, `OperationId`, employee ID, query string or body. The Employee
permission behavior narrowly bypasses tenant/user database validation and
module-operation persistence for this constants lookup only. The existing
ASP.NET bearer authentication validates the token, as on `Role/type-options`.
No tenant/employee claim or active-login database lookup is required. Angular explicitly
excludes this route from permission-ID injection.

Existing Contact create/read/update/delete routes retain the established
Employee permission pipeline. The UI must resolve their current ModuleId and
OperationId dynamically from the authenticated menu; numeric examples must
never be hard-coded.

## Relation options endpoint

```http
GET /api/Employee/Contact/relation-options
Authorization: Bearer <redacted>
```

Representative success response:

```json
{
  "isSucceeded": true,
  "message": "Contact relation options retrieved successfully.",
  "errors": [],
  "data": [
    { "id": 1, "code": "FATHER", "label": "Father" },
    { "id": 2, "code": "MOTHER", "label": "Mother" },
    { "id": 3, "code": "HUSBAND", "label": "Husband" },
    { "id": 4, "code": "WIFE", "label": "Wife" },
    { "id": 5, "code": "SON", "label": "Son" },
    { "id": 6, "code": "DAUGHTER", "label": "Daughter" },
    { "id": 7, "code": "BROTHER", "label": "Brother" },
    { "id": 8, "code": "SISTER", "label": "Sister" },
    { "id": 9, "code": "GRANDFATHER", "label": "Grandfather" },
    { "id": 10, "code": "GRANDMOTHER", "label": "Grandmother" },
    { "id": 11, "code": "UNCLE", "label": "Uncle" },
    { "id": 12, "code": "AUNT", "label": "Aunt" },
    { "id": 13, "code": "COUSIN", "label": "Cousin" },
    { "id": 14, "code": "GUARDIAN", "label": "Guardian" },
    { "id": 15, "code": "LANDLORD", "label": "Landlord" },
    { "id": 16, "code": "OWNER", "label": "Owner" },
    { "id": 99, "code": "OTHER", "label": "Other" }
  ]
}
```

Missing/invalid bearer tokens return HTTP `401 Unauthorized` before the handler.
The isolated HTTP tests observe this status; a JSON error body is not guaranteed
by the bearer challenge. Consumers must handle the status even with an empty body.

## Location lookup sequence

The existing location lookup endpoints and their authentication behavior remain unchanged:

| Selection | Method and route | Required input | UI action |
| --- | --- | --- | --- |
| Country | `GET /api/Location/Country/option` | existing lookup request context | Populate Country |
| State | `GET /api/Location/State/option` | `CountryId` | Clear State/District/Locality, load States |
| District | `GET /api/Location/District/option` | `StateId` | Clear District/Locality, load Districts |
| Locality | `GET /api/Location/Locality/option` | `DistrictId` | Clear Locality, load Localities |

The Angular `LookupStore` is the single state source for all four levels. A
failed parent lookup leaves its dependent option list empty. There is no polling,
retry job, cancellation endpoint or report download in this flow.

Every location request also supplies `TodaysDate` as an ISO timestamp; existing
`UserEmployeeId` context is optional. Example query:
`GET /api/Location/State/option?CountryId=<selected-country-id>&TodaysDate=2026-10-01T12%3A00%3A00Z`.
Use `StateId` or `DistrictId` for subsequent levels, not fixed environment IDs.

### UAE catalogue

The local database originally contained **zero UAE states**. The approved public
catalogue is now in `database-scripts/SeedUaeLocations.sql`: 7 emirates, 28 source
ADM2 districts/municipalities plus 7 explicitly labelled Unassigned District
groups, and 2,776 current populated places/ADM3 localities. It uses
[GeoNames AE export](https://download.geonames.org/export/dump/AE.zip), CC BY 4.0,
retrieved 2026-10-01. Emirate names/codes are checked against the
[UAE government](https://u.ae/en/about-the-uae/the-seven-emirates) and
[GeoNames subdivision table](https://www.geonames.org/AE/administrative-division-united-arab-emirates.html).

This is public reference data, not a claim of exhaustive official coverage.
Missing district membership is not guessed; five places without a known emirate
are omitted. PostalCode stays null. The seed preserves existing IDs/rows and
other countries. LocalityType uses the existing Other / Unclassified value 4.
Some source districts may have no mapped locality; unassigned places remain in
the explicitly labelled group. Do not silently assign them to a municipality.

## Contact create and update payload

Manual Add/Edit keeps the current Contact routes. Example create body (permission
IDs are injected from the authenticated menu, not copied from this example):

```json
{
  "userEmployeeId": "<session-employee-id>",
  "employeeId": "<target-employee-id>",
  "contactName": "Property Owner",
  "contactType": 2,
  "relation": 16,
  "contactNumber": "+971501234567",
  "alternateNumber": "",
  "email": "",
  "isPrimary": false,
  "countryId": 10,
  "stateId": 2,
  "districtId": 7,
  "localityId": 45,
  "houseNo": "12A",
  "landMark": "",
  "street": "Example Street",
  "address": "Example address",
  "remark": "",
  "description": ""
}
```

Contact type values remain `1 = Rental`, `2 = Permanent`, `3 = Temporary`.
All geography IDs above are illustrative and must come from the lookup chain.
Update uses `POST /api/Employee/Contact/update` with the same editable fields plus
`id`; its success `data` is a boolean. Create uses `POST /api/Employee/Contact/create`.
Contact update accepts the UI's international `+` number format (7–15 digits)
and preserves support for legacy ten-digit numbers. Selected-country validation
still runs in the UI. Malformed numbers return the existing validation error.
The Angular manual-contact form requires name, type, relation, contact number,
country, state, district, locality, house number and address. Alternate number,
email, landmark, street, description, remark and primary status are optional.
The API DTO keeps location IDs nullable so the automatically generated initial
row can exist before the employee completes it.

`GET /api/Employee/Contact/get` now returns nullable `localityId` and
`localityName` in addition to the existing location fields. Create and update
accept nullable `localityId`; the current UI sends the selected numeric ID.

## Persistence and transaction timing

- Table: `skolenza."EmployeeContact"`.
- Employee creation adds the placeholder to the new Employee aggregate before
  `CreateEmployeeAsync` saves the graph.
- The Employee, login, role, image and initial contact are saved inside the
  existing employee-creation transaction. A failure before commit rolls the
  graph back; no independent contact transaction is opened.
- Initial values: `ContactName = employee full name`, `CountryId = employee
  CountryId`, `Relation = null`; user-entered contact/address fields are null.
- Workflow metadata is initialized as active, editable, unverified and not
  primary. Existing employee authorization still controls later edits.
- `database-scripts/AddEmployeeContactDefaultAndLocality.sql` makes
  `ContactNumber` nullable, widens `ContactName` to 302 (three 100-character
  Employee name parts plus two spaces), and adds nullable
  `LocalityId`, its index and a `SET NULL` foreign key. The script is additive
  and idempotent and does not rewrite existing rows.

The migration and UAE seed were applied to local Development database
`localhost / skolenza_local_october_2026` on 2026-10-01. The seed was executed
twice: second run inserted zero State/District/Locality rows. Non-AE geography
row fingerprints were identical before/after. No existing employee was backfilled.
Apply the schema script before deploying the API elsewhere, and apply the UAE
seed after the country and locality-type masters. Deployed application is not verified.

## Verification status

- Backend Employee contact/profile suite: 45 passed, 0 failed, 0 skipped,
  including three isolated bearer HTTP cases (401 missing/invalid; 200 valid).
- Employee contact DB tests: 2 passed. Real employee-create/edit/add handlers,
  repository read-back and all seven UAE state cascades verified; test account
  graph rolled back. Token/encoding/email in the DB probe are synthetic.
- Backend locality contract suite applicable to this flow: 9 passed, 0 failed,
  0 skipped.
- Protected tenant-registration transaction gate: 14 passed, 0 failed, 0 skipped
  before and after the change.
- Protected tenant PostgreSQL rollback probe: 2 passed, 0 failed, 0 skipped; no
  test tenant was retained.
- Angular affected UI suite: 8 files and 66 tests passed.
- Angular development and production builds: passed; changed files also passed
  format and ESLint checks.
- Full running-product authenticated browser/HTTP and deployed acceptance remain
  pending. Isolated HTTP tests use a random test signing key, not real sessions.

Scenario evidence: [Employee contact relation and location cascade — 2026-10-01](../testing/employee/contact-relation-location/2026-10-01.md).
