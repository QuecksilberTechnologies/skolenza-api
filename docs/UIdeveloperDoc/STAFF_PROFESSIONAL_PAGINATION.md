# Professional Structure pagination — API-only handoff

## Scope and permissions

The five institution Professional Structure tabs now reuse the existing
`QueryPagination.ToPageAsync`, `PaginationDefaults`, `PagedResponseDTO<T>` and
`ApiResponse.SuccessPaginated`. No Angular file, permission behavior, catalogue
seed or database schema was changed. No profile-completion metadata is added.

All requests require the existing institution bearer token. Resolve the current
`STAFF_DIRECTORY` leaf module and its `View` operation through authenticated
`GET /api/Navigation/my-menu` and the existing permission flow. Send those
`ModuleId` / `OperationId` values on every request. Do not hardcode IDs. Tenant
ownership comes from validated server context; Host login gets no tenant bypass.

## Routes

All routes are GET, relative to `/api/Staff/professional-structure`.

| Tab | Route | Existing filters |
| --- | --- | --- |
| Job Families | `/masters/JobFamily` (enum value 1) | `activeOnly` |
| Staff Categories | `/categories` | `activeOnly` |
| Capabilities | `/masters/Capability` (enum value 2) | `activeOnly` |
| Duties | `/masters/DutyDefinition` (enum value 3) | `activeOnly` |
| Positions | `/positions` | `activeOnly`, optional `institutionStaffCategoryMasterId` |

The three master names/values come from existing `StaffProfessionalMasterType`;
no new option catalogue was introduced. Mutation, deactivate/reactivate,
verification, assignment and single-staff-profile routes are unchanged.

## Inputs and examples

- Required: authenticated `ModuleId`, `OperationId` query context.
- `PageNumber`: optional integer, default 1; <=0 normalizes to 1.
- `PageSize`: optional integer, default 10; <=0 normalizes to 10. Positive custom
  sizes are honored under the existing helper; no new maximum-size rule was added.
- `activeOnly`: optional, default false; true restricts to active records.
- `institutionStaffCategoryMasterId`: optional on Positions only, applies before
  count and paging. It is the institution category ID, not the Host category type.
- Fixed order: `DisplayOrder`, `Name`, then unique `Id`, ascending. Inherited
  `SortBy`/`SortOrder` fields do not override this endpoint order.

Copyable requests with variables populated by authenticated login/menu:

```powershell
$query = "ModuleId=$moduleId&OperationId=$viewOperationId&PageNumber=2&PageSize=10&activeOnly=false"
Invoke-RestMethod -Headers @{Authorization="Bearer $accessToken"} `
  -Uri "$apiBase/Staff/professional-structure/masters/JobFamily?$query"
Invoke-RestMethod -Headers @{Authorization="Bearer $accessToken"} `
  -Uri "$apiBase/Staff/professional-structure/categories?$query"
Invoke-RestMethod -Headers @{Authorization="Bearer $accessToken"} `
  -Uri "$apiBase/Staff/professional-structure/masters/Capability?$query"
Invoke-RestMethod -Headers @{Authorization="Bearer $accessToken"} `
  -Uri "$apiBase/Staff/professional-structure/masters/DutyDefinition?$query"
Invoke-RestMethod -Headers @{Authorization="Bearer $accessToken"} `
  -Uri "$apiBase/Staff/professional-structure/positions?${query}&institutionStaffCategoryMasterId=$categoryId"
```

Representative Job Family response, illustrative record ID:

```json
{
  "isSucceeded": true,
  "message": "Professional master records retrieved successfully.",
  "data": [
    {
      "id": 501,
      "code": "LIBRARY",
      "name": "Library",
      "description": "Library operations and reading support.",
      "displayOrder": 50,
      "isActive": true,
      "requiresCertification": false,
      "certificationValidityRequired": false,
      "defaultFrequency": null,
      "requiresEvidence": false
    }
  ],
  "errors": [],
  "pageNumber": 2,
  "pageSize": 10,
  "totalRecords": 11,
  "totalPages": 2
}
```

The existing root `data` array and item fields remain; root `pageNumber`,
`pageSize`, `totalRecords`, `totalPages` are added. No nested paging envelope.
`completionPercentage`, `hasAllDocUploaded`, `isPrimaryMarked` are omitted.
An empty result has zero totals/pages. A page beyond the end returns `data: []`
with the real totals and requested page number. Invalid non-integer paging
inputs return normal model-validation 400 responses.

Each Category still includes its **complete** `staffJobFamilyMasterIds` and
`activeJobFamilyCount`, `activePositionCount`, `activeStaffCount`. Counts are not
limited to the displayed page. Every Position retains its complete capability
and duty requirements, including mandatory/optional flags. Certification,
evidence and default-frequency fields are preserved for the relevant masters.

## Errors, persistence and UI integration

Missing/invalid token: HTTP 401. Missing/wrong module-operation grant: 403.
Unsupported master type or malformed query: 400. Category filters with no
matching Position return an empty page. Existing exception middleware remains
authoritative; representative unsupported-type error:

```json
{
  "IsSucceeded": false,
  "Message": "Unsupported professional master type.",
  "Data": null,
  "Errors": ["Unsupported professional master type."],
  "ErrorCode": "VALIDATION_ERROR"
}
```

Error property casing follows the existing exception middleware; MVC success
responses use camelCase. GETs have no request JSON body/FormData, spreadsheet
contract, background job, polling or new workflow statuses. HTTP cancellation
propagates to EF. Retry transient network/server failures as appropriate, not
indefinite retries of 400/401/403. Offset paging is stable for an unchanged data
set, but count and page are separate reads under existing database isolation;
concurrent edits can move records between requests.

Queries filter tenant ownership, soft-delete state, active flag and optional
Position category before SQL count/offset/limit. Existing master/mapping/staff
tables are reused. Only the selected Position page is projected with its full
requirements. No persistence migration or page cache is introduced. Unpaged
internal repository reads remain available for existing seed/save/profile flows,
so a save response for an item after page 1 is not lost.

**Angular was deliberately untouched.** The UI developer must use root totals
and request subsequent pages for each tab. Reset page number on filter/category
change. Existing selectors using these list APIs must also account for paging;
they must not assume the first 10 records are the entire catalogue. Browser
navigation/selector completeness is pending UI integration, not proven by these
backend tests. Render deployment has not been performed or accepted.

Local verification and exact build/test results:
[2026-10-18 scenario](../testing/staff/professional-pagination/2026-10-18.md).
