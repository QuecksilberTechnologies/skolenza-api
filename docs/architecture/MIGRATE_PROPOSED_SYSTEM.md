# Migrate Proposed System

## Purpose

This is the execution contract for migrating Skolenza from its inherited workforce model to the proposed school and college staff-management system. Read it with `LOCKED_FIXES.md`, both repository `AGENTS` files, `SKOLENZA_RECONSTRUCTION_MASTER_PLAN.md`, and `STAFF_MANAGEMENT_PROPOSED_SYSTEM.md` before changing this programme.

Database, API and Angular must move together in every vertical slice. A slice is **WIP** until its schema, data, backend contract, UI consumer, tests, migration evidence and documentation are at the same stage.

The user explicitly waived a new database backup on 2026-10-12 because a backup was created on 2026-10-11. This exception applies only to this programme and does not remove the normal backup gate elsewhere.

## Rules

1. PostgreSQL master data is authoritative for configurable business catalogues. Angular must consume catalogues through APIs and must not duplicate option arrays or numeric IDs.
2. Backend enums/constants may hold stable protocol IDs only when the database seed fixes those IDs. Labels, descriptions, order and active state still come from the database.
3. Institution-owned data must carry `InstitutionId`; every query and uniqueness rule must preserve institution isolation.
4. Reuse the existing permission middleware, module-operation discovery, StaffMember handlers/repositories, regions and endpoint documentation.
5. Migrations must be idempotent and include prerequisites, data mapping, constraints, indexes, verification and rollback.
6. Remove obsolete Employee code and conflicting seeds only in the slice that replaces them, after dependency scans and tests pass.
7. Do not infer an unresolved business rule. Keep its phase blocked at the decision gate.
8. Local and deployed acceptance are recorded separately. Skipped or blocked checks are never passes.
9. Every new master-data table created from Phase 1B onward uses the `Master` suffix so operational tables and seed-bearing catalogues remain visibly distinct. Existing tables are not renamed implicitly; any legacy rename requires its own compatibility migration.

## Status

`PENDING` means not started. `WIP` means at least one layer is incomplete. `BLOCKED` means a named decision or regression prevents safe work. `COMPLETE` means DB, API, Angular, tests, docs and cleanup all satisfy the exit gate.

| Phase | Deliverable | DB | API | Angular | Overall |
| --- | --- | --- | --- | --- | --- |
| 0 | Governance, inventory and baselines | COMPLETE | COMPLETE | COMPLETE | COMPLETE |
| 1A | Central Staff Category catalogue | COMPLETE | COMPLETE | COMPLETE | COMPLETE |
| 1B | Job families, positions, capabilities and duties | COMPLETE | COMPLETE | COMPLETE | COMPLETE |
| 1C | Effective-dated staff assignments | COMPLETE | COMPLETE | COMPLETE | COMPLETE |
| 2 | Academic structure and teaching assignments | PENDING | PENDING | PENDING | PENDING |
| 3 | Curriculum, lesson planning and syllabus progress | PENDING | PENDING | PENDING | PENDING |
| 4 | Timetable, leave impact and substitute coverage | PENDING | PENDING | PENDING | PENDING |
| 5 | Staff queries, direct messages and broadcasts | PENDING | PENDING | PENDING | PENDING |
| 6 | Feedback, innovation and professional development | PENDING | PENDING | PENDING | PENDING |
| 7 | Legacy removal, cutover and deployed acceptance | PENDING | PENDING | PENDING | PENDING |

### Pending cross-cutting institution date and time settings

The user deferred staff date/time normalization on 2026-10-12. A future coordinated
Institution Settings slice will own the institution's date display format and time zone.
All Angular date/time display must consume that central setting; API and persistence
contracts must define UTC/date-only boundaries before implementation. Until that slice is
approved, do not infer an institution time zone, add a hardcoded display format, or treat
the current timezone-free staff-date failure as resolved.

## Phase 0 — inventory and baselines

Confirmed gaps:

- `StaffMember.StaffCategoryId` has no central `StaffCategory` master or enforced FK.
- `20261011_AddStaffCategory.sql` targets retired `Employee`, while the live table is `StaffMember`.
- Employment duration and workforce categories are mixed in `StaffEmploymentType` seed data.
- No job-family, capability, duty, position or staff-duty assignment model exists.
- No academic session, class/section, subject, teaching assignment, curriculum, timetable or substitution model exists.
- `ThreadMessage` belongs to ticketing and is not general staff communication.
- `FunctionalId` and `ReferalId` are ambiguous and cannot be migrated safely yet.

Phase 0 exits after current schema/API/UI/seed ownership is mapped, all affected locked pre-change gates pass, and every remaining ambiguity has a decision owner.

Baseline evidence recorded on 2026-10-12:

- Institution registration transaction gate: 14 passed, 0 failed, 0 skipped.
- StaffMember profile/contact gate: 52 passed, 0 failed, 0 skipped.
- Role/persona catalogue gate: 26 passed, 0 failed, 0 skipped.
- Angular StaffMember dialog and permission-route gate: 63 passed, 0 failed.
- Angular production build: passed.
- `LOCKED_FIXES.md` still names retired `Tenant_creation...` and `Employee...` tests in some commands. The equivalent current `Institution_creation...` and `StaffMember...` tests were executed; this documentation drift must be corrected without changing locked behavior.

## Phase 1A — central Staff Category catalogue

Target catalogue proposed during architecture analysis (not yet approved as an ID migration):

| ID | Code | Label |
| --- | --- | --- |
| 1 | `ADMINISTRATIVE` | Administrative |
| 2 | `TEACHING` | Teaching |
| 3 | `NON_TEACHING` | Non-Teaching |
| 4 | `SUPPORT` | Support |
| 5 | `SPECIALIST` | Specialist |

For both self-registration and Host-created institutions, the initial account remains `ADMINISTRATIVE`. The backend supplies it; registration must not show a category dropdown.

### D-00 blocking conflict discovered before implementation

The current locked application constants and the existing migration assign these meanings:

| Current ID | Current meaning |
| --- | --- |
| 1 | Teaching |
| 2 | Non-Teaching |
| 3 | Technical |
| 4 | Administrative |
| 5 | Support |

The proposed target table changes every meaning and replaces `Technical` with `Specialist`. The user approved coordinated migration on 2026-10-12. Applied mapping: `1→2`, `2→3`, `3→5`, `4→1`, `5→4`; constants, registration, API contracts, Angular consumers and local data moved together.

Database work:

- Add global `StaffCategory` master with unique code, label, description, display order, active/system flags and audit fields following the existing master pattern.
- Seed the five approved rows idempotently.
- Validate every existing non-null category value, map initial administrative accounts, then add index and FK from `StaffMember.StaffCategoryId`.
- Replace the obsolete Employee-targeting script with a StaffMember migration and rollback.

API work:

- Add an authenticated options endpoint following the current Role/StaffEmploymentType option pattern.
- Return `id`, `code`, `label`, `description`; do not expose an Angular hardcoded list.
- Validate StaffMember create/update values against active catalogue records.
- Preserve fixed Administrative assignment in both institution-registration paths.
- Add contract, permission, validation and registration tests.

Angular work:

- Add the shared interface and API-backed lookup resource.
- Use it in authorized StaffMember create/edit forms.
- Do not add it to institution registration.
- Remove duplicate category labels/IDs from components, helpers and tests.
- Add API, lookup/form and interceptor tests.

Exit: local migration and FK verification pass; both registration paths produce Administrative staff; staff forms consume the API catalogue; applicable locked gates and new tests pass; a repository scan finds no duplicate category list.

## Phase 1B — job families, positions, capabilities and duties

- Add institution-scoped `StaffJobFamilyMaster` and `StaffPositionMaster` catalogues.
- Add `StaffCapabilityMaster`, `StaffDutyDefinitionMaster`, `StaffPositionCapability` and `StaffPositionDuty` after ownership is confirmed.
- Electrical, Accounts, Library, Transport, Sports, Arts, IT and Security belong here, not in employment duration.
- API CRUD/options use established handlers, repositories and permission flow, with dependency-safe deletion responses.
- Angular administration and shared dropdowns consume only those APIs.

Decision D-01: platform templates are copied into each institution. The institution-owned copy can be customized without a later platform-template update overwriting approved institution data.

Completed 2026-10-12: idempotent migration/rollback, central copy function,
institution master CRUD, position mapping CRUD, full-page Angular administration
and staff form catalogue consumption are synchronized. See
`docs/UIdeveloperDoc/STAFF_PROFESSIONAL_STRUCTURE.md`.

## Phase 1C — effective-dated staff assignments

- Add `StaffPositionAssignment`, `StaffCapabilityAssignment` and `StaffDutyAssignment` with institution, effective dates and active state.
- Reuse Department, Designation, Manager, Location and Employment Type FKs.
- Enforce approved overlap and primary-assignment rules.
- Staff onboarding and profile history consume canonical option/assignment contracts.
- Do not repurpose `FunctionalId` or `ReferalId` before decision D-02 defines their meanings and mappings.

Completed 2026-10-12: staff creation and institution registration create the
primary assignment and generated capability/duty assignments; official updates
replace the primary position with effective dating; Overview reads the same
canonical profile and authorized capability approval persists its audit.

## Phase 2 — academic structure and teaching assignments

- Add institution-scoped `AcademicSession`, `AcademicProgramme`, `AcademicClass`, `AcademicSection`, `Subject` and mapping tables.
- Add effective-dated `StaffTeachingAssignment` for session, class/section, subject and assignment role.
- Represent class teacher, subject teacher, sports and arts through assignments, not special StaffMember columns.
- Add API validation for duplicates/conflicts and Angular master/assignment workflows.

Decision D-03: confirm whether the first release models school, college, or both; this controls programme/class/section cardinality.

## Phase 3 — curriculum and delivery

- Add `Curriculum`, `CurriculumUnit`, `LessonPlan`, `LessonDelivery` and `SyllabusProgress` with session, subject and class scope.
- Keep planned and delivered work separate and preserve progress history.
- Teachers record plans/delivery; authorized heads review calculated coverage in Angular.

## Phase 4 — timetable and absence coverage

- Add `TimetablePeriod`, `TimetableEntry`, `StaffAbsenceImpact` and `SubstituteAssignment` with conflict indexes.
- Integrate approved leave with impacted work without altering the locked leave workflow.
- APIs detect conflicts and uncovered work; Angular supports proposed substitute, approval and status views.

Decision D-04: confirm substitute approval hierarchy and whether substitute acceptance is mandatory.

## Phase 5 — queries and communication

- Add a dedicated conversation/broadcast aggregate; do not overload ticket `ThreadMessage`.
- Model participants, messages, attachments, read receipts and audiences by institution, campus, department, category, role or explicit staff.
- Staff can raise a structured query; permitted principals/heads can message one person or broadcast to an authorized audience.

Decision D-05: confirm in-app-only versus email/SMS/push delivery and retention policy.

## Phase 6 — feedback and professional development

- Add structured ideas/feedback with review status, ownership and comments.
- Add training/certification goals only after catalogue ownership and approval workflow are confirmed.
- Link feedback to academic or operational context through real FKs, never ambiguous free-form identifiers.

## Phase 7 — cleanup and acceptance

- Remove obsolete Employee controllers, routes, DTOs, folders, symbols, SQL and Swagger text after every consumer is migrated.
- Remove conflicting seed constants/runtime lists after the DB-backed catalogue is live.
- Correct `EmployementCode` through an explicit compatibility migration and contract plan.
- Scan all projects for Employee/Tenant legacy naming, obsolete IDs, duplicate catalogues and dead migrations.
- Run affected locked gates, phase tests, Release API build, Angular production build, formatter and lint.
- Apply every idempotent script twice and record local schema/data verification.
- Record deployed verification separately and mark the programme complete only when every phase is complete.

## Required evidence for every slice

Update this file, the relevant `docs/UIdeveloperDoc` handoff, a dated `docs/testing/<module>/<scenario>` report, `docs/testing/README.md`, the module test index, and exact apply/verify/rollback instructions. Numeric permission IDs in docs are examples only; Angular resolves current IDs through the authenticated menu/permission flow.

## Decision register

| ID | Decision | Blocks |
| --- | --- | --- |
| D-00 | Proposed ID mapping approved and applied locally on 2026-10-12 | RESOLVED |
| D-01 | Platform templates copied into institution-owned capability/duty masters | RESOLVED on 2026-10-12 |
| D-02 | Meaning and mapping of `FunctionalId` and `ReferalId` | Phase 1C cleanup |
| D-03 | First academic model: school, college or both | Phase 2 |
| D-04 | Substitute approval and acceptance workflow | Phase 4 |
| D-05 | Broadcast channels and retention | Phase 5 |
| D-06 | New seed-bearing master tables use the `Master` suffix | RESOLVED on 2026-10-12 |

## Immediate execution order

1. Run corrected StaffMember-era locked baselines.
2. Implement Phase 1A as one DB/API/Angular slice.
3. Apply and verify it on the approved local DB without a new backup under the recorded waiver.
4. Run post-change locked/slice tests and update evidence/statuses.
5. Stop at D-01 before Phase 1B if ownership remains unconfirmed.
