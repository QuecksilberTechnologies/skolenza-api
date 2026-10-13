# Skolenza Staff Management: proposed professional system and current gap analysis

**Status:** PROPOSED — analysis and design only; no database/API/UI implementation is claimed.  
**Prepared:** 12 October 2026  
**Scope:** School, college and coaching-center staff management, with the first implementation slice focused on staff. Student and parent workflows are considered only where staff assignments need future-safe references.

## 1. Executive decision

Continue with the current Skolenza architecture. A new application or a second staff model is not required. The existing system already has useful institution isolation, staff identity/profile, department, designation, employment type, reporting manager, attendance, leave, payroll, roles/permissions, tickets and audit fields. The missing capability is an education-specific **assignment layer** between a staff member and the work that person performs.

The system must keep these concepts separate:

| Concept | Meaning | Examples | Ownership |
| --- | --- | --- | --- |
| Staff category | Broad nature of work | Teaching, Non-Teaching, Technical, Administrative, Support | Central platform catalogue |
| Employment type | Contractual relationship | Permanent, Contractual, Guest/Visiting | Central template; institution-owned option when customization is required |
| Department | Organization unit | Mathematics, Accounts, Administration, Facilities | Institution master |
| Designation | Position title | PGT Mathematics, Accountant, Electrician, Peon/MTS | Institution master, linked to department |
| Job family/service domain | Professional specialization | Subject Teaching, Sports, Arts, Accounts, Electrical, Library, Lab, Transport, Housekeeping | Central catalogue with institution activation |
| Role | Application access | Staff Manager, Staff Member, Institution Administrator | Institution access-control master |
| Assignment | What the person is responsible for, where and for how long | Teacher of Grade 8A Mathematics; Class Teacher of 6B; electrician for Main Campus | Institution transactional data |
| Duty/task | Specific accountable work | Morning gate duty, fee reconciliation, lab preparation, repair request | Institution transactional data |

`Role` must never be used as a job title, `Designation` must never grant application permission, and `StaffEmploymentType` must never contain Teaching/Non-Teaching classifications.

## 2. Evidence used for the proposed model

The design is based on current Skolenza code and the local PostgreSQL schema, plus these authoritative sources:

- [CBSE Affiliation Bye-Laws: duties of the Head of School](https://www.cbse.gov.in/cbsenew/aff-bye-laws.html) place timetable preparation, teaching-load allocation, supervision of teaching and non-teaching staff, monthly staff review, academic planning, remedial teaching, homework scrutiny and co-curricular coordination under school leadership.
- [CBSE teaching-staff guidance](https://www.cbse.gov.in/cbsenew/teaching_staff.html) separates qualified teaching staff, service rules and continuing teacher training.
- [CBSE curriculum for 2026–27](https://cbseacademic.nic.in/curriculum_2027.html) shows that subjects include languages, core subjects, vocational education, physical education and art education; the model must therefore support classroom, sports, arts and vocational teaching assignments.
- [CBSE lesson-plan example](https://www.cbse.gov.in/cbsenew/documents/B.K.Birla%20Public%20School%2C%20Kalyan.pdf) records objectives, methods, resources, activities, homework, periods and review/approval, supporting structured lesson planning and delivery logs.
- [CBSE Principals Handbook](https://www.cbse.gov.in/cbsenew/documents/Principals_Handbook.pdf) connects lesson plans, annual pedagogical plans, classroom execution, assessment and learning outcomes.
- [KVS duties and responsibilities](https://kvsangathan.nic.in/en/powers-and-duties-of-officers-and-employees/) cover timetable/subject coordination, examinations, co-curricular work, library/lab resources, property, cleanliness and delegated responsibilities. It also states that non-teaching work allocation can be redistributed by the Principal according to local needs.
- [Ministry of Education UDISE+ report](https://dsel.education.gov.in/sites/default/files/statistics/report_in_PDF/UDISE%2B2020_21_Booklet.pdf) identifies non-teaching groups including Peon/MTS, LDC, UDC/Head Clerk, laboratory assistant, library assistant, accountant and night watchman.
- [Ed-Fi Staff Domain](https://docs.ed-fi.org/reference/data-exchange/data-standard/4/model-reference/staff-domain/overview/) separates employment from position assignment and school-of-service assignment.
- [Ed-Fi teaching and learning model](https://docs.ed-fi.org/reference/data-exchange/data-standard/5/model-reference/teaching-and-learning-domain/best-practices/) uses a staff-to-section association for every teacher/support assignment, with classroom position, dates and contribution.
- [1EdTech OneRoster](https://www.imsglobal.org/oneroster-v11-final-specification) models academic sessions, courses, classes, subjects, periods and teacher enrolments, supporting interoperable class/subject allocation.
- [PM SHRI school-quality framework](https://pmshri.education.gov.in/sqaf-pillar-details/3) expects duties to be assigned according to staff capability and sufficient staff for academic and non-academic activities.

These sources define a reusable operational model; institution-specific duty wording remains configurable and must not be hardcoded in consumers.

## 3. Staff lifecycle

### 3.1 Workforce planning

1. Institution creates/activates departments and designations.
2. It enables required job families and defines positions with sanctioned headcount, campus, qualification/skill expectations and effective dates.
3. Academic leadership defines the academic session, grades/programmes, sections/cohorts, subjects/courses and required teaching periods.
4. The workload view compares required work with available qualified staff before onboarding or allocation.

### 3.2 Onboarding

1. Create the staff identity and code.
2. Select exactly one primary staff category and one employment type.
3. Select department, designation, primary campus and reporting manager.
4. Attach one or more job-family/capability records; examples: Mathematics teaching, sports coaching, electrical maintenance, accounting.
5. Validate qualifications/documents applicable to the position.
6. Create effective-dated position assignment and work arrangement.
7. Assign application role(s) through the existing permission pipeline.
8. Assign duties or teaching responsibilities. A staff member can have multiple assignments, but only one marked primary within the same assignment dimension and overlapping period.
9. Record acknowledgement of policies, responsibilities and handover.

### 3.3 Day-to-day operation

- Attendance and leave continue through existing modules.
- Teaching staff see timetable, classes, subjects, curriculum units, lesson plans, pending syllabus and substitute duties.
- Class teachers see their homeroom/class-teacher assignment and class coordination tasks; this designation does not imply they teach every subject.
- Non-teaching staff see role-appropriate duty queues: accounts work, facilities/service requests, library/lab tasks, office tasks, transport/security/housekeeping duties.
- Heads see workload, unassigned work, conflicts, overdue tasks, leave impact and substitution recommendations.

### 3.4 Change and exit

Transfers, promotions, category changes, employment-type changes and assignment changes must close the old effective-dated record and create a new record. Exit must revoke future timetable/duty assignments, reassign open work, preserve history and then deactivate access according to the existing security process.

## 4. Teaching staff operating model

### 4.1 Required relationships

A teacher is not simply “Teaching”. The system must answer:

- Which campus and academic session?
- Which grade/programme, section/cohort and subject/course?
- Is the person teacher-of-record, co-teacher, assistant, substitute, sports coach, arts instructor, special educator or class teacher?
- What date range and contribution/workload apply?
- Which periods and rooms are allocated?
- Which curriculum version, units and learning outcomes apply?

### 4.2 Curriculum and syllabus progress

Use this hierarchy:

`CurriculumFramework → CurriculumSubject → CurriculumUnit → CurriculumTopic/LearningOutcome`

Each `CourseOffering` binds subject/course + academic session + grade/programme. A `TeachingAssignment` binds staff + class section/cohort + course offering. Lesson planning and delivery are separate:

- `LessonPlan`: intended date/period, objectives, method, resources, activities, assessment/homework, approval status.
- `LessonDeliveryLog`: what actually happened, covered units/topics, completion percentage/evidence, homework, exception/replan reason.
- Syllabus progress is derived from approved delivery logs against curriculum units. Do not store one manually editable percentage as the source of truth.

### 4.3 Class teacher

`ClassTeacherAssignment` is a separate effective-dated assignment for a class section. It may include attendance oversight, notices, parent coordination, consolidated academic follow-up and escalation. It must not automatically grant subject marks or unrelated student-data access; permissions remain module/operation based and scoped to the assigned class.

### 4.4 Leave and substitution

When approved leave overlaps timetable entries:

1. Create an impact record per affected period/duty.
2. Find eligible substitutes using active status, same campus, subject/job-family capability, timetable availability, workload ceiling and no leave/conflict.
3. A scheduler/head confirms the replacement; the system must not silently assign a person.
4. Create `SubstituteAssignment` with original staff, substitute, timetable/duty reference, reason, approval and acknowledgement.
5. Notify the substitute and relevant class/administration; keep an immutable audit trail.
6. If no substitute exists, flag the period as unresolved and escalate.

## 5. Non-teaching and support staff operating model

Do not create a rigid nested enum such as `NonTeaching → Peon/Electrician/Accounts`. Institutions use different names and one person may cover multiple services. Use configurable job families, designations and duties.

| Example job family | Typical configurable duties | Recommended work source |
| --- | --- | --- |
| Accounts/Finance | fee reconciliation, vouchers, payroll support, audit documents | Duty assignment + ticket/task |
| Office/Clerical | admissions documents, records, correspondence, certificates | Duty assignment + workflow/task |
| Facilities/Electrical | preventive checks, repair response, vendor follow-up, safety evidence | Asset/service ticket + scheduled duty |
| Peon/MTS/Office Support | document movement, room readiness, dispatch, office assistance | recurring duty + ad-hoc task |
| Laboratory | equipment preparation, issue/return, stock and safety checklist | lab duty + inventory task |
| Library | circulation, catalogue/stock, reading support | library duty + module task |
| Sports/Arts | scheduled instruction, events, equipment, student participation | teaching/co-curricular assignment |
| Security/Transport/Housekeeping | rostered post/route/area, checklist, incident escalation | shift/location duty + incident ticket |

The catalogue defines capability and templates; the institution defines actual duties, frequency, owner, approver, SLA and campus. A designation may suggest default duty templates, but assignments must be explicitly created and versioned.

## 6. Queries, direct messages and broadcasts

Three different channels are required; combining them into one generic message table would create permission and audit problems.

### 6.1 Accountable query/service request

Reuse and extend the existing `Ticket`, `TicketType`, `TicketThread`, `ThreadMessage`, assignment, approval, SLA and escalation model. Examples: “salary discrepancy”, “electric repair”, “document required”, or a Principal asking a staff member for a formal response. Add education-aware targeting and confidentiality, but keep the existing ticket permission pipeline.

Required behavior: requester/requested-for, assigned user/role, due date, priority, acknowledgement, status, attachments, threaded replies, escalation and audit history. A query can target one staff member or a responsible role, not an uncontrolled group.

### 6.2 Direct operational conversation

Use a new conversation model for quick one-to-one or controlled group communication. Membership must be explicit; messages need sender, timestamp, edit/delete policy and read receipt. It is not a replacement for tickets, approvals or permanent academic records.

### 6.3 Announcement/broadcast

A Principal or authorized head creates one announcement and selects a server-resolved audience:

- all active staff;
- category, department, designation, campus or job family;
- teaching assignment/class/subject cohort;
- selected staff members.

Store the audience rule and a recipient snapshot at publish time so history does not change when staff move departments. Support draft, scheduled, published, cancelled and expired states; channel selection (in-app/email/push), acknowledgement requirement, delivery/read/acknowledgement receipts and attachments. Every publish action must pass the existing permission pipeline. Recipient lists must be resolved on the server and institution-scoped.

## 7. Proposed data model

Names below are proposals and require migration approval before implementation.

### 7.1 Central catalogues

| Table | Key fields | Purpose |
| --- | --- | --- |
| `StaffCategory` | `Id`, `Code`, `Name`, `Description`, audit/active | Replace numeric-only 1–5 knowledge with one API-backed source: Teaching, Non-Teaching, Technical, Administrative, Support |
| `StaffJobFamily` | `Id`, `Code`, `Name`, `ParentId`, `DefaultCategoryId`, `Description` | Central hierarchy for Subject Teaching, Sports, Arts, Accounts, Electrical, Library, Lab, etc. |
| `StaffDutyTemplate` | `Id`, `JobFamilyId`, `Code`, `Name`, `Description`, `DefaultFrequency`, `RequiresEvidence` | Reusable duty definitions; institutions activate/customize them |
| `TeachingAssignmentType` | `Id`, `Code`, `Name` | Teacher of record, co-teacher, assistant, substitute, coach, special educator |

Catalogues are maintained once in persistence and exposed by authenticated option endpoints. Angular/API consumers must not duplicate IDs or lists.

### 7.2 Institution workforce structure

| Table | Important columns and constraints |
| --- | --- |
| `InstitutionJobFamily` | `InstitutionId`, `StaffJobFamilyId`, `IsEnabled`; unique pair |
| `Position` | `InstitutionId`, `CampusId`, `DepartmentId`, `DesignationId`, `JobFamilyId`, `PositionCode`, `SanctionedCount`, `EffectiveFrom/To`, status |
| `StaffPositionAssignment` | `InstitutionId`, `StaffMemberId`, `PositionId`, `IsPrimary`, `AllocationPercent`, `EffectiveFrom/To`, audit; prevent overlapping duplicate primary assignments |
| `StaffCapability` | `InstitutionId`, `StaffMemberId`, `JobFamilyId`, optional `SubjectId`, `ProficiencyLevel`, verification and validity dates |
| `InstitutionDuty` | institution customization of a duty template: owner role, SLA, frequency, campus applicability |
| `StaffDutyAssignment` | staff, institution duty, campus/area, effective dates, recurrence/workload, approver |
| `StaffTask` | assignment/source, title, due date, status, priority, assignee, evidence, completion/verification |

### 7.3 Academic structure and teaching

| Table | Important columns and constraints |
| --- | --- |
| `AcademicSession` | institution/campus, code, name, start/end, parent session, type (year/term/semester); unique institution + code |
| `AcademicProgramme` | school stream, college programme or coaching course; code, name, level |
| `AcademicGroup` | generic grade, semester, batch, class or cohort; programme, academic session, code/name |
| `ClassSection` | academic group, section name/code, campus, capacity |
| `Subject` | institution/board code, name, subject type (academic/sports/arts/vocational) |
| `CourseOffering` | academic session, academic group/programme, subject, curriculum version, required periods |
| `TeachingAssignment` | staff, class section, course offering, assignment type, contribution %, begin/end, primary flag |
| `ClassTeacherAssignment` | staff, class section, begin/end, primary flag |
| `TimetablePeriod` | campus/session, day, sequence, start/end, break flag |
| `TimetableEntry` | class section, course offering, period, staff assignment, room, effective dates; conflict constraints |
| `CurriculumFramework` | board/university, academic year/version, status |
| `CurriculumUnit` | framework/course, parent unit, sequence, title, planned periods, learning outcome |
| `LessonPlan` | teaching assignment, date/period, objectives, method/resources/assessment, approval workflow |
| `LessonDeliveryLog` | timetable/lesson plan, actual coverage, homework/evidence, status and replanning reason |
| `LeaveAssignmentImpact` | leave request + affected timetable/duty reference + resolution status |
| `SubstituteAssignment` | impact, original staff, substitute staff, approver, acknowledgement, reason |

`AcademicGroup` and `AcademicProgramme` make the same foundation work for school
grades/sections, college programmes/semesters/batches and coaching courses/batches
without duplicating staff assignment logic. Institution type controls applicable
levels and display terminology; catalogue values remain database-owned.

### 7.4 Innovation and professional development

| Table | Purpose |
| --- | --- |
| `StaffInnovationProposal` | problem, proposal, expected benefit, category, owner, status, reviewer and decision |
| `StaffInnovationReview` | scored/structured feedback and decision history |
| `StaffProfessionalDevelopment` | training/certification, provider, dates, evidence, expiry and verification |
| `StaffGoal` / `StaffGoalProgress` | agreed goals and evidence; keep separate from disciplinary records |

### 7.5 Communication

| Table | Purpose |
| --- | --- |
| `Conversation`, `ConversationParticipant`, `ConversationMessage`, `MessageReadReceipt` | explicit direct/group operational messaging |
| `Announcement`, `AnnouncementAudienceRule`, `AnnouncementRecipient`, `AnnouncementReceipt` | controlled broadcast with immutable recipient snapshot and delivery/read/acknowledgement |

## 8. Required changes to current tables

### 8.1 `StaffMember`

| Current column | Finding | Proposed treatment |
| --- | --- | --- |
| `EmployementCode` | Typo in physical/API naming | Migrate to `StaffMemberCode`; retain a temporary compatibility alias only during coordinated API/UI migration |
| `StaffCategoryId` | Numeric check only; no `StaffCategory` FK/table was present in the inspected DB | Add FK to central `StaffCategory`; expose option endpoint; preserve values 1–5 |
| `StaffEmploymentTypeId` | Correct concept, but current global DB rows also contain Technical, Teaching and Non-Teaching | Keep FK; migrate dependencies, then remove category-like employment rows after explicit data reconciliation |
| `HasPermanent` | Duplicates Permanent employment-type meaning and can disagree with the FK | Make derived/read-only during transition, then remove after all consumers migrate |
| `DepartmentId`, `DesignationId` | Useful current assignment snapshot, but cannot represent transfers/history/multiple positions | Keep temporarily for compatibility; source future state from primary effective `StaffPositionAssignment` |
| `FunctionalId` | No clear education-domain meaning or verified FK found | Audit usages/data; replace with job family/capability/assignment and retire only after approved mapping |
| `ReferalId` | Ambiguous spelling and ownership | Decide whether it means referring staff or recruitment source; migrate to explicit `ReferrerStaffMemberId` FK or `RecruitmentSourceId` |
| `DateOfOnBoarding`, `DateOfExit` | Correct lifecycle dates but timestamps are unnecessarily broad | Consider `date` in a later compatible migration; do not change until timezone/API impact is tested |

### 8.2 Other current structures

- `Department` and `Designation` remain institution masters. Add effective-date/history only if changes must preserve historical naming; do not turn them into enums.
- `StaffMemberManagerMapping` is useful and already effective-dated. Add tenant-safe uniqueness/overlap constraints and use reporting type for line, functional, academic/HOD or dotted-line reporting.
- `StaffMemberCategorySkill` is too generic to safely represent verified teaching subjects or job capabilities. Migrate it to `StaffCapability` after its current data semantics are confirmed.
- `StaffMemberWorkProfile` only records fresher/comment and does not describe current duties. Keep it for experience-profile compatibility; do not overload it with assignments.
- `LeaveRequest` has no timetable/duty impact or substitute relation. Add separate impact/substitution tables instead of nullable columns for every future work type.
- Existing `Ticket`/thread tables are strong foundations for formal staff queries. Add scope/confidentiality and education target references through explicit nullable reference/type design or dedicated link tables; do not create a second ticket engine.
- Existing `ThreadMessage` belongs to ticket threads and cannot safely serve as institution broadcast or general chat.

## 9. Confirmed current gaps and conflicts

1. Local DB has no `StaffCategory` table although `StaffMember.StaffCategoryId` exists.
2. `20261011_AddStaffCategory.sql` still targets the old physical table name `Employee`, while the inspected live table is `StaffMember`; this script must be corrected through the coordinated migration process before reuse.
3. Global `StaffEmploymentType` data contains `Technical`, `Teaching` and `Non-Teaching`, conflicting with the now-documented employment/category separation. Current application constants define only Permanent, Contractual and Guest.
4. No current tables were found for academic session, programme/grade, class section, subject, course offering, curriculum/syllabus, lesson plan, syllabus-delivery progress, timetable or substitute assignment.
5. No general conversation or announcement/broadcast persistence exists; only ticket-thread messages were found.
6. No configurable duty catalogue, staff duty assignment, task/evidence or workload allocation model exists.
7. Current department/designation/staff fields describe one present placement but cannot represent simultaneous roles, historical transfers or percentage allocations professionally.
8. Leave approval does not calculate affected classes/duties or manage replacement ownership.

## 10. Permission and data-scope rules

- Reuse the existing authenticated module/operation permission pipeline.
- Every record carries `InstitutionId`; campus/academic references must belong to that institution.
- Capability does not grant access. Assignment scope plus permission determines which records a staff member can operate on.
- Heads can assign only within authorized departments/campuses/academic scopes.
- Sensitive HR, payroll, grievance and performance conversations require separate permissions and restricted recipients.
- Server resolves audiences, substitutes and workload; UI-supplied staff IDs are validated against institution and active assignment.
- All assignments, approvals, substitutions, broadcasts and acknowledgements need actor/time audit data and concurrency protection.

## 11. Recommended implementation sequence

### Phase 0 — decisions and cleanup

1. Confirm catalogue ownership, job-family names, school/college terminology and the meaning of `FunctionalId`/`ReferalId`.
2. Inventory all dependencies on the conflicting employment-type rows and `HasPermanent`.
3. Correct the StaffCategory migration target and add a central catalogue without changing IDs 1–5.
4. Reconcile and retire obsolete/conflicting seed rows only after data mapping is approved.

### Phase 1 — staff foundation

Implement central StaffCategory/JobFamily/Duty catalogues, institution activation, position/capability/duty assignments and effective-dated history. Update onboarding and bulk templates together. This gives professional teaching/non-teaching classification before academic modules exist.

### Phase 2 — academic assignment

Implement academic session/programme/group/section/subject/course offering, teaching assignment, class-teacher assignment and timetable. Keep student enrolment out of scope initially, but use stable class-section keys for the future student module.

### Phase 3 — curriculum and delivery

Implement curriculum versions/units, lesson planning, delivery logs, derived syllabus progress, review and evidence.

### Phase 4 — leave coverage and workload

Connect approved leave to timetable/duties, create impact records, candidate suggestions, confirmed substitutes, acknowledgement and unresolved escalation.

### Phase 5 — queries and communication

Extend tickets for formal staff queries; add direct conversations and controlled announcements with recipient snapshots and receipts.

### Phase 6 — innovation and development

Add proposals/reviews, professional development and goals once core assignments provide reliable context.

For every phase: migration → entity/configuration → repository/handler → API/handoff → explicitly authorized Angular → automation tests → scenario evidence → deployment validation. No hardcoded consumer catalogues and no phase is COMPLETE until its tests pass.

## 12. Decisions required before implementation

1. Should institution administrators be allowed to add custom staff categories, or must categories 1–5 remain platform-controlled?
2. Should `Technical` remain a top-level category, or be a job family under Non-Teaching/Support?
3. Does `Administrative` overlap Non-Teaching in reporting, or must it stay independently reportable?
4. What do current `FunctionalId` and `ReferalId` values mean in production data?
5. **Resolved 2026-10-13:** the first academic slice supports School, College and Coaching Center through the common `AcademicProgramme`/`AcademicGroup` abstraction.
6. Who may approve substitutions: Principal, Vice Principal, HOD, timetable coordinator, or configurable approval policy?
7. Which broadcast channels are initially required: in-app only, email, push, SMS/WhatsApp integration?

Until these decisions are approved, this document is the proposed target and gap register, not authorization to create schema or business logic.
