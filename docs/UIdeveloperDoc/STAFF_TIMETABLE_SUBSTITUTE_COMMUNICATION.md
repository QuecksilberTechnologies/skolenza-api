# Staff timetable, substitute coverage and communication

## Scope

Phase 4 provides institution-owned periods, timetable entries, approved-leave impact synchronization and configurable substitute approval. Phase 5 provides institution-owned query categories, direct/query conversations and server-resolved broadcasts. Every endpoint is bearer authenticated and uses the existing `STAFF_DIRECTORY` module-operation pipeline; the UI must discover current numeric IDs from the authenticated navigation response.

Base route: `/api/Staff/operations`.

## Dynamic options

`GET /options` returns active tenant-owned staff members, campuses, departments, staff categories, access roles and positions. Consumers must not hardcode these catalogues.

## Timetable and coverage

- `GET|POST /timetable/periods`, `PUT /timetable/periods/{id}`
- `GET|POST /timetable/entries`, `PUT /timetable/entries/{id}`
- `GET /absence-impacts`
- `POST /absence-impacts/leave/{leaveRequestId}/synchronize`
- `POST /substitute-assignments`
- `POST /substitute-assignments/{id}/actions`

Example assignment body:

```json
{"staffAbsenceImpactId":21,"substituteWorkflowMasterId":4,"substituteStaffMemberId":88,"remark":"Grade 5-A mathematics coverage"}
```

Action codes are `APPROVE`, `REJECT`, `ACCEPT`, `DECLINE` and `CANCEL`. Manager, access-role, position and named-staff approvers are resolved by the server within the institution. A configured workflow may require substitute acceptance.

## Communication

- `GET|PUT /communication/settings`
- `GET|POST /communication/query-categories`
- `PUT|DELETE /communication/query-categories/{id}`; delete deactivates, editing with `isActive=true` reactivates.
- `GET|POST /communication/conversations`
- `GET /communication/conversations/{id}`
- `POST /communication/conversations/{id}/messages`
- `POST /communication/conversations/{id}/read`
- `POST /communication/conversations/{id}/close`

Conversation types are `DIRECT`, `QUERY`, and `BROADCAST`. Query requires an active tenant query category. Direct and query require recipient staff IDs. Broadcast audiences accept `CAMPUS`, `DEPARTMENT`, `STAFF_CATEGORY`, `ACCESS_ROLE`, or `STAFF_MEMBER`; the API validates target ownership and stores the resolved recipient snapshot as participants.

```json
{
  "conversationTypeCode":"BROADCAST",
  "subject":"Campus briefing",
  "initialMessage":"Briefing starts at 08:00.",
  "participantStaffMemberIds":[],
  "audiences":[{"audienceTypeCode":"CAMPUS","targetId":3}]
}
```

Successful responses use the existing `ApiDataResponse<T>` envelope. Validation/ownership/conflict failures use the established API exception middleware. Local build, schema and seed verification passed on 2026-10-14; deployed acceptance must be recorded separately after the development deployment is live.
