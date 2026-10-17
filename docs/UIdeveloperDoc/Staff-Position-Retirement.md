# Staff Position retirement handoff

## Resulting behavior

`StaffPositionMaster` is the only selectable current job-title master. Staff create/edit, list, login/bootstrap, profile, dashboards, policies and monitoring use `StaffPositionMasterId` and `PositionName`. Previous-employer text is preserved separately as `PreviousPositionTitle` and `ColleaguePositionTitle`.

The legacy Designation controller, Angular route, API client, bulk page and permission leaf are retired. UI consumers must discover the current Professional Structure and Staff Directory module/operation IDs through the authenticated menu/permission flow; numeric IDs are never fixed client constants.

## Staff create and update contract

- `StaffPositionMasterId` is mandatory for current employment.
- Select Staff Category first, then Job Family, then Position. Options are institution-scoped and active.
- Department remains an independent organizational assignment. The selected Position must belong to the same institution and its department scope must be compatible with the staff department when the Position is department-bound.
- Do not send `DesignationId`. Strict policy applicability input rejects unknown legacy fields rather than silently broadening a policy.

Representative JSON fragment:

```json
{
  "departmentId": 12,
  "staffCategoryId": 3,
  "jobFamilyId": 8,
  "staffPositionMasterId": 61
}
```

## Read contracts

Current employment responses expose `StaffPositionMasterId` and `PositionName`. A scheduled future Position does not replace the current read until its effective date. Profile experience responses expose `PreviousPositionTitle` and `ColleaguePositionTitle` because they describe historical external employment, not a current selectable Position.

## Policy targeting

Policy applicability uses optional `StaffPositionMasterId`. The backend validates active state, institution ownership and department compatibility. Existing approved policy fingerprints retain their pre-cutover shape when they had no Position target; this prevents retirement alone from invalidating an approval.

## Bulk import

The persisted legacy bulk master slot `2` is reserved as `RetiredWorkTitle` and must never be reused. Preview, act and worker execution reject that slot. Staff bulk import uses the canonical Position field and dependency order; the removed Designation template/API must not be restored.

## Deployment order

1. Stage the approved Position-only API and existing Position UI while stage 1 compatibility columns still exist. Before starting that API, back up the target and run `database-scripts/migrations/20261017_PreparePositionHistoryColumns.sql`. This renames only three external-work-history text columns, retains their values and is repeatable. Coordinate the API switch: an old binary expecting the previous column names must not remain active against the renamed history schema.
2. Verify login, staff list/create/edit/profile, policy assignment and permissions against Development. History preparation is independent of destructive master retirement, so these checks no longer depend on first dropping the legacy table.
3. Stop writes, take a full backup, then run stage 2 of `database-scripts/migrations/20261016_RetireDesignationIntoPositions.sql`.
4. Repeat smoke checks. Apply the same controlled sequence to Render.

Stage 2 reuses the same idempotent history preparation script and hard-drops legacy foreign keys/table. It remains gated by deployment acceptance. Do not run destructive Stage 2 merely to make history reads work.

### 17 October local reconciliation

- User explicitly approved local history renames while preserving the legacy table and leaving Render unchanged. A full local backup was taken; both history tables had zero rows before/after, with matching fingerprints. Repeating the preparation script succeeded.
- The Position-only local API now returns both saved staff rows, including Mathematics Teacher, and the UI teacher summary/profile display Position without Designation.
- Experience's existing `StaffMemberIdOfInstitution` database column is mapped explicitly to the API model's `StaffMemberIdOfCompany`; the mismatch was caught by the seeded rollback test. No text or identity data was rewritten.
- Dashboard Department Headcount ordering now occurs before DTO construction so EF translates it to PostgreSQL. The read contract and permission pipeline are unchanged.
- Protected pre-change profile/contact/role gate: 78/78. Dashboard/Staff gate: 23/23. Post-change combined gate: 109/109, zero failed/skipped, including seeded history preservation/idempotency and actual PostgreSQL headcounts.
- Local UI pagination still reports zero entries despite displaying two rows. The directory On Leave card displays a placeholder count. These are handed to the UI developer; no Angular changes were made.
- [Current local regression evidence](../testing/staff/position-history-preparation/2026-10-17.md). Render preparation, deployment and destructive Stage 2 remain pending.

## Tested and deployed status

- Local backend source/tests: PASS; see the linked scenario report.
- Local Angular focused tests and production build: PASS.
- Development/Render stage 1 data mapping: APPLIED.
- Stage 2 and independently verified deployed API/UI: PENDING.
