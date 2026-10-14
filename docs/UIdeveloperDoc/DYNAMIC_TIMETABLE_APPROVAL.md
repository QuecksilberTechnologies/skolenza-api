# Dynamic timetable approval and publication

## Behavior

An institution configures one default timetable workflow and may add campus or section-specific workflows. The most specific active workflow wins: section, campus, then institution default. A workflow decides whether a teacher may propose a draft, which approval steps apply, and whether final approval publishes automatically.

Timetable entries use `DRAFT -> SUBMITTED -> APPROVED -> PUBLISHED`. Rejection returns the entry to `REJECTED`, where it can be corrected and resubmitted. Published rows are immutable. `Create revision` copies a published row into a new Draft; publishing that revision archives the prior published revision.

Approval step types are `MANAGER`, `ACCESS_ROLE`, `POSITION`, and `STAFF_MEMBER`. Targets come from authenticated API options. The API validates the current actor against the configured step and tenant. The existing module-operation permission pipeline remains required on every route; UI clients must resolve current numeric IDs from `GET /api/Navigation/my-menu` and must not hardcode them.

## Routes

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/Staff/operations/timetable/workflows` | List institution workflows and steps |
| POST | `/api/Staff/operations/timetable/workflows` | Create workflow |
| PUT | `/api/Staff/operations/timetable/workflows/{id}` | Update workflow |
| POST | `/api/Staff/operations/timetable/entries` | Create Draft |
| PUT | `/api/Staff/operations/timetable/entries/{id}` | Edit Draft or Rejected row |
| POST | `/api/Staff/operations/timetable/entries/{id}/submit` | Validate conflicts and submit |
| POST | `/api/Staff/operations/timetable/entries/{id}/actions` | `APPROVE`, `REJECT`, or `PUBLISH` |
| POST | `/api/Staff/operations/timetable/entries/{id}/revisions` | Create Draft revision from Published |
| GET | `/api/Staff/operations/timetable/entries/{id}/history` | Approval audit trail |

## Workflow request

```json
{
  "code": "STANDARD_TIMETABLE_APPROVAL",
  "name": "Standard Timetable Approval",
  "campusId": null,
  "classSectionId": null,
  "allowTeacherDraftProposal": true,
  "autoPublishAfterFinalApproval": true,
  "isDefault": true,
  "isActive": true,
  "steps": [
    {
      "sequence": 1,
      "approverTypeCode": "ACCESS_ROLE",
      "accessRoleId": 25,
      "isActive": true
    }
  ]
}
```

The numeric role above is illustrative. Resolve the institution's active role from `/api/Staff/operations/options`.

## Errors

- `400`: invalid scope, sequence, approver target, teacher proposals disabled, or actor is not current approver.
- `404`: tenant-scoped row does not exist.
- `409`: invalid lifecycle transition, duplicate default workflow, edit of immutable entry, or published timetable conflict.
- `401/403`: authentication or permission pipeline rejection.

## Persistence and isolation

`TimetableWorkflowMaster`, `TimetableWorkflowStep`, `TimetableEntry`, and `TimetableApprovalHistory` are institution-scoped. Existing entries are migrated as Published revision 1. All reads and writes include authenticated `InstitutionId`; configured role, position, named staff and reporting-manager checks are also tenant-scoped.

## Verification status

- Local migration rerun: passed.
- Local seed and tenant counts: passed.
- Backend focused Phase 1-5 staff tests: passed.
- Authenticated local revision, submit, approval, auto-publication and history: passed.
- Angular route regression and production build: passed.
- Render database migration and seed: passed.
- Deployed API/UI acceptance: pending deployment of the new commits.

## Actor-specific action projection

Each timetable row returns `canReview` and `canPublish` for the authenticated actor. The server resolves these flags from the current workflow step and validates them again when an action is posted. The UI must render Approve/Reject only for `canReview=true` and Publish only for `canPublish=true`; status alone never grants an action. Local focused tests and the Angular production build passed on 2026-10-14. Deployment acceptance remains pending.

## Functional walkthrough

Professional structure se timetable approval, substitute aur communication tak complete flow: [Staff Phase 1–5 Complete Functional Flow — Hinglish Guide](STAFF_PHASE_1_TO_5_COMPLETE_FLOW_HINGLISH.md).
