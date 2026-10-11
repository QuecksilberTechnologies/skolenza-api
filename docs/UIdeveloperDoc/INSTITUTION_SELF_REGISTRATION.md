# Institution self-registration

## Status
Implemented and locally verified on 2026-10-11. Deployment verification is still required after publishing.

## Public flow
- UI: `/auth/register-institution`
- API: `POST /api/Institution/create-institution`
- Authentication: anonymous
- `/auth/register-tenant` redirects to the canonical UI route.
- The retired `POST /api/Institution/create-tenant` alias is no longer exposed. Clients must use the canonical Institution route.

## Canonical request
```json
{"subscriptionPlanId":1,"institutionTypeId":1,"institutionName":"Greenfield International School","institutionCode":"GIS","staffCodePrefix":"GIS","includeYear":true,"includeMonth":true,"includeDepartment":false,"separator":"/","runningNumberLength":"4","institutionEmailDomain":"greenfield.edu","genderId":1,"institutionEmail":"admin@greenfield.edu","contactPersonName":"Aarav Sharma","contactNumber":"+919876543210","countryId":101}
```

The controller maps the public Institution/Staff contract into the established transactional pipeline. Subscription, campus, administrator, role, permission and email setup remain atomic.

## Response
```json
{"isSucceeded":true,"message":"Institution created successfully. Please check your email and set password.","data":{"success":true,"emailSent":true,"message":"Institution created successfully. Please check your email and set password."},"errors":[]}
```

## Database bootstrap
Run `database-scripts/deployment-bootstrap/004-self-registration-prerequisites.sql` through `psql` with `smtp_username` and `smtp_secret`. It seeds Gender, six approved global Staff Type templates, and the active default registration SMTP record without embedding credentials.

## Validation
- Angular registration/pricing: 53 passed.
- API creation/rollback regression: 17 passed, including two live database rollback paths.
- Release automation-test build: passed.
