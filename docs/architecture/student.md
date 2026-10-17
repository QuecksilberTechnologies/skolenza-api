# Student Teacher Guardian and Login Structure

Status: **PROPOSED AND DEFERRED**  
Decision date: **2026-10-17**  
Implementation priority: **After the Staff structure and its client training document are complete**

## Purpose and current boundary

Skolenza must keep Students separate from the existing StaffMember HR aggregate. A Student is an academic learner, while a Teacher is a StaffMember whose professional Position and academic teaching assignments describe their work. Student, StaffMember and Guardian identities may receive portal access through a common authentication layer, but their business records and permissions must remain isolated.

This document records the proposed structure and operating flow only. No Student database table, API, Angular screen, seed or authentication migration is implemented by this decision.

## Recommended relationship structure

```text
Institution
  -> AcademicSession
      -> Programme
          -> AcademicGroup or Grade
              -> ClassSection
              -> CourseOffering or Subject

StudentAdmission
  -> Student
      -> StudentEnrollment
          -> StudentSectionAssignment
          -> StudentCourseEnrollment

Guardian
  <-> StudentGuardian <-> Student

StaffMember
  -> StaffPositionAssignment -> StaffPositionMaster
  -> StaffTeachingAssignment -> ClassSection and CourseOffering

LoginCredential
  -> LoginSubject or UserIdentity
      -> StaffMember, Student or Guardian persona
      -> UserAccessRole
```

## Student aggregate

### Student

`Student` is the permanent institution-owned learner identity. Academic year, grade and section must not be stored as replaceable current values on this table.

Recommended data includes InstitutionId, StudentCode, AdmissionNumber, name, date of birth, GenderId, CountryId, optional contact information, lifecycle status, active state and audit fields. StudentCode must be unique inside one Institution.

The same Student row continues across academic sessions. Promotion creates a new enrollment; it does not create a duplicate Student.

### StudentAdmission

`StudentAdmission` owns the application and approval lifecycle before a permanent Student is created. Recommended statuses are Draft, Submitted, Under Review, Approved, Rejected and Cancelled.

Approval must create the Student, StudentEnrollment and approved Guardian mappings atomically. A rejected or cancelled application must not create a partial Student graph. Correction, rejection and cancellation must preserve reasons and audit history.

### StudentEnrollment

`StudentEnrollment` represents a Student's academic placement for a particular session. It links Student, AcademicSession, Programme, AcademicGroup or Grade and Campus, and stores enrollment date, roll number, effective dates, status and audit information.

Recommended statuses are Draft, Active, Completed, Withdrawn, Transferred and Cancelled. A completed 2028-29 enrollment remains historic; promotion creates a new 2029-30 enrollment for the same Student.

### StudentSectionAssignment

`StudentSectionAssignment` is effective-dated so a mid-session section transfer does not overwrite history. It links StudentEnrollment to ClassSection and includes roll number, EffectiveFrom, EffectiveTo, primary flag, transfer reason and audit fields.

### StudentCourseEnrollment

Mandatory subjects may be derived from the active Grade or Programme course offerings. `StudentCourseEnrollment` records elective, optional, added or dropped offerings when an Institution needs learner-level course selection. It links StudentEnrollment to CourseOffering and preserves status and effective dates.

## Guardian aggregate

`Guardian` is an independent person record with name, verified contact details, address, occupation, active state and audit fields.

`StudentGuardian` is a many-to-many relationship. It records relationship type, primary guardian, emergency contact, pickup authorization, academic and fee communication preferences, portal access and effective dates. One Guardian may access multiple linked Students through one account, subject to Institution isolation and the active relationship permissions.

## Teacher structure

A Teacher remains a `StaffMember`; no duplicate Teacher person table is required.

```text
StaffMember
  -> StaffCategory: Teaching
  -> StaffPositionAssignment: Mathematics Teacher
  -> UserAccessRole: Faculty
  -> StaffTeachingAssignment: Grade 5 Section A Mathematics
```

`StaffPositionAssignment` describes the professional job. `StaffTeachingAssignment` describes the session, class section and course offering that the Teacher actually teaches. One Teacher may have multiple effective teaching assignments.

Position and AccessRole must remain separate. Position describes the job; AccessRole controls application permissions. Capabilities and duties describe professional requirements and expected work, but they do not grant software access.

## Common authentication structure

The current `LoginCredential` is directly tied to a mandatory StaffMemberId, so it cannot be reused for Students by treating them as StaffMembers. Authentication must be migrated in a dedicated, compatibility-controlled phase.

The recommended future structure keeps `LoginCredential` responsible only for credentials and security state. A `LoginSubject` or equivalent persona mapping links the credential to exactly one StaffMember, Student or Guardian for each persona. `UserAccessRole` attaches permission roles to that persona.

This supports the following accounts:

- Teacher persona with Faculty permissions.
- Student persona with Student portal permissions.
- Guardian persona limited to actively linked Students.
- Institution Administrator persona linked to the existing StaffMember and Institution Administrator Position.
- One person with multiple personas, such as a Teacher who is also a Guardian, without duplicating passwords or business records.

The login session must carry InstitutionId, LoginSubjectId, SubjectType, the relevant subject identifier and active AccessRole. Existing staff claims and permission behavior must remain compatible during migration. Student and Guardian permissions must use the same established permission pipeline rather than a parallel authorization system.

Not every Student requires a login. Younger learners may be represented only through a Guardian account. A Student portal account should be created only when the Institution's configuration and verified contact policy require it.

## End to end operating flow

1. The Institution configures AcademicSession, Programme, AcademicGroup or Grade, ClassSection, Subject and CourseOffering.
2. The Institution configures Teaching StaffCategory, academic JobFamily, required capabilities, duties, Teacher Position and Faculty AccessRole.
3. A StaffMember is created, assigned the Teacher Position and Faculty role, then linked to classes and subjects through StaffTeachingAssignment.
4. Admission staff save a StudentAdmission as Draft, add Guardian relationships and documents, then submit it for review.
5. An authorized reviewer approves, rejects, cancels or returns the application with an auditable reason.
6. Approval creates Student, active StudentEnrollment, Guardian mappings and an initial StudentSectionAssignment in one transaction.
7. Mandatory subjects are derived from approved offerings; elective selections are stored through StudentCourseEnrollment where applicable.
8. Portal invitations are sent only to eligible Student or Guardian identities. Credential activation and academic enrollment remain separate states.
9. A Teacher sees only Students in their effective StaffTeachingAssignments and records lesson plans, delivery, attendance, homework and assessments against the correct class and course.
10. A Student sees only their own academic information. A Guardian sees only Students linked through active StudentGuardian permissions.
11. At session end, enrollment is completed or withdrawn. Promotion creates a new enrollment and section assignment while the permanent Student, Guardian links, account and historic results remain intact.

## Integrity and security rules

- Student must never reuse StaffMember or recruitment Candidate records.
- Every relationship must validate that all referenced records belong to the same Institution.
- Section and course assignments must belong to the enrollment's active AcademicSession and academic structure.
- Effective date ranges must be valid and historical assignments must not be overwritten.
- Position, capabilities and duties must never grant application permissions implicitly.
- Student, Guardian and Teacher visibility must be enforced by the existing server permission pipeline and data-scope checks.
- Deactivating a login must not delete the Student, StaffMember, Guardian, enrollment or academic history.
- Withdrawing an enrollment must not delete the permanent Student identity.
- Login IDs must be normalized and unique according to the future approved multi-institution account rule.
- Passwords, tokens and sensitive identity data must continue to use the existing security and encryption controls.

## Deferred implementation boundary

Before implementation, the Student phase requires an approved dependency inventory covering PostgreSQL objects, EF mappings, repositories, handlers, DTOs, API routes, login claims, roles, permission discovery, Angular routes and stores, seed data, bulk import, tests, rollback and client documentation. The current priority is completing the Staff Position migration and Staff client training document.
