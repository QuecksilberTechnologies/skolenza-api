# Tenant initial location lifecycle

## Behavior

Every newly created Tenant now receives an initial `TenantLocation` in the same database transaction.

- Self registration supplies only the Tenant country. The API creates the minimum valid location with `LocationCode = PRIMARY`, `LocationName = CompanyName`, Head Office type, `TimeZoneId = UTC`, and no address/state/district/locality values.
- Host onboarding persists the complete `InitialLocation` submitted by the Host UI.
- Host aggregate update modifies the location selected by the UI. If its country is omitted, the final Tenant country is used.
- The simpler Host update synchronizes the canonical active location: Head Office is preferred, otherwise the oldest active location. It also creates the minimum valid row when updating a legacy Tenant with no location.

Changing the Tenant country clears an initial location's State, District and Locality unless the aggregate update explicitly supplies valid replacements. This prevents old-country geography from remaining attached to the new country.

## Authentication and permissions

`POST /api/Tenant/create-tenant` remains anonymous. Host create and update routes require a bearer token and retain the existing permission pipeline. The UI must discover the current Module and Operation IDs through the authenticated menu/permission response and send them in the route's existing query/body contract. Numeric IDs must not be hardcoded.

## Routes

| Method and route | Purpose | Location behavior |
| --- | --- | --- |
| `POST /api/Tenant/create-tenant` | Self registration | Creates a minimum valid initial location from Tenant country and company name. |
| `POST /api/Tenant/new-tentant-creation-by-host` | Host onboarding | Creates the supplied complete `InitialLocation`. |
| `PUT /api/Tenant/new-tenant-update-by-host/{encryptedTenantId}` | Host aggregate edit | Updates `SelectedLocation`; omitted country inherits Tenant country. |
| `PUT /api/Tenant/{encryptedTenantId}` | Narrow Host edit | Synchronizes the canonical initial location. |
| `POST /api/Tenant/update-tenant` | Existing Angular-compatible Host edit | Uses the same canonical synchronization as the narrow Host edit. |

## Request examples

Illustrative self-registration JSON (other existing mandatory fields remain required):

```json
{
  "subscriptionPlanId": 1,
  "tenantIndustryId": 9,
  "companyName": "Northwind Services Pvt. Ltd.",
  "tenantCode": "NORTHWIND",
  "companyEmailDomain": "northwind.example",
  "genderId": 1,
  "tenantEmail": "admin@northwind.example",
  "contactPersonName": "Tenant Administrator",
  "contactNumber": "+91-0000000000",
  "countryId": 101
}
```

The API derives this location; the UI does not send it:

```json
{
  "locationCode": "PRIMARY",
  "locationName": "Northwind Services Pvt. Ltd.",
  "locationType": 1,
  "countryId": 101,
  "stateId": null,
  "districtId": null,
  "localityId": null,
  "timeZoneId": "UTC",
  "isGeoFenceEnabled": false,
  "isAttendanceAllowed": false,
  "isBiometricEnabled": false
}
```

Illustrative Host onboarding fragment:

```json
{
  "companyName": "Northwind Services Pvt. Ltd.",
  "countryId": 101,
  "initialLocation": {
    "locationCode": "HQ",
    "locationName": "Mumbai Head Office",
    "locationType": 1,
    "stateId": 22,
    "districtId": 405,
    "localityId": 9001,
    "address": "Business District",
    "postalCode": "400001",
    "timeZoneId": "Asia/Kolkata",
    "isGeoFenceEnabled": true,
    "isAttendanceAllowed": true,
    "isBiometricEnabled": false
  }
}
```

Illustrative selected-location update fragment:

```json
{
  "countryId": 101,
  "selectedLocation": {
    "tenantLocationId": 25,
    "locationName": "Mumbai Head Office",
    "stateId": 22,
    "districtId": 405,
    "localityId": 9001,
    "timeZoneId": "Asia/Kolkata"
  }
}
```

`tenantLocationId` must belong to the Tenant being edited. The API validates the country/state/district/locality hierarchy. Invalid ownership or geography returns the established unsuccessful `ApiResponse` contract; raw exception details are not returned.

Representative response shape:

```json
{
  "isSucceeded": true,
  "message": "Tenant updated successfully.",
  "data": {
    "id": "<encrypted-tenant-id>",
    "companyName": "Northwind Services Pvt. Ltd.",
    "countryId": 101
  },
  "errors": []
}
```

## Persistence and isolation

`Tenant` and `TenantLocation` are written atomically during registration. Host aggregate updates also use the existing transaction. The narrow update stages the Tenant and canonical location in one EF `SaveChangesAsync`. Tenant ownership filters prevent one Tenant from selecting or updating another Tenant's location.

No polling, retry or cancellation UI was added. Existing cancellation tokens and registration error handling remain in place. No Angular source was changed for this backend requirement.

## Verification status

- Local command/contract tests: PASS, 15/15.
- Local PostgreSQL transaction rollback probes: PASS, 2/2; no records retained.
- Protected Role mapping regression: PASS, 15/15.
- Deployed API and Render database acceptance: PENDING. Passing local tests do not establish deployment acceptance.

Detailed evidence: [Tenant initial location lifecycle scenario](../testing/tenant/initial-location-lifecycle/2026-10-04.md).
