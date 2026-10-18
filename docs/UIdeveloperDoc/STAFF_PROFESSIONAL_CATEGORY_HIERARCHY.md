# Institution Staff Category Hierarchy and Professional Structure UI

## Behavior

Staff Category has two levels. `StaffCategory` is the platform semantic type catalogue. `InstitutionStaffCategoryMaster` is the institution-owned working catalogue shown in Staff Professional Structure and Staff create/edit. Each institution category explicitly maps allowed Job Families; a Position is valid only inside one active Category–Job Family mapping.

Example: Red Rose creates `Teaching Faculty` from canonical type `Teaching`, maps `Academics`, then creates `Mathematics Teacher`. The position can require `Subject Knowledge` while keeping `Smart Board Operation` optional, and can require duties such as `Prepare lesson plans`. Staff create sends the local category and position. The API derives the canonical type and creates the effective position/capability/duty assignments.

## Authentication and permissions

All routes require authentication and use the existing `STAFF_DIRECTORY` menu/operation pipeline. The Angular interceptor discovers current ModuleId and OperationId; consumers must not hardcode numeric IDs.

## Routes

**2026-10-18 pagination:** all five professional-structure tab lists now use
the existing central query paging helper. Default `PageNumber=1`, `PageSize=10`.
See [Professional Structure pagination](STAFF_PROFESSIONAL_PAGINATION.md) for
query/response examples and the exact Angular integration boundary.

| Method | Route | Operation | Purpose |
| --- | --- | --- | --- |
| GET | `/api/Staff/professional-structure/categories?activeOnly=false` | View | List local categories, mapped family IDs and dependency counts |
| POST | `/api/Staff/professional-structure/categories` | Add | Create local category |
| PUT | `/api/Staff/professional-structure/categories/{id}` | Update | Edit/reactivate local category |
| PUT | `/api/Staff/professional-structure/categories/{id}/job-families` | Update | Replace allowed family mappings |
| PUT | `/api/Staff/professional-structure/categories/{id}/active/{bool}` | Update | Dependency-safe activate/deactivate |
| GET/POST/PUT/DELETE | `/api/Staff/professional-structure/masters/...` | View/Add/Update/Delete | Manage Job Families, Capabilities and Duties; DELETE deactivates |
| GET/POST/PUT/DELETE | `/api/Staff/professional-structure/positions/...` | View/Add/Update/Delete | Manage positions; DELETE deactivates |

## Copyable requests

```json
{
  "staffCategoryTypeId": 2,
  "code": "TEACHING_FACULTY",
  "name": "Teaching Faculty",
  "description": "Academic teaching staff",
  "displayOrder": 10,
  "isActive": true
}
```

```json
{
  "institutionStaffCategoryMasterId": 21,
  "staffJobFamilyMasterIds": [31]
}
```

```json
{
  "institutionStaffCategoryMasterId": 21,
  "staffJobFamilyMasterId": 31,
  "code": "MATH_TEACHER",
  "name": "Mathematics Teacher",
  "displayOrder": 10,
  "isActive": true,
  "capabilities": [
    { "id": 41, "isMandatory": true, "displayOrder": 10 },
    { "id": 42, "isMandatory": false, "displayOrder": 20 }
  ],
  "duties": [
    { "id": 51, "isMandatory": true, "displayOrder": 10 }
  ]
}
```

Staff create and official update use `institutionStaffCategoryMasterId` plus `staffPositionMasterId`. The API rejects cross-tenant, inactive, or unmapped combinations with validation/conflict responses. Deactivation is leaf-first: active staff/positions/mappings must be handled before their parent category or family.

## Persistence and rollout

Migration: `database-scripts/migrations/20261013_AddInstitutionStaffCategoryHierarchy.sql`. It is idempotent, seeds local copies from platform templates, derives current mappings, and backfills Staff and Position local category references. Rollback is available under `database-scripts/rollback`. The migration ran on local and Render databases on 2026-10-13; API/UI deployment still requires the corresponding commits to be published before browser acceptance.

For a business-user explanation with screenshots and current Red Rose setup evidence, see [Staff and Academic Setup — Hinglish Walkthrough](STAFF_AND_ACADEMIC_SETUP_WALKTHROUGH_HINGLISH.md).
## 2026-10-17 onboarding correction

Host-created category IDs are not limited to the original five seeds. Apply
`database-scripts/migrations/20261017_AllowHostManagedStaffCategories.sql` before
onboarding staff under extended categories. It preserves the authoritative
catalogue FK. This continuation applied it to local only; deployment is pending.

`POST /api/Staff/create` keeps its existing inputs and permission pipeline. The
creation response now resolves the saved primary Position after assignment and
returns the institution category name. No Angular implementation is required for
the additive response correction. Existing consumers must continue using the
API-backed category/family/Position catalogues and dynamic permission discovery.

Local acceptance: [scenario and exact limits](../testing/staff/host-category-onboarding/2026-10-17.md).
Teacher account creation, named academic allocation and approved delivery progress
passed locally. Welcome email was rejected by the external provider's IP policy;
first login and teacher-role grants have not passed. No Angular changes were made.
