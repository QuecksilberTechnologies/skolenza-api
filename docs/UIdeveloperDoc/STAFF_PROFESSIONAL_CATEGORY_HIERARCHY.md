# Institution Staff Category Hierarchy and Professional Structure UI

## Behavior

Staff Category has two levels. `StaffCategory` is the platform semantic type catalogue. `InstitutionStaffCategoryMaster` is the institution-owned working catalogue shown in Staff Professional Structure and Staff create/edit. Each institution category explicitly maps allowed Job Families; a Position is valid only inside one active Category–Job Family mapping.

Example: Red Rose creates `Teaching Faculty` from canonical type `Teaching`, maps `Academics`, then creates `Mathematics Teacher`. The position can require `Subject Knowledge` while keeping `Smart Board Operation` optional, and can require duties such as `Prepare lesson plans`. Staff create sends the local category and position. The API derives the canonical type and creates the effective position/capability/duty assignments.

## Authentication and permissions

All routes require authentication and use the existing `STAFF_DIRECTORY` menu/operation pipeline. The Angular interceptor discovers current ModuleId and OperationId; consumers must not hardcode numeric IDs.

## Routes

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
