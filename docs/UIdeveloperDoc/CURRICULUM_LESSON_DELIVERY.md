# Curriculum and Lesson Delivery API/UI handoff

## Scope

Phase 3 completes curriculum frameworks, units/topics, lesson-plan approval, actual delivery approval and derived syllabus progress for School, College and Coaching Center tenants. PostgreSQL is authoritative. Angular resolves `STAFF_DIRECTORY` View/Add/Update/Delete operation IDs through the authenticated menu interceptor; IDs are never hardcoded.

## API

**2026-10-17 API-only pagination update:** the six collection routes now use
`PageNumber=1`, `PageSize=10` by default. Topics are independently paged through
`GET /units/{unitId}/topics`; paged unit headers no longer embed `topics`.
See [the complete paging contract and UI integration handoff](CURRICULUM_PAGINATION.md).
Angular was not changed in this update; existing clients must integrate page
navigation and the independent Topics request before browser acceptance.

Base route: `/api/Staff/curriculum-delivery`; every route requires authentication and the established institution permission pipeline.

- `GET /workflow-statuses`
- `GET|POST /frameworks`, `PUT|DELETE /frameworks/{id}`, `PUT /frameworks/{id}/review`
- `GET /frameworks/{id}/units`
- `GET /units/{unitId}/topics`
- `POST /units`, `PUT|DELETE /units/{id}`
- `POST /topics`, `PUT|DELETE /topics/{id}`
- `GET /teaching-assignments`
- `GET|POST /lesson-plans`, `PUT|DELETE /lesson-plans/{id}`, `PUT /lesson-plans/{id}/review`
- `GET|POST /lesson-deliveries`, `PUT|DELETE /lesson-deliveries/{id}`, `PUT /lesson-deliveries/{id}/review`
- `GET /syllabus-progress?courseOfferingId={optional}`

Review body: `{ "workflowStatusId": 2, "remark": "Reviewed against the approved syllabus." }`. Resolve the status ID from `/workflow-statuses`; valid transitions are Draft/Rejected → Submitted and Submitted → Approved/Rejected.

Frameworks own units; units bind the curriculum to a real course offering; topics hold planned periods and learning outcomes. A lesson plan references one effective teaching assignment and one or more topics belonging to its course offering. Delivery is accepted only for an approved plan. Progress is calculated from approved topic coverage and is not stored as a separate master.

Delete is soft delete and dependency safe. Approved/submitted history cannot be deleted. Framework deletion requires no units; unit deletion requires no child units/topics; topic deletion is blocked when referenced; lesson-plan deletion requires Draft/Rejected and no delivery; delivery deletion requires Draft/Rejected.

## Angular

Route: `/app/staff/curriculum-delivery`. The Staff Directory action opens a full page using existing Optimus card, button, select, confirmation-dialog and shared page-layout patterns. Tabs cover Frameworks, Units & Topics, Lesson Plans, Delivery Logs and Syllabus Progress. All catalogues come from APIs.

## Persistence

Tables: `AcademicWorkflowStatusMaster`, `CurriculumFrameworkMaster`, `CurriculumUnit`, `CurriculumTopic`, `LessonPlan`, `LessonPlanCurriculumTopic`, `LessonDeliveryLog`, `LessonDeliveryCoverage`. Apply `database-scripts/20261013_AddCurriculumLessonDelivery.sql`; rollback uses `database-scripts/rollback/20261013_RemoveCurriculumLessonDelivery.sql`.

The optional institution-specific starter catalogue for the existing Red Rose St Marry School tenant is `database-scripts/20261013_SeedRedRoseSchoolAcademicCurriculum.sql`. It is idempotent and has been applied twice to local and Render. It seeds academic structure, offerings and approved curriculum content only. Teaching assignments and lesson transactions remain tied to real staff onboarding and are never fabricated by the seed.

## Verification status

Local and Render migrations were applied twice on 2026-10-13: both contain 8/8 tables and 6 active workflow statuses. The RRSMS seed was also applied twice to each database with matching counts: 1 session, 3 programmes, 10 grades, 20 sections, 10 subjects, 74 offerings, 1 framework, 222 units and 666 topics. Backend focused gate passed 16/16, the locked Staff gate passed 12/12, the institution rollback probe passed 2/2 and Release build passed. Angular production build passed. Live deployed route/authenticated acceptance must be recorded after the new commit is deployed.
