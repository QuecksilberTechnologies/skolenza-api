# Locality and Locality Type API

## Purpose

The geographic selection flow is now:

`Country -> State -> District -> Locality (City/Town/Village/Other)`

`City` has been renamed to the generic `Locality` model. A locality always belongs
to one District and one LocalityType. Existing catalog rows are preserved and
classified as `City`. Postal source rows whose source does not reliably classify
the settlement use `Other / Unclassified`; the API never guesses their type.

## Lookup sequence

All examples use illustrative values. Send the current UTC time in `TodaysDate`.

1. `GET /api/Location/country/option?TodaysDate=2026-09-16T10:00:00Z`
2. `GET /api/Location/State/option?CountryId=1&TodaysDate=2026-09-16T10:00:00Z`
3. `GET /api/Location/District/option?StateId=22&TodaysDate=2026-09-16T10:00:00Z`
4. `GET /api/Location/Locality/option?DistrictId=501&TodaysDate=2026-09-16T10:00:00Z`
5. `GET /api/Location/LocalityType/option?TodaysDate=2026-09-16T10:00:00Z`

These option endpoints follow the existing Location controller authentication
behavior. They do not accept ModuleId/OperationId. Do not invent or hard-code
permission IDs for these routes.

## Locality option

### Mandatory query fields

| Field | Type | Rule |
| --- | --- | --- |
| `DistrictId` | integer | Must identify an active District. |
| `TodaysDate` | ISO-8601 datetime | Required by the existing option request contract. |

`UserEmployeeId` remains optional because it is inherited from the common option
request contract and is not used to filter this master lookup.

Example response:

```json
{
  "isSucceeded": true,
  "message": "Localities fetched successfully.",
  "data": [
    {
      "id": 501,
      "districtId": 501,
      "stateId": 22,
      "localityTypeId": 1,
      "localityName": "Mumbai",
      "localityCode": "IN-MH-MUMS-MUMBAI",
      "postalCode": "400001",
      "localityTypeName": "City",
      "isActive": true
    }
  ],
  "errors": []
}
```

An invalid or inactive District returns the established not-found error response.
A missing/zero DistrictId or missing TodaysDate returns the established validation
error response.

## Locality Type option

Mandatory query field: `TodaysDate` as an ISO-8601 datetime.

Example response:

```json
{
  "isSucceeded": true,
  "message": "Locality types fetched successfully.",
  "data": [
    { "id": 1, "typeName": "City" },
    { "id": 2, "typeName": "Town" },
    { "id": 3, "typeName": "Village" },
    { "id": 4, "typeName": "Other / Unclassified" }
  ],
  "errors": []
}
```

## Tenant location payload change

Tenant-location create, update, filter, and response contracts use `districtId`,
`localityId`, and `localityName`. The selected Locality must belong to the selected
District; the District must belong to the selected State; the State must belong to
the selected Country.

```json
{
  "countryId": 1,
  "stateId": 22,
  "districtId": 501,
  "localityId": 501,
  "locationCode": "MUM-HQ",
  "locationName": "Mumbai Head Office",
  "locationType": 1,
  "timeZoneId": "Asia/Kolkata"
}
```

## Persistence and rollout

- `skolenza.LocalityType`: permanent master rows City, Town, Village, and Other / Unclassified.
- API code uses `LocalityTypeConstants` as the canonical identifiers and names:
  `1/City`, `2/Town`, `3/Village`, `4/Other / Unclassified`; the lookup returns matching active DB rows.
- `skolenza.Locality`: renamed data-preserving City catalog; contains DistrictId
  and LocalityTypeId foreign keys plus LocalityCode and PostalCode.
- `database-scripts/production-seed/03-geography/001-four-country-postal-catalog.sql` adds GeoNames postal records without
  deleting or renumbering existing rows. Target DB counts after execution:
  India 155,545; China 2,352; Germany 23,296; United States 41,490.
- `postalCode` was previously null because the old 8,333-row city catalogue had
  no postal-code field. It is populated only when the source supplies a value;
  the migration does not fabricate postal codes.
- `skolenza.State.StateCode`, `skolenza.District.DistrictCode`, and
  `skolenza.Locality.LocalityCode` are required stable codes. `District.PinCode`
  has been removed; postal ownership is on Locality.
- `skolenza.TenantLocation`: stores DistrictId and the selected locality reference.
  During the deployment transition, the physical legacy column remains `CityId`
  while the API property is `LocalityId`.
- A compatibility `skolenza.City` view keeps the older deployed API readable until
  the updated build is deployed.

There is no upload, polling, cancellation, retry job, file storage, or retention
window for these synchronous lookup endpoints.

## Verification status

Target migrations, local build, 10/10 focused tests, and DB-backed local endpoint smoke
passed on 2026-09-16. The updated API build has not yet been verified as deployed;
deployed API acceptance remains pending.
