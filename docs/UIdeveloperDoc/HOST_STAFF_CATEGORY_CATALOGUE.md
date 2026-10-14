# Host Staff Category Catalogue

## Purpose and ownership

The Host owns the central Staff Category catalogue. Institutions consume active catalogue rows when they build their own professional hierarchy; an Institution cannot create or change a platform category. Existing Institution mappings remain valid when a Host deactivates a catalogue row, while that row is no longer offered for new mappings.

The Host menu hierarchy is `Institution Master Data` (`HOST_INSTITUTION_MASTER_DATA`) → `Staff Category Catalogue` (`HOST_STAFF_CATEGORY_CATALOGUE`). Both modules use `ModuleScope = 2`. The child publishes `View`, `Add`, and `Update`; authenticated clients must resolve the current numeric module and operation IDs from the existing Host menu/permission flow.

## API contract

All routes require a Host session and the established runtime permission pipeline.

| Method | Route | Operation | Purpose |
| --- | --- | --- | --- |
| GET | `/api/Staff/Category/host` | View | List active and inactive platform categories. |
| POST | `/api/Staff/Category/host` | Add | Create a platform category. |
| PUT | `/api/Staff/Category/host/{id}` | Update | Edit, activate, or deactivate a platform category. |

GET query example: `?ModuleId=<resolved-module-id>&OperationId=<resolved-view-id>`.

POST/PUT JSON example:

```json
{
  "id": null,
  "code": "STUDENT_SERVICES",
  "name": "Student Services",
  "description": "Counselling, wellbeing and learner-support roles.",
  "displayOrder": 60,
  "isActive": true,
  "moduleId": 0,
  "operationId": 0
}
```

The UI interceptor replaces the illustrative permission IDs with IDs resolved from the authenticated catalogue. `code` and `name` are mandatory. Codes are trimmed and normalized to uppercase. Duplicate codes return HTTP 409. An unknown update ID returns HTTP 404. Non-Host callers or callers without the requested operation receive HTTP 403.

Persistence uses the global `StaffCategory` table. Institution-specific names and mappings remain in the Institution category hierarchy tables and retain tenant isolation.

## UI

Route: `/app/institution-master-data/staff-categories`. The page follows the existing Host catalogue list/form pattern. It supports create, edit, deactivate, and reactivate. Deactivation is reversible and does not delete dependent Institution data.

## Deployment

Run `database-scripts/SeedHostInstitutionMasterData.sql`. It is idempotent and is included in the canonical production seed runner. Deploy the API before the Angular UI so the new contract exists when the menu becomes visible.

## Verification status

Local automated contract tests and Angular route-permission tests passed on 2026-10-14. Fresh local Host and Institution API login/read-back also passed and is recorded in the scenario report. No new authenticated browser session was captured in that run; the attached screenshots came from the user. Render API/UI acceptance remains separate and pending.

Business-user setup walkthrough with supplied screenshots and the current local Host/Institution API read-backs: [Staff and Academic Setup — Hinglish Walkthrough](STAFF_AND_ACADEMIC_SETUP_WALKTHROUGH_HINGLISH.md).
