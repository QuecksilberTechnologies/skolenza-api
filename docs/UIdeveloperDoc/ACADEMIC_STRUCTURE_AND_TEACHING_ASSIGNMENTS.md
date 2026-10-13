# Academic structure and staff teaching assignments

## Behavior and permission discovery

Phase 2 provides one institution-owned hierarchy for School, College and Coaching Center tenants: Academic Session, Programme, Academic Group, Section, Subject and Course Offering. Staff subject delivery and class-teacher responsibility are separate effective-dated assignments. Consumers must resolve the current `STAFF_DIRECTORY` module and View/Add/Update/Delete operation IDs through the authenticated menu/permission flow; numeric IDs are never fixed in the client.

All routes require the bearer token. The Angular interceptor sends `ModuleId` and `OperationId` in query parameters for GET/DELETE and in JSON for POST/PUT.

## Routes

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
