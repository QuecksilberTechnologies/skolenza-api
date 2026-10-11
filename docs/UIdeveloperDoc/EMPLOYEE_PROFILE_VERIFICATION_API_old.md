# Employee profile verification API handoff

Status: local implementation and automated tests passed on 2026-09-13; deployment is still required.

## Read sections

Call authenticated `GET /api/Employee/get-all-percentage?EmployeeId={encodedEmployeeId}&ModuleId={id}&OperationId={id}`. Resolve numeric permission IDs from the authenticated menu; do not hard-code example IDs.

```json
{
  "sectionName": "Bank",
  "completionPercent": 100,
  "isInfoVerified": false,
  "isEditAllowed": false,
  "isSectionCreate": true,
  "tabInfoType": 2,
  "canUpdateVerificationStatus": true
}
```

Only enable a verification control when `canUpdateVerificationStatus` is `true`. Mapping: Overview=1, Bank=2, Contact=3, Experience=4, Identity=5, Education=6, Dependent=7.

Insurance, Work Locations, Devices, Work Arrangement, Work Pattern and Overrides return `tabInfoType: null` and `canUpdateVerificationStatus: false`. Do not send these completion-only rows to the update API.

## Update sections

Call authenticated `POST /api/Employee/update-bulk`.

```json
{
  "moduleId": 8,
  "operationId": 2,
  "employeeId": "encoded-employee-id",
  "isActive": true,
  "sections": [
    { "tabInfoType": 1, "isVerified": true, "isEditAllowed": false },
    { "tabInfoType": 2, "isVerified": false, "isEditAllowed": true }
  ]
}
```

`moduleId`, `operationId`, `employeeId`, `sections` and every `sections[].tabInfoType` are mandatory. `sectionName` is neither accepted nor required. Each `tabInfoType` may occur once. Unsupported, missing/default `0`, duplicate, or Insurance `8` values return HTTP 400.

```json
{
  "isSucceeded": true,
  "message": "Section status updated successfully.",
  "data": true,
  "errors": []
}
```

Permission denial for an authenticated user is HTTP 403. Missing, invalid, expired, or stale authentication is HTTP 401. The endpoint updates `IsInfoVerified` and `IsEditAllowed` in the corresponding Employee/profile table in one transaction; a failure rolls back the request.

