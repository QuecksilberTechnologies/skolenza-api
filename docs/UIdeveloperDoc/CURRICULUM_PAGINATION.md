# Curriculum collection pagination — API-only handoff

## Behavior and ownership

Requested scope: reuse Staff list pagination, default page 1 / 10 records,
centralize query paging, independently page Topics, and make no Angular changes.
`QueryPagination.ToPageAsync` counts the filtered EF query and retrieves only
the requested page with SQL offset/limit. `PaginationDefaults` owns defaults.
The existing `PagedResponseDTO<T>` and `ApiResponse.SuccessPaginated` are reused.
No profile/document completion metadata is calculated or added.

Syllabus `completionPercent` and delivery topic coverage are existing business
data and remain unchanged. All selected topics of an individual lesson plan,
and all coverage entries of an individual delivery, remain complete. Independent
Topics paging applies to the unit's topic catalogue, not these record aggregates.

## Authentication and dynamic permissions

Send `Authorization: Bearer <access-token>`. Resolve the current `STAFF_DIRECTORY`
leaf module and its `View` operation through the existing authenticated navigation
API (`GET /api/Navigation/my-menu`) and permission flow. Send the resolved
`ModuleId` and `OperationId` on every collection request, including Topics.
Do not copy numeric permission IDs from another environment. The established
StaffMember institution permission behavior validates the token role and module
operation. Institution ownership comes from validated server context; this API
does not accept a caller-selected institution. Host login has no tenant bypass.

## Routes and ordering

All routes below are GET, relative to `/api/Staff/curriculum-delivery`.

| Route | Record being paged | Fixed deterministic order |
| --- | --- | --- |
| `/frameworks` | Framework | Name, Id ascending |
| `/frameworks/{frameworkId}/units` | Unit header | Sequence, Name, Id ascending |
| `/units/{unitId}/topics` | Topic belonging to that unit | Sequence, Name, Id ascending |
| `/lesson-plans` | Lesson plan, complete selected topics | PlannedDate, Id descending |
| `/lesson-deliveries` | Delivery log, complete topic coverage | DeliveredDate, Id descending |
| `/syllabus-progress` | Unit progress | CurriculumUnitId ascending |

`/workflow-statuses` and `/teaching-assignments` remain existing unpaged lookup
APIs. Write, review and delete routes retain their existing contracts.

## Query inputs

| Input | Required | Behavior |
| --- | --- | --- |
| `ModuleId` | Yes | Current authenticated module ID, existing permission flow |
| `OperationId` | Yes | Current View operation ID |
| `PageNumber` | No | Integer; omitted or <=0 becomes 1 |
| `PageSize` | No | Integer; omitted or <=0 becomes 10; positive sizes are honored |
| `courseOfferingId` | No, progress route only | Existing offering filter is applied before count/page |
| `frameworkId` / `unitId` | Required on respective path | Institution-owned, non-deleted parent ID |

Inherited `SortBy`/`SortOrder` fields do not change these fixed endpoint orders.
Malformed/non-integer inputs produce normal model-validation HTTP 400 responses.
No new maximum page-size rule has been introduced. Page numbering is one-based.
Empty filters yield `data: []`, `totalRecords: 0`, `totalPages: 0`. A page after
the last returns an empty array with the real totals and requested page number.

Copyable PowerShell request using session variables populated by login/menu:

```powershell
$query = "ModuleId=$moduleId&OperationId=$viewOperationId&PageNumber=2&PageSize=10"
Invoke-RestMethod -Headers @{ Authorization = "Bearer $accessToken" } `
  -Uri "$apiBase/Staff/curriculum-delivery/frameworks/$frameworkId/units?$query"
Invoke-RestMethod -Headers @{ Authorization = "Bearer $accessToken" } `
  -Uri "$apiBase/Staff/curriculum-delivery/units/$unitId/topics?$query"
```

Use the same query on `frameworks`, `lesson-plans`, `lesson-deliveries`, and
`syllabus-progress`; append `&courseOfferingId=$courseOfferingId` for filtered
progress. These GET endpoints have no JSON body, FormData, Excel/CSV import,
polling job or workflow status transition.

Representative Topics response (illustrative record IDs):

```json
{
  "isSucceeded": true,
  "message": "Curriculum topics retrieved successfully.",
  "data": [
    {
      "id": 801,
      "curriculumUnitId": 80,
      "code": "FRACTIONS_COMPARE",
      "name": "Compare fractions",
      "description": "Compare fractions using a number line.",
      "sequence": 11,
      "plannedPeriods": 2,
      "learningOutcome": "Order fractions with equal denominators.",
      "isActive": true
    }
  ],
  "errors": [],
  "pageNumber": 2,
  "pageSize": 10,
  "totalRecords": 11,
  "totalPages": 2
}
```

The root `data` remains an array. The root totals are named `totalRecords` and
`totalPages`; do not expect nested `data.data` or a `totalCount` root property.
`completionPercentage`, `hasAllDocUploaded`, and `isPrimaryMarked` are omitted.
Paged Unit items contain their existing fields **without `topics`**. Fetch the
requested topic page using that unit's `id`; do not interpret an absent `topics`
field as an empty unit. Single-record Unit save responses retain nested topics.

## Errors, cancellation and navigation

Existing authentication, permission and centralized error handling apply.
Missing/invalid token returns 401; missing/wrong module-operation grant returns
403. A nonexistent/deleted/other-institution framework or unit returns 404.
Representative application error body:

```json
{
  "IsSucceeded": false,
  "Message": "The requested resource was not found.",
  "Data": null,
  "Errors": [],
  "ErrorCode": "NOT_FOUND"
}
```

The existing exception middleware serializes PascalCase error properties, while
success responses use the MVC camelCase contract. No response-wrapper redesign
was made here. HTTP status is authoritative.
Do not retry 400/401/403/404 indefinitely. A fresh authorized request can retry a
transient network/server failure. HTTP cancellation propagates to EF operations;
there is no background job to cancel or poll.

UI developer integration: maintain separate unit and per-unit topic page state;
reset the unit page when framework changes, and topic page when unit changes.
Use root totals to enable next/previous; request page 1 after filter changes.
Do not eagerly download all pages to simulate server pagination. Offset paging
is deterministic for an unchanged dataset but is not a cross-request snapshot;
concurrent edits may move records. Refresh after editing/deleting as appropriate.

## Persistence and acceptance

No schema migration, catalogue seed, new table or persistent page cache is needed.
Reads use existing `CurriculumFrameworkMaster`, `CurriculumUnit`, `CurriculumTopic`,
`LessonPlan`, `LessonPlanCurriculumTopic`, `LessonDeliveryLog`,
`LessonDeliveryCoverage`, workflow/academic/staff join tables. Ownership and
soft-delete predicates are applied before counting. Queries are read-only;
count and page execute separately under the existing database isolation level.
Progress aggregates all approved coverage for the returned units, independently
of the delivery-list page; it is not computed from just 10 log rows.

Exact executed results and deployment limits are recorded in the
[2026-10-17 scenario report](../testing/staff/curriculum-pagination/2026-10-17.md).
Angular integration and Render deployment are separate acceptance steps and are
not implied by local backend tests. No Angular files were changed.
