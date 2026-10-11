# Institution endpoint compatibility

Skolenza Angular must use the canonical Institution controller segments below. The API keeps the
legacy Tenant segments as temporary compatibility aliases; new UI code must not use those aliases.

| Feature | Canonical API segment | Legacy compatibility segment |
| --- | --- | --- |
| Current-user access bootstrap | `/api/InstitutionUserAccess` | `/api/TenantUserAccess` |
| Policies | `/api/InstitutionPolicy` | `/api/TenantPolicy` |
| Institution email templates | `/api/InstitutionEmailTemplate` | `/api/TenantEmailTemplate` |
| Institution SMTP configuration | `/api/InstitutionEmailConfig` | `/api/TenantEmailConfig` |
| Institution card inventory | `/api/InstitutionCardMaster` | `/api/TenantCardMaster` |
| Institution card bulk import | `/api/InstitutionCardMaster/import` | `/api/TenantCardMaster/import` |

Authentication, request bodies, query parameters, response JSON and permission IDs are unchanged
in this compatibility slice. The Angular module-operation interceptor resolves canonical and legacy
segments to the same authenticated menu module and operation. Numeric IDs must continue to come
from the authenticated navigation/permission flow.

Local verification on 2026-10-11 covers controller route reflection, locked Institution creation,
Campus lifecycle, role mapping, Angular permission-route resolution and production compilation.
Deployment verification remains pending.
