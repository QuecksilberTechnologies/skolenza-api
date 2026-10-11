# Host Tenant Verification Resend

## Behavior

`POST /api/Tenant/{tenantId}/resend-verification-by-host` resends the pending
Tenant onboarding email through the active Host default SMTP configuration.
The authenticated Host permission pipeline remains authoritative. The UI must
resolve the current module and operation IDs from the authenticated menu data;
numeric examples are not stable identifiers.

## Request

- Authentication: Host bearer token.
- Route: `POST /api/Tenant/{tenantId}/resend-verification-by-host`
- Body: none.
- Permission carrier: `ModuleId` and `OperationId` query values injected by the
  existing module-operation interceptor.

## Success

```json
{
  "isSucceeded": true,
  "message": "Tenant verification email sent successfully.",
  "data": true
}
```

## SMTP provider failure

Provider failures use HTTP `502` and a safe actionable message. Do not replace
the backend message with a generic UI string.

```json
{
  "isSucceeded": false,
  "message": "The email provider rejected the API server IP address. Authorize the server IP in the email provider and try again.",
  "errorCode": "SMTP_IP_NOT_AUTHORIZED"
}
```

Other stable provider codes are `SMTP_AUTHENTICATION_FAILED` and
`SMTP_PROVIDER_REJECTED`. The response never contains SMTP credentials or the
raw provider exception.

## Persistence and retry

Sending does not mark the Tenant verified. A pending onboarding credential
remains pending until the recipient completes the verification link. Retrying
is user initiated; the UI must prevent duplicate in-flight requests.

## Validation status

Local focused automation passed on 2026-09-29. The Render deployment and live
submission were verified on 2026-09-30 after the updated backend build was
deployed and both current Render outbound CIDR ranges were authorized in Brevo.
See the [deployed scenario report](../testing/tenant/resend-verification/2026-09-30.md).

## Intermittent SMTP failure diagnostic

A successful SMTP test from a developer workstation does not prove that the
Render service can authenticate. Render can originate an outbound connection
from any address in the ranges shown under the service's **Connect > Outbound**
tab. When Brevo SMTP-key IP blocking is active, authorize every listed CIDR;
authorizing one observed Render address can produce intermittent success and
`SMTP_IP_NOT_AUTHORIZED` failures as the egress address changes.

For a recurrence:

1. Confirm the Render environment setting is named `EmailConfig__Secret` and
   deploy the saved environment change.
2. Copy the complete current CIDR list from Render **Connect > Outbound**.
3. Compare it with Brevo **Security > Authorized IPs** and add every missing
   range. Inspect Brevo's **Unauthorized IP addresses** tab for rejected callers.
4. Repeat the real Host resend. Use a workstation SMTP test only to validate the
   credentials, sender and recipient; record it as a different caller path.
5. If Brevo has no matching transactional event, inspect authentication,
   connection and IP-authorization logs before diagnosing delivery or templates.

The verified 2026-09-30 Render ranges are recorded in the scenario report, not
as permanent application constants. Re-read them from Render because the
platform can change its network ranges.
