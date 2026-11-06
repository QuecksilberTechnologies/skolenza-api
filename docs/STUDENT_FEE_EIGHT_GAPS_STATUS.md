# Student fee: status of the eight requested scenarios

Updated 2026-11-03. Source changes are uncommitted and undeployed. These
statuses distinguish source and Local tests from deployed client acceptance.

## Future request: “incomplete”

When the user writes only **incomplete**, read this file and
`docs/STUDENT_CONTINUATION.md`, check any newer recorded changes, and report
what is complete, partial, pending, tested and undeployed for these exact eight
fee scenarios. Do not treat that word alone as an instruction to start coding,
run migrations or change data; the user wants the status first. Preserve the
distinction between synthetic gateway tests and real Cashfree acceptance. Do not
invent a completion percentage or claim client readiness without evidence.

The four substantial implementation areas still open are: (1) atomic
multi-installment payment and allocations, (2) applying the stored arrears
policy to those payments, (3) consolidated month/head receipts, and (4)
auditable reversal/refund, corrected balances and concession changes. Then
complete safe application-database migration, authenticated testing and
deployment verification. Actual Cashfree sandbox acceptance additionally
depends on school-owned gateway credentials.

2026-11-03 pre-change verification: guarded Local scratch Student 180/180,
protected security/profile 91/91 and staff/rollback 41/41 passed. This does not
implement or accept the outstanding batch, policy or refund APIs below.

1. **Header paid/pending — PARTIAL.** New quote snapshots and single-installment
   balance expose per-head totals. Multi-installment and reversal-aware net
   balances and deployed acceptance remain open.
2. **School header allocation rule — PARTIAL.** Component-master DisplayOrder
   is snapshotted and used for single-installment posting. A Local database test
   confirms changing the master after quoting does not change that quote's
   allocation order. A complete
   configurable cross-month policy and deployed acceptance remain open.
3. **One payment across months — WIP.** A batch schema script was rehearsed in
   scratch with duplicate/cross-quote constraints. Entity and EF mappings have
   focused tests; there is no atomic payment API, application-database migration or batch
   receipt yet.
4. **Arrears policy — PENDING.** Oldest-first and selected-installment-first
   calculations have tests. Source now contains a fee-structure policy field
   and dynamic options. Matching Local and Render schema is applied after
   independent backup and restore rehearsals;
   the batch payment API does not yet consume the policy. Do not expose this as
   operational until schema, API integration and tests are complete.
5. **Header-wise receipt — PARTIAL.** Single-installment receipts expose their
   head allocation lines; consolidated multi-month receipts remain open.
6. **Include/exclude — PARTIAL.** Structure item presence determines fee
   inclusion; a separate recurring-head discount eligibility flag is in source
   and the additive columns exist on Local/Render. One-time heads are excluded
   from discounts. Deployed authenticated acceptance remains open.
7. **Adjustments and corrections — PENDING.** Reversal calculation is tested,
   but no reversal/refund API, audit ledger, correction receipt, net balance,
   concession change, gateway refund or concurrency acceptance exists.
8. **Real online testing — BLOCKED by missing school Cashfree sandbox account.**
   Fake-provider tests are not real provider acceptance. Checkout, webhook,
   partial-payment and refund behavior must be verified with actual sandbox
   credentials, followed by authenticated deployed acceptance.

Student scratch suite most recently passed 180/180; protected gates 91/91 and
41/41; focused fee tests passed 25/25. These are not evidence of an end-to-end batch or refund. Gateway
credentials must never be pasted into chat or tracked files. Do not claim client
readiness until the relevant pending integration and acceptance checks pass.

Evidence: [single-installment allocation](testing/student/fee-head-payment-allocation/2026-11-01.md),
[discount eligibility](testing/student/fee-head-discount/2026-11-01.md),
[reversal calculation](testing/student/fee-reversal-calculation/2026-11-01.md),
and [continuation](STUDENT_CONTINUATION.md).
