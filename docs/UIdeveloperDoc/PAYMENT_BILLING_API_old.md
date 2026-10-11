# Subscription payment and billing design

## Status

The guarded schema, centralized settings and HostAdmin billing-configuration GET/PUT endpoints were added on 2026-09-24. Gateway calls, checkout/refund endpoints, webhook processing, reconciliation worker, invoice rendering and live sandbox acceptance are **PENDING**.

## Approved business behavior

- Initial launch country: India; launch currency: INR.
- Gateway adapter: Cashfree Payments. Core persistence uses gateway-neutral identifiers so another adapter can be added later.
- Billing cycles: monthly and yearly, selected by the Tenant.
- Recurring mandate/auto-renewal is the intended production model.
- Upgrade: effective immediately after the prorated payment is verified.
- Downgrade: scheduled for the next billing period; current entitlement remains unchanged until then.
- Refund: partial or full amount initiated only by an authorized Host administrator. Total successful refunds cannot exceed the captured amount.
- Application retry limit: 3. Grace period: 1 day. Both come from the `Billing` configuration section and are not embedded in processing logic.
- India GST is selected from effective-dated tax data. Same-state supply splits GST into CGST/SGST; interstate supply uses IGST. Seller registration and state must be configured before invoices can be issued.
- Future countries receive fixed country/currency plan-price rows and effective-dated tax rules. Checkout never performs live foreign-exchange conversion.

## Security boundaries

- Cashfree credentials are read only from `CASHFREE_CLIENT_ID`, `CASHFREE_CLIENT_SECRET`, and `CASHFREE_WEBHOOK_SECRET`.
- Only authenticated HostAdmin configuration endpoints may create, update or activate the gateway, seller/GST configuration, country prices and tax rules. Tenant APIs cannot mutate these records.
- The UI never receives a secret. It receives only the provider checkout/session identifier.
- The browser redirect is informational. It never activates a subscription.
- A subscription is activated or renewed only after signature-verified webhook processing or server-to-server reconciliation confirms the expected order, Tenant, currency and amount.
- Raw webhook payload and its processing outcome are retained. Duplicate gateway event IDs are idempotently ignored.
- Card, bank and UPI credentials are never stored in Skolenza.
- Refund operations use the existing Host permission pipeline and require a new idempotency key for each intended refund.

## Persistence ownership

| Table | Purpose |
| --- | --- |
| `PaymentGateway` | Provider metadata and names of secret environment variables; never stores secrets. |
| `HostBillingConfiguration` | HostAdmin-owned seller/GST identity, active gateway, invoice prefix and bounded retry/grace settings. The seed is inactive until legal values are supplied. |
| `SubscriptionPlanPrice` | Effective-dated plan price per country, currency and monthly/yearly cycle. |
| `BillingTaxRule` | Effective-dated country tax rule and component configuration. |
| `TenantBillingProfile` | Legal name, invoice address, tax registration, country and billing currency snapshot source. |
| `TenantBillingSubscription` | Gateway mandate/subscription identity and current billing lifecycle. Links to the existing entitlement-oriented `TenantSubscription`. |
| `BillingOrder` | Immutable server-calculated checkout amount, tax, currency, expiry and idempotency identity. |
| `PaymentTransaction` | Every gateway payment result and bank reference. |
| `PaymentAttempt` | Retry sequence and next retry time; bounded by configured retry count. |
| `PaymentWebhookEvent` | Signed webhook inbox, duplicate protection, retries and processing errors. |
| `BillingInvoice` / `BillingInvoiceLine` | Legal invoice snapshot and tax breakdown. Historic invoices never recalculate when plan price/tax changes. |
| `BillingRefund` | Host-initiated partial/full refund lifecycle and provider reference. |
| `BillingSubscriptionChange` | Immediate upgrade or next-cycle downgrade request and proration. |
| `BillingAuditLog` | Append-only business audit for user, Host, webhook, worker and system transitions. |

The existing `TenantSubscription` continues to control product entitlement dates. Payment rows do not directly grant modules. Verified payment processing creates/renews the entitlement transactionally; failed or pending payment cannot activate it.

## Required API inventory

The following contracts must be implemented before release:

| Method | Route | Authentication and permission | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/host/billing/configuration` | Host token + dynamic Billing Configuration `View` permission | Returns seller/gateway settings and credential-presence flags without returning secrets. **Implemented.** |
| `PUT` | `/api/host/billing/configuration` | Host token + dynamic Billing Configuration `Update` permission | Updates India seller/GST identity and bounded operational settings with optimistic version checking. **Implemented.** |
| `GET/POST/PUT/DELETE` | `/api/host/billing/plan-prices` | `HOST_BILLING_PLAN_PRICES` + matching View/Add/Update/Delete operation | Effective-dated monthly/yearly country pricing; overlapping active windows are rejected. **Implemented.** |
| `GET/POST/PUT/DELETE` | `/api/host/billing/tax-rules` | `HOST_BILLING_TAX_RULES` + matching operation | Effective-dated tax rules and validated JSON configuration; overlapping active windows are rejected. **Implemented.** |
| `GET` | `/api/host/billing/transactions` | `HOST_BILLING_TRANSACTIONS` + View | Paged transaction search by status, tenant name or gateway payment ID. **Implemented.** |
| `GET` | `/api/host/billing/refunds` | `HOST_BILLING_REFUNDS` + View | Paged refund history. **Implemented.** |
| `POST` | `/api/host/billing/refunds` | `HOST_BILLING_REFUNDS` + Add | Validates successful payment, idempotency and remaining refundable balance, then queues `Requested`. **Implemented; provider worker pending.** |
| `GET` | `/api/host/billing/reconciliation` | `HOST_BILLING_RECONCILIATION` + View | Counts stale orders, webhook failures, failed attempts and queued refunds. **Implemented.** |
| `POST` | `/api/host/billing/webhooks/{id}/retry` | `HOST_BILLING_RECONCILIATION` + Update | Requeues a failed webhook within the configured retry limit. **Implemented; processor pending.** |
| `GET` | `/api/host/billing/audit` | `HOST_BILLING_AUDIT` + View | Paged billing audit trail. **Implemented.** |
| `GET` | `/api/billing/plan-prices` | Valid Tenant token | Country/currency-specific monthly/yearly choices. |
| `GET` | `/api/billing/current` | Valid Tenant token | Current billing, entitlement, pending change and grace state. |
| `PUT` | `/api/billing/profile` | Tenant billing Update permission | Validate and save legal invoice identity. |
| `POST` | `/api/billing/checkout` | Tenant subscription Add/Update permission | Server calculates price/tax and creates idempotent provider checkout. |
| `GET` | `/api/billing/orders/{id}` | Same Tenant or authorized Host | Server-verifies current payment state; never trusts redirect query values. |
| `POST` | `/api/billing/change-plan` | Tenant subscription Update permission | Create prorated upgrade checkout or schedule downgrade. |
| `POST` | `/api/billing/cancel-renewal` | Tenant subscription Update permission | Cancel at period end and provider mandate where applicable. |
| `GET` | `/api/billing/invoices` | Valid Tenant token | Tenant-isolated invoice history. |
| `GET` | `/api/billing/invoices/{id}/download` | Same Tenant or authorized Host | Download immutable invoice document. |
| `POST` | `/api/billing/webhooks/cashfree` | Anonymous transport; mandatory signature verification | Persist and idempotently process provider events. |
| `POST` | `/api/host/billing/refunds` | Host Refund operation | Validate refundable balance and initiate partial/full refund. |
| `GET` | `/api/host/billing/reconciliation` | Host View operation | Show mismatches, pending webhooks, stale orders and failed retries. |

Numeric ModuleId and OperationId values must be resolved through the authenticated menu/permission flow; they must not be hard-coded by the UI.

### HostAdmin screens and module codes

| UI screen | Module code | Available actions |
| --- | --- | --- |
| Billing Configuration | `HOST_BILLING_CONFIGURATION` | View, Update |
| Plan Pricing | `HOST_BILLING_PLAN_PRICES` | View, Add, Update, Delete (deactivate) |
| Tax Rules | `HOST_BILLING_TAX_RULES` | View, Add, Update, Delete (deactivate) |
| Payment Transactions | `HOST_BILLING_TRANSACTIONS` | View |
| Refunds | `HOST_BILLING_REFUNDS` | View, Add refund request |
| Reconciliation | `HOST_BILLING_RECONCILIATION` | View, Update for retry |
| Billing Audit | `HOST_BILLING_AUDIT` | View |

All seven are Host-scope children of `HOST_SUBSCRIPTIONS`. The UI must select the module and operation IDs returned by the authenticated Host menu. Each API checks bearer token, exact module code, exact operation name and persisted Host permission.

### Plan-price JSON

```json
{
  "permissionRequest": { "moduleId": 0, "operationId": 0 },
  "subscriptionPlanId": 1,
  "countryCode": "IN",
  "currencyCode": "INR",
  "billingCycle": "Monthly",
  "baseAmount": 999.00,
  "effectiveFrom": "2026-10-01",
  "effectiveTo": null,
  "isActive": true
}
```

### Tax-rule JSON

```json
{
  "permissionRequest": { "moduleId": 0, "operationId": 0 },
  "countryCode": "IN",
  "taxCode": "GST",
  "taxName": "Goods and Services Tax",
  "ratePercent": 18.0,
  "effectiveFrom": "2026-10-01",
  "effectiveTo": null,
  "isActive": true,
  "configurationJson": "{\"splitByPlaceOfSupply\":true,\"intraStateComponents\":[\"CGST\",\"SGST\"],\"interStateComponents\":[\"IGST\"]}"
}
```

### Refund-request JSON

```json
{
  "permissionRequest": { "moduleId": 0, "operationId": 0 },
  "paymentTransactionId": 1,
  "amount": 250.00,
  "reason": "Approved partial service refund",
  "idempotencyKey": "de305d54-75b4-431b-adb2-eb6b9e546014"
}
```

The refund response remains `Requested` until the provider worker receives a successful Cashfree result. The UI must never display it as refunded before that transition.

### Host billing configuration request

Resolve `ModuleId` for module code `HOST_BILLING_CONFIGURATION` and its `Update` operation through the authenticated menu. The numeric values below are placeholders only.

```json
{
  "permissionRequest": { "moduleId": 0, "operationId": 0 },
  "sellerLegalName": "Example Private Limited",
  "sellerBillingEmail": "billing@example.com",
  "sellerBillingPhone": "+919999999999",
  "countryCode": "IN",
  "stateCode": "MP",
  "postalCode": "482001",
  "addressLine1": "Example registered address",
  "addressLine2": null,
  "taxRegistrationNumber": "23ABCDE1234F1Z5",
  "invoicePrefix": "AXP",
  "paymentRetryCount": 3,
  "gracePeriodDays": 1,
  "paymentTimeoutMinutes": 30,
  "reconciliationLookbackDays": 7,
  "isActive": true,
  "version": 1
}
```

The response includes `hasClientId`, `hasClientSecret`, and `hasWebhookSecret`. Credential values never appear in the API. A stale `version` returns a conflict and the UI must reload before retrying. Initial launch accepts only `countryCode: "IN"`.

## State transitions

1. UI selects a returned `SubscriptionPlanPrice` identifier and supplies an idempotency UUID.
2. Server resolves Tenant/country, recalculates current price and effective tax, snapshots the order and creates provider checkout.
3. Redirect displays Pending while server verification completes.
4. Verified success creates `PaymentTransaction`, invoice, audit and entitlement in one database transaction.
5. Failed renewal records the attempt. After three application attempts, billing enters `GracePeriod` for one configured day; after grace expiry it becomes `Suspended` and entitlement removal follows the approved entitlement sync process.
6. A later verified success clears grace and reactivates the same subscription safely.
7. Reconciliation polls stale Pending records and repairs missed webhooks without duplicating fulfilment.

## Validation still required

- Cashfree merchant KYC/agreement and Subscriptions activation.
- Approved seller legal name, invoice address, GSTIN and registered state.
- Cashfree sandbox keys and a publicly reachable sandbox webhook URL.
- Seeded India plan prices for every sellable plan/cycle.
- Automated unit, database concurrency, webhook replay/out-of-order, refund-limit, tax, proration and reconciliation tests.
- Authenticated sandbox checkout, mandate, renewal failure/retry, refund and invoice smoke tests.

## Remaining APIs and system processes

The following work is **not implemented yet** and is required before end-to-end billing can be released.

### Payment-provider integration

- Create Cashfree checkout/order and return the provider payment session identifier.
- Verify the current payment/order status with Cashfree server-to-server.
- Create and manage the recurring subscription or mandate.
- Cancel a provider mandate when renewal is cancelled.
- Receive Cashfree webhooks, verify their signatures and process them idempotently.
- Submit queued full/partial refunds to Cashfree and verify the final refund status.

### Remaining HostAdmin APIs

- Transaction detail by transaction identifier.
- Refund detail by refund identifier.
- Reconciliation issue list with individual stale order/webhook/payment details.
- Reconcile a selected order or payment with the provider.
- Retry an eligible failed payment attempt.
- Webhook-event list and detail views.
- Invoice list, invoice detail and invoice download.
- Billing dashboard totals and status breakdown.
- Transaction, refund and invoice exports.

### Remaining Tenant APIs

- Get/update the Tenant billing profile.
- Return country/currency-specific available plans and effective prices.
- Create checkout with a server-calculated price and tax snapshot.
- Get the current order/payment status.
- Get the current subscription, renewal, pending change and grace state.
- Upgrade immediately with verified proration payment.
- Schedule downgrade for the next billing cycle.
- Cancel renewal at period end.
- List and download Tenant-isolated invoices.

### Required background processing

- Process recurring payments and the configured three retry attempts.
- Apply the configured one-day grace period and suspension transition.
- Reconcile missed/out-of-order webhooks and stale pending orders.
- Generate immutable invoice numbers, tax snapshots and PDF documents.
- Activate or renew Tenant entitlement only after verified payment.
- Suspend entitlement after failed retries and grace expiry.
- Update payment, invoice and order states after a verified refund result.

Current Host refund creation only validates the refundable balance and writes a `Requested` queue record. Current webhook retry only moves an eligible failed event back to `Pending`. Neither operation calls Cashfree until the provider adapter and worker are implemented.

## Tested and deployed status

- Configured database: 15/15 billing tables verified; seven Host billing modules and 16 exact operation mappings/grants verified after an idempotent rerun.
- Local API build: passed with 0 errors.
- Focused automated contracts: 6 passed, 0 failed, 0 skipped.
- Authenticated Host configuration HTTP smoke test: not run because a test bearer token was not provided.
- Cashfree sandbox and production payment flow: not deployed or accepted.
