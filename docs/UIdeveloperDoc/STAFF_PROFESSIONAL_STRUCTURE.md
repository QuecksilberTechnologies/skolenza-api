# Staff professional structure — API and UI handoff

## Behaviour

Phase 1B/1C separates a StaffMember's professional position from academic
class/subject allocation. An institution owns editable copies of the platform
templates. Staff creation selects Category → Job Family → Position together
with Department, Designation, Employment Type and Role. The API creates one
active primary `StaffPositionAssignment`, copies the position's default duties
as active assignments, and copies required capabilities as `Pending
Verification` assignments.

Changing a position closes the previous effective-dated primary assignment and
its generated duties/capabilities. A database trigger prevents overlapping
active primary positions. Class, subject, syllabus and substitute-teacher
allocation remain Phase 2.

## Authentication and permission discovery

All routes require an institution bearer token. They use the existing
`STAFF_DIRECTORY` permission pipeline. UI code must resolve current
ModuleId/OperationId through the authenticated navigation/permission flow;
numeric examples must never be persisted. Master and position mutations use
Add/Update/Delete operations. Capability verification currently uses the
published `STAFF_DIRECTORY` Update operation. Institution Administrators may
approve. The UI keeps Staff Manager approval disabled until the central menu
catalogue publishes an explicit Approve operation for this module; the client
does not invent an operation or numeric identifier.

## Routes

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/Staff/professional-structure/masters/{1|2|3}?activeOnly=true` | Job Family, Capability or Duty Definition masters |
| POST | `/api/Staff/professional-structure/masters/{type}` | Create institution master |
| PUT | `/api/Staff/professional-structure/masters/{type}/{id}` | Update institution master |
| DELETE | `/api/Staff/professional-structure/masters/{type}/{id}` | Dependency-safe soft delete |
| GET | `/api/Staff/professional-structure/positions?activeOnly=true&staffCategoryId=2` | Positions with mapped capabilities/duties |
| POST/PUT | `/api/Staff/professional-structure/positions[/{id}]` | Create/update position and mappings |
| DELETE | `/api/Staff/professional-structure/positions/{id}` | Dependency-safe soft delete |
| GET | `/api/Staff/professional-structure/staff/{encodedStaffMemberId}` | Effective professional profile |
| PUT | `/api/Staff/professional-structure/capability-verification/{assignmentId}` | Approve/reject capability evidence |

Master request example:

```json
{"code":"ACADEMIC_TEACHING","name":"Academic Teaching","description":"Teaching roles","displayOrder":10,"isActive":true,"requiresCertification":false,"certificationValidityRequired":false,"defaultFrequency":null,"requiresEvidence":false}
```

Position request example:

```json
{"staffJobFamilyMasterId":21,"staffCategoryId":2,"departmentId":7,"designationId":12,"code":"PRIMARY_TEACHER","name":"Primary Teacher","description":"Primary section teacher","displayOrder":10,"isActive":true,"capabilityIds":[31,32],"dutyDefinitionIds":[41,42]}
```

Staff create/update adds `staffPositionMasterId`. The server validates that the
position belongs to the same institution and that its `staffCategoryId` matches
the StaffMember category. The onboarding cascade is driven by configured
positions: select Category, load positions for that category, derive the
available Job Families from those positions, then filter Position by the chosen
Job Family. A Job Family with no position for the selected category is not a
valid onboarding option. Example:

```json
{"firstName":"Ananya","lastName":"Mehta","officialEmail":"ananya.mehta@example.edu","staffCategoryId":2,"staffPositionMasterId":51,"departmentId":7,"designationId":12,"staffEmploymentTypeId":3,"roleId":8,"dateOfBirth":"1992-04-18T00:00:00Z","dateOfOnBoarding":"2026-10-12T00:00:00Z","genderId":2,"countryId":101,"hasPermanent":true,"isActive":true}
```

Capability verification request:

```json
{"assignmentId":88,"isApproved":true,"remark":"Certificate verified against institution record"}
```

Standard responses use `{ isSucceeded, message, errors, data }`. Validation is
400, permission denial 403, missing data 404, and dependency/overlap conflict
409. Writes are synchronous; there is no polling or cancellation workflow.

## Persistence and UI

Masters use `StaffJobFamilyMaster`, `StaffPositionMaster`,
`StaffCapabilityMaster`, and `StaffDutyDefinitionMaster`. Mapping and effective
assignment tables are documented in the migration. All rows are institution
isolated. The Angular routes are `/app/staff/new`,
`/app/staff/:staffMemberId/edit`, and `/app/staff/professional-structure`.
Create/edit is a full page, not a dialog. Overview shows position, job family,
capability verification state and active duties.

### Configuration order and inactive dependency behaviour

The UI presents the institution configuration in dependency order: **Job
Families → Staff Categories → Capabilities → Duties → Positions**. An
Institution Administrator first creates the reusable job families, then maps
the permitted families to each institution category. Capabilities and duties
are defined before a position so the position can select its requirements in
one save.

Every dependency selector follows the same lifecycle rule. While adding a new
record it offers active dependencies only. While editing an existing record,
an inactive dependency that is already assigned remains visible but disabled;
this preserves the historical relationship without permitting a new inactive
selection. For example, after **Head Mistress** is deactivated it is absent
from a new Position category list. A Position that already references Head
Mistress continues to display that value in disabled form until the user
chooses an active replacement.

The status filter defaults to **All**, so inactive institution categories,
masters and positions remain recoverable. Activate and Deactivate use the
shared confirmation dialog. **Proceed** performs the requested status change;
**Cancel** closes the dialog without calling the API. These status changes are
institution isolated and do not alter the host catalogue or another
institution's records.

## Status

Implemented across DB, API and Angular. The professional-structure migration is
reconciled on local and Render databases. On 2026-10-13 the unused legacy
`FunctionalId` and `ReferalId` fields were removed from persistence and consumer
contracts after both databases confirmed that they contained no values. Clients
must use the canonical position, capability and duty assignment contracts.
The dependency-order, inactive-option and reactivation behaviour was updated
and locally regression-tested on 2026-10-15. Deployment/browser acceptance is
tracked separately and is not implied by the local test result.
