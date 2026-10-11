# Attendance Mode Analysis and Recommended Flow

Last reviewed: 2026-09-27

## Purpose

This document records the current attendance execution gaps and the recommended design for tenants
with and without biometric devices. It is an analysis and implementation guide. Items marked as
recommended or pending are not implemented behavior.

## Confirmed product requirement

- Biometric hardware is an optional tenant capability.
- A tenant that has not purchased biometric capability must not see Biometric, Installed Device,
  Device Configuration, Employee Device Enrollment, Card Inventory, or similar device-management
  menu items in its tenant application.
- Hiding device menus must not disable attendance. Employees may mark attendance through the
  channels enabled by the effective Attendance policy, such as Mobile or Web.
- Employee Device Enrollment is relevant only when the selected attendance channel requires a
  registered physical device or credential.
- Work arrangement, daily work mode, attendance channel, and physical location are different
  concepts and must remain separately validated.

## Terminology

| Concept | Answers | Examples |
| --- | --- | --- |
| Work arrangement | What is the employee's effective employment arrangement for a date? | Office, WFH, Hybrid, Field, Client Site |
| Resolved daily work mode | Where/how is the employee expected to work today? | Office today, WFH today, Client Site today |
| Attendance channel | How is the punch submitted? | Mobile, Web, Biometric, Manual |
| Attendance location | At which tenant/client/project location is a location-bound punch made? | Head Office, Client Site A, Project Site B |
| Verification method | What proves the punch is allowed? | GPS, geofence, installed device, enrollment, manual approval |

Calling Mobile, Web, or Biometric an employee work mode would mix unrelated rules. One employee can
work at a Client Site and mark attendance using Mobile; another can work at an Office and use a
biometric terminal.

## Current implementation observed

The backend attendance mark flow currently:

1. Resolves tenant and employee from the authenticated tenant token.
2. Finds exactly one active, non-deleted arrangement effective on the calculated business date.
3. Loads the arrangement's Attendance policy version configuration.
4. Accepts only active Mobile or Web attendance device types on the self-service endpoint.
5. Validates the policy's Mobile/Web switch.
6. Uses the arrangement's base `WorkMode` and the request location or arrangement primary location.
7. Applies location scope, employee assignment and GPS/geofence validation.
8. Enforces check-in/check-out ordering and idempotency, then persists the punch.

The current Angular header Check-In flow is separate from this backend flow. It stores attendance in
browser local storage and generates mock weekly records. It does not currently prove that a validated
attendance punch was saved in the database.

## Confirmed gaps

| ID | Gap | Impact |
| --- | --- | --- |
| AM-01 | Attendance runtime reads the arrangement's base mode but does not resolve today's work pattern. | Fixed Hybrid Office/WFH/Field/Client Site days execute incorrectly. |
| AM-02 | Approved work-mode overrides are not applied during attendance marking. | An approved exception may be ignored. |
| AM-03 | Hybrid is treated as a physical arrangement using its primary location. | The API cannot tell whether today is Office, WFH, Field, or Client Site. |
| AM-04 | Weekly-off pattern rows are not checked during attendance marking. | Normal attendance may be accepted on a configured weekly off. |
| AM-05 | Flexible Hybrid has no daily declaration/resolution record. | The expected mode and location for today are unknown. |
| AM-06 | Pattern and override locations are validated when configured but are not selected at attendance runtime. | Attendance can use the wrong location. |
| AM-07 | Requested location is not validated against the resolved daily mode because no daily mode is resolved. | Client Site/Field/Office compatibility can drift. |
| AM-08 | Field and Client Site have no dedicated policy controls for GPS versus fixed geofence. | The Office geofence switch is reused for different operational cases. |
| AM-09 | `AllowOutsideLocationWithApproval` is stored but its approval result is not resolved during attendance marking. | Configured policy behavior is incomplete. |
| AM-10 | Self-service mark accepts Mobile/Web only; Biometric and Manual are rejected. | Separate ingestion/manual workflows are required but are not connected here. |
| AM-11 | Employee Device Enrollment is not part of the self-service attendance decision. | This is correct for Mobile/Web, but physical-device ingestion still needs a validated path. |
| AM-12 | Active attendance device types can be listed even when the mark endpoint cannot use them. | UI can display a channel that the endpoint rejects. |
| AM-13 | Angular header Check-In uses local storage and mock data. | UI success can differ from database attendance. |
| AM-14 | Punches do not store the resolved work mode, pattern source, or override source. | Later audit cannot fully explain why a punch was accepted. |
| AM-15 | Marking can derive the business date from the selected location, while today's timeline uses head-office time zone. | Cross-time-zone users can see inconsistent “today” results. |
| AM-16 | Hybrid weekly/monthly quotas are stored but not evaluated during attendance. | Flexible/Hybrid compliance cannot be established. |
| AM-17 | Device menu visibility and policy channel availability are not documented as independent entitlement decisions. | A tenant without hardware could accidentally lose attendance UI or see unusable device UI. |

## Recommended design decision

Do not add an “attendance mode” field to Work Arrangement if it means Mobile/Web/Biometric/Manual.
The Attendance policy already owns allowed channels, and channel availability can change between
policy versions without changing the employee's work arrangement.

Keep these responsibilities:

- **Work Arrangement:** effective date range, base work mode, Hybrid type, primary location, policy version.
- **Work Pattern:** fixed Hybrid daily work mode/location and weekly off.
- **Work Mode Override:** approved temporary exception for a date range.
- **Attendance Policy:** allowed channels and verification requirements.
- **Tenant capability/entitlement:** whether hardware/device modules are purchased and visible.
- **Installed Device and Employee Device Enrollment:** physical-device inventory and credentials only.

The arrangement should map to one published Attendance policy version, as it does now. It should not
repeat that policy's channel switches. Duplicating them would create two sources of truth.

## Recommended effective attendance context

Create one central resolver used by attendance preview and punch persistence. For an authenticated
tenant employee and server timestamp, it should produce:

```text
EmployeeAttendanceContext
  WorkDate
  TimeZoneId
  WorkArrangementId
  PolicyVersionId
  ResolvedWorkMode
  ResolutionSource       Arrangement | Pattern | ApprovedOverride | DailyDeclaration
  WorkPatternId?
  WorkModeOverrideId?
  TenantLocationId?
  IsWorkingDay
  AllowedChannels
  RequiresGps
  RequiresGeofence
  RequiresDeviceEnrollment
  FailureCode?
  FailureMessage?
```

Resolution precedence:

1. Resolve exactly one active, non-deleted arrangement for the business date.
2. Apply one active Approved override covering the date, if present.
3. Otherwise, for Fixed Hybrid, use today's active weekday pattern.
4. Otherwise, for a non-Hybrid arrangement, use the arrangement mode.
5. For Flexible Hybrid, use an authorized daily declaration decision once that product behavior is approved.
6. Reject a weekly off unless a separately authorized attendance exception exists.
7. Load the mapped published Attendance policy version and derive allowed channels.
8. Validate resolved mode, resolved location, location type, employee assignment, date coverage and policy scope.
9. Apply the selected channel's verification rules.

## Mode and verification matrix

| Resolved daily mode | Location | Recommended verification |
| --- | --- | --- |
| Office | Required; compatible office location assigned to employee | Policy channel + office geofence when required |
| Work From Home | No tenant location | Policy must allow WFH; remote GPS when required |
| Client Site | Required; `ClientSite` location assigned to employee | Policy channel + client-site GPS/geofence rule |
| Field | Required when tied to Client/Project Site; product decision needed for travelling field work | Field GPS rule or assigned-site geofence |
| Weekly off | None | Reject normal punch; require an approved exception if the product supports it |

## Attendance channel rules

| Channel | Hardware purchase required | Employee device enrollment required | Execution path |
| --- | --- | --- | --- |
| Mobile | No | No | Authenticated employee self-service API with GPS as required |
| Web | No | No | Authenticated employee self-service API; location proof must follow policy |
| Biometric | Yes | Yes | Trusted installed-device ingestion; device, location and enrollment must be active |
| Manual | No biometric hardware | No | Authorized tenant user correction/entry with reason and audit trail |

The Attendance policy can expose a channel only when both conditions are true:

```text
Effective channel = policy allows channel AND tenant capability supports channel
```

For Mobile/Web, tenant capability is the base attendance feature. For Biometric, it is the purchased
hardware/device entitlement plus at least one eligible configured device where appropriate.

## Tenant menu and entitlement behavior

For a tenant without biometric capability:

- Hide Installed Devices, Device Configuration, Employee Device Enrollment, Card Inventory and other
  hardware-only modules through the existing module/plan/tenant permission pipeline.
- Do not hard-code menu hiding in components.
- Attendance Policy should either hide/disable the Biometric switch with a clear “Biometric capability
  is not enabled for this tenant” explanation.
- Mobile/Web/Manual switches remain based on the tenant's enabled software capabilities.
- Check-In remains visible when the effective policy exposes at least one supported self-service channel.

For a tenant with biometric capability:

- Show only device modules included by plan/tenant entitlement and role permission.
- Biometric policy selection still does not guarantee that a particular employee can punch.
- At runtime validate installed device, tenant/location ownership, device active/effective state,
  attendance-device flag, employee enrollment and credential deployment state.

## Fixed and Flexible Hybrid recommendation

### Fixed Hybrid

Use one weekday pattern per parent arrangement. The pattern determines today's concrete mode and
location. The Attendance policy determines how the employee may punch for that resolved mode.

### Flexible Hybrid

Do not force Mobile/Web/Biometric selection into the arrangement. The unresolved question is how the
employee declares today's work mode. Recommended product flow:

1. Employee selects today's Office/WFH/Field/Client Site mode before first check-in.
2. For a location-bound mode, employee selects an eligible assigned location.
3. The server validates the policy, quotas, assignment and location.
4. The declaration becomes immutable after the first punch except through an audited authorized correction.
5. Tenant policy decides whether a particular change needs approval.

This daily declaration needs explicit user approval before implementation because no existing entity
currently owns it.

## Recommended API/UI sequence

1. `GET attendance/context` returns the resolved context, allowed channels and actionable failure.
2. The header Check-In button loads that context instead of local storage.
3. If exactly one channel/location is valid, the UI can proceed directly after required GPS permission.
4. If several are valid, show only the eligible choices.
5. `POST attendance/mark` resolves the context again on the server and does not trust the preview.
6. Save the resolved mode, resolution source and relevant pattern/override/declaration references with the punch.
7. `GET attendance/today` uses the same context/time-zone service as marking.

## Clear tenant-facing failures required

- No effective work arrangement exists for the business date.
- More than one arrangement is effective for the date.
- Today's Fixed Hybrid pattern is missing or inactive.
- Today is configured as a weekly off.
- Flexible Hybrid requires today's mode declaration.
- The approved override is invalid/inactive or conflicts with another approved override.
- The resolved location is inactive, deleted, not assigned, outside its effective dates, or attendance-disabled.
- The location type is incompatible with the resolved work mode.
- The selected attendance channel is disabled by policy.
- Biometric capability is not enabled for the tenant.
- The installed device or employee enrollment is inactive/not deployed.
- GPS is missing, inaccurate beyond the accepted rule, or outside the geofence.
- Policy configuration for the published version is missing.

## Implementation order

1. Replace the Angular local-storage Check-In path with an API-backed context/read/mark flow.
2. Add the central attendance-context resolver and use it in preview, mark and today's timeline.
3. Resolve Fixed Hybrid patterns and approved overrides at runtime.
4. Persist resolution evidence on attendance punches.
5. Filter policy channels by tenant capability; control hardware menus through the existing entitlement and permission pipeline.
6. Complete trusted biometric ingestion and manual attendance as separate audited channels.
7. Decide and implement Flexible Hybrid daily declaration and Field travel behavior.
8. Add automated tenant-isolation, soft-delete, concurrency, timezone, pattern, override, channel, location,
   geofence and optional-hardware scenarios before deployed acceptance.

## Decisions still required before implementation

1. For Flexible Hybrid, can an employee self-declare today's mode, or must selected modes receive approval?
2. For travelling Field work without a fixed Client/Project Site, should GPS capture alone be sufficient?
3. Does Web attendance require geolocation, trusted network/IP, or no location proof when policy allows Web?
4. Who may create Manual attendance and what correction/approval workflow is required?
5. Can attendance be marked on a weekly off with an approved override, overtime request, or only an administrator correction?

