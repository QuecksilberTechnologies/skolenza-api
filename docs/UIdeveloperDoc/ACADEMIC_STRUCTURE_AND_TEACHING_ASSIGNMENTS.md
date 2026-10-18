# Academic structure and staff teaching assignments

## Behavior and permission discovery

Phase 2 provides one institution-owned hierarchy for School, College and Coaching Center tenants: Academic Session, Programme, Academic Group, Section, Subject and Course Offering. Staff subject delivery and class-teacher responsibility are separate effective-dated assignments. Consumers must resolve the current `STAFF_DIRECTORY` module and View/Add/Update/Delete operation IDs through the authenticated menu/permission flow; numeric IDs are never fixed in the client.

All routes require the bearer token. The Angular interceptor sends `ModuleId` and `OperationId` in query parameters for GET/DELETE and in JSON for POST/PUT.

## Routes

**2026-10-18 API-only paging:** all six Academic master GET variants below now
default to `PageNumber=1`, `PageSize=10`. Existing writes, staff profiles and
teaching-assignment-type lookup contracts are unchanged. See the paging contract
at the end of this document and the [consolidated 17-list API index](STAFF_PAGINATION_API_INDEX.md).

- `GET|POST /api/Staff/academic-structure/masters/{Session|Programme|Group|Section|Subject|CourseOffering}`
- `GET /api/Staff/academic-structure/teaching-assignment-types`
- `PUT|DELETE /api/Staff/academic-structure/masters/{masterType}/{id}`
- `GET /api/Staff/academic-structure/staff/{encodedStaffMemberId}`
- `POST /api/Staff/academic-structure/teaching-assignments`
- `PUT|DELETE /api/Staff/academic-structure/teaching-assignments/{id}`
- `POST /api/Staff/academic-structure/class-teacher-assignments`
- `PUT|DELETE /api/Staff/academic-structure/class-teacher-assignments/{id}`

## Representative requests

```json
{
  "code": "GRADE-08",
  "name": "Grade 8",
  "academicSessionId": 1,
  "academicProgrammeId": 1,
  "type": 1,
  "isActive": true,
  "moduleId": 0,
  "operationId": 0
}
```

```json
{
  "staffMemberId": "<encoded staff id>",
  "courseOfferingId": 1,
  "classSectionId": 1,
  "assignmentTypeId": 1,
  "contributionPercent": 100,
  "effectiveFrom": "2026-04-01",
  "isPrimary": true,
  "isActive": true,
  "moduleId": 0,
  "operationId": 0
}
```

`assignmentTypeId` must come from `teaching-assignment-types`; consumers must not duplicate its numeric IDs or labels. The server rejects cross-tenant references, an offering/section group mismatch, overlapping effective teaching assignments, invalid date ranges and a second overlapping primary class teacher for the same section. Standard `ApiResponse<T>` envelopes and the existing global error contract apply. These endpoints are synchronous; polling and cancellation APIs are not used.

## Persistence and UI status

The additive migration creates catalogue tables `AcademicSessionMaster`, `AcademicProgrammeMaster`, `AcademicGroupMaster`, `ClassSectionMaster`, `SubjectMaster`, `CourseOfferingMaster`, `TeachingAssignmentTypeMaster` and operational tables `StaffTeachingAssignment`, `ClassTeacherAssignment`. The Angular implementation provides full-page catalogue and staff assignment routes, API-backed teaching-role selection, edit/delete operations and assignment display on Staff Overview.

Local migration, API compilation/startup, Angular production build, 12 focused regressions, authenticated hierarchy CRUD, assignment read-back/update/delete, HTTP 409 conflict handling and cross-institution HTTP 403 verification passed on 2026-10-13. The Render PostgreSQL migration is applied and reconciled. Render API commit `3115a53` is Live; deployed OpenAPI exposes every route above and an unauthenticated lookup returns HTTP 401. Authenticated deployed browser acceptance remains pending.

## Functional walkthrough

Business users aur developers ke liye complete Phase 1–5 real-time example: [Staff Phase 1–5 Complete Functional Flow — Hinglish Guide](STAFF_PHASE_1_TO_5_COMPLETE_FLOW_HINGLISH.md).


## Academic master list pagination � 2026-10-18

All six GET variants `/masters/{Session|Programme|Group|Section|Subject|CourseOffering}`
now accept the same paging query. These names/values are the existing
`AcademicMasterType` enum (Session=1 through CourseOffering=6); no new catalogue
or business mappings were added.

| Query field | Required | Behavior |
| --- | --- | --- |
| `ModuleId`, `OperationId` | Yes | Resolve current STAFF_DIRECTORY / View IDs through authenticated navigation/permission flow |
| `PageNumber` | No | Default 1; nonpositive values normalize to 1 |
| `PageSize` | No | Default 10; nonpositive values normalize to 10; positive custom sizes are honored |
| `activeOnly` | No | Default false; true returns only active rows |

Send the existing institution bearer token. The existing permission pipeline
resolves tenant ownership from authenticated context. Do not add a caller-chosen
InstitutionId or hardcode module-operation IDs. Fixed order is Name ascending,
then Id ascending; inherited SortBy/SortOrder fields do not override this order.
Filters and soft-delete scope apply before count/page. No new maximum-size rule,
parent filter, workflow status or write behavior was introduced.

Copyable request; variables come from login/menu rather than fixed IDs:

```powershell
$query = "ModuleId=$moduleId&OperationId=$viewOperationId&PageNumber=2&PageSize=10&activeOnly=false"
Invoke-RestMethod -Headers @{Authorization="Bearer $accessToken"} `
  -Uri "$apiBase/Staff/academic-structure/masters/Subject?$query"
```

Use the same query for Session, Programme, Group, Section and CourseOffering.
These GET requests have no JSON body, FormData, spreadsheet contract or job.
Representative response below abbreviates the existing item fields:

```json
{
  "isSucceeded": true,
  "message": "Academic records retrieved successfully.",
  "data": [
    { "id": 501, "code": "SCI", "name": "Science", "description": "Science learning", "isActive": true, "type": 1 }
  ],
  "errors": [],
  "pageNumber": 2,
  "pageSize": 10,
  "totalRecords": 11,
  "totalPages": 2
}
```

The root data array remains. No nested data.data envelope or profile-completion
flags are introduced. Existing date ranges, campus/parent/type fields,
Session/Programme/Group/Subject IDs and names, capacity, level, curriculum
version and required periods remain in the full AcademicMasterResponseDTO.
Parent names resolve even when that parent is outside its own first catalogue
page. Internal reads and write responses are not truncated to the first page.

Empty results have totalRecords=0, totalPages=0. A page after the end returns an
empty array with real totals and the requested page number. Missing/invalid
bearer returns 401; absent/incorrect View context returns 403. Non-integer paging
inputs and unsupported master types return 400. Existing centralized error
serialization remains; representative unsupported type response:

```json
{
  "IsSucceeded": false,
  "Message": "Unsupported academic master type.",
  "Data": null,
  "Errors": ["Unsupported academic master type."],
  "ErrorCode": "VALIDATION_ERROR"
}
```

HTTP cancellation propagates through existing EF queries. No polling or
background cancellation endpoint is involved; retry transient network/server
failures as appropriate, not indefinite retries of 400/401/403. Count and page
are separate reads under existing isolation, not a cross-request snapshot.

No table, migration, catalogue seed or cache was added. Existing AcademicSessionMaster,
AcademicProgrammeMaster, AcademicGroupMaster, ClassSectionMaster, SubjectMaster
and CourseOfferingMaster plus their existing reference projections are reused.

**Angular was not changed.** UI developers must consume root totals and fetch
subsequent pages for tab lists and parent-option selectors. Reset page on filter
changes; the first 10 rows are not the whole catalogue. Existing teaching role
lookup and staff assignment/profile routes remain unpaged/unchanged.

Local affected suite: 41/41; Debug and Release builds: zero errors. All six
Academic and all existing Professional/Curriculum list variants passed local
HTTP paging checks. Render deployment and browser integration remain unaccepted.
See [the scenario and sanitized evidence](../testing/staff/academic-pagination/2026-10-18.md)
and [the complete 17-list index](STAFF_PAGINATION_API_INDEX.md).
