# Institution, module and device canonical routes

## Behaviour

Skolenza navigation has exactly two catalogue levels: a **Parent Header** and its direct **Child Options**. A child cannot own another module. The database trigger rejects a third level and the Angular parent detail no longer provides drill-down.

## Auth and permissions

Use the existing authenticated menu and permission pipeline. Resolve module and operation IDs dynamically from the signed-in user's menu response; never hardcode numeric IDs.

## Canonical routes

| Area | Canonical API prefix | Compatibility prefix |
| --- | --- | --- |
| Institution devices | `/api/InstitutionDevice` | `/api/TenantDevice` |
| Institution device configuration | `/api/InstitutionDeviceConfiguration` | `/api/TenantDeviceConfiguration` |
| Parent headers | `/api/ParentHeader` | `/api/ParentModule` |
| Child options | `/api/ChildModule` | `/api/SubModule` |
| Child option import | `/api/ChildModule/import` | `/api/SubModule/import` |

Methods, payload shape, pagination, cancellation and response envelope remain compatible during this migration slice. Existing physical `TenantId`/`TenantLocationId` JSON fields remain compatibility fields until the separately coordinated column and DTO migration; the UI must display them as Institution and Campus. Do not create a second client-side permission path.

## UI routes and copy

- `/app/institutions`
- `/app/institution-devices`
- `/app/institution-device-configurations`
- `/app/modules` for parent headers and direct child options

Use Institution, Campus, Staff, Parent Header and Child Option in titles, breadcrumbs, labels, validation, confirmation dialogs and errors.

## Validation status

Locally verified on 2026-10-11 with 77 focused Angular tests, the two-level database probe, focused backend contract/Host tests, Release build and Angular production build. Deployment verification is pending.
