# Staff canonical routes

## Current contract

The API publishes only the following Staff routes. Angular must use these routes and must not call an `Employee` compatibility alias.

- `/api/Staff`
- `/api/Staff/Bank`
- `/api/Staff/Contact`
- `/api/Staff/Dependent`
- `/api/Staff/Education`
- `/api/Staff/Experience`
- `/api/Staff/Sensitive`
- `/api/Staff/Monitoring`
- `/api/StaffEmploymentType`
- `/api/StaffReportingMapping`
- `/api/StaffCampusAssignment`
- `/api/StaffDeviceEnrollment`
- `/api/StaffWorkArrangement`
- `/api/StaffWorkPattern`
- `/api/StaffWorkModeOverride`

Authentication and permission checks remain in the existing authenticated menu and permission pipeline. The UI must resolve current Module and Operation identifiers from that pipeline; numeric identifiers must not be hard-coded.

## Tested status

On 2026-10-11, reflection regression tests verified that every controller above publishes its Staff route and does not publish an `Employee` route. The Angular service audit found the active consumers already using the canonical Staff URLs. Local API build and Angular production build are the release gates for this contract.
