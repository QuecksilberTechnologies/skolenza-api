# Student API work — continuation handoff

**User instruction — future “incomplete”:** If the user writes only
“incomplete”, read [the eight fee scenarios and their status](STUDENT_FEE_EIGHT_GAPS_STATUS.md)
and any newer recorded work, then report completed, partial, remaining and test/
deployment status. The user intends a status answer at that point; do not start
implementation or database changes from that word alone. Resume implementation
only when the user requests it.

**Multi-installment batch schema WIP, 3 November:** Added
`database-scripts/AddStudentFeePaymentBatch.sql`, rehearsed twice on guarded
scratch only. A rolled-back probe verified duplicate payment and cross-quote
selected-installment rejection. Batch entity/EF mappings are WIP; no batch API yet, no application
DB migration and no payment posting. Next implement atomic idempotent batch
posting through the existing permission pipeline, receipt/balance integration,
and required concurrency/authorization tests. [Evidence](testing/student/fee-payment-batch-schema/2026-11-03.md).

**Arrears policy WIP, 3 November:** Central enum, options response, fee
structure request/response and persistence field are present in source. Local
and Render migrations were applied after separate backups and restore rehearsals;
column and constraint match. Student 180/180 and protected 91/91, 41/41 passed;
batch payment does not consume this field. [Evidence](testing/student/fee-arrears-policy/2026-11-03.md).

**Reversal calculation, 1 November:** User confirmed no gateway credentials yet
and asked to continue. The central helper now calculates partial reversal of a
single payment's allocations in reverse charge order with precision and
over-reversal checks. After a corrected new test expectation, scratch Student
**180/180**, protected **91/91** and **41/41** passed. This is calculation
only: offline reversal API, immutable persistence, adjustment receipt, net
balances, idempotency/concurrency and real refund remain pending. Do not claim
cash was returned. [Evidence](testing/student/fee-reversal-calculation/2026-11-01.md).

**Multi-installment calculation follow-up, 1 November:** The central helper
handles late-fee balances after regular heads for each installment, including
both payment orderings. Scratch Student **179/179**, protected **91/91** and
**41/41** passed. This is not a transactional batch payment API; batch ledger,
receipts, corrections and real Cashfree acceptance remain pending. The user
has no Cashfree credentials yet and directed work to continue without them.
[Evidence](testing/student/fee-head-allocation/2026-11-01.md).

**Cashfree sandbox prerequisite, 1 November:** Read-only counts found zero active Student sandbox gateway configurations in both Local and Render. Do not invent credentials or use production accounts. This blocks real Cashfree checkout/webhook/refund acceptance; it does not prevent implementing and testing independent offline, policy and ledger code. [Evidence](testing/student/fee-head-payment-allocation/2026-11-01.md).

**Latest single-installment head allocation, 1 November:** New-snapshot single-installment offline and confirmed Cashfree posting now persists fee-head allocation rows transactionally. Priority comes from component master DisplayOrder snapshotted at quote creation; fee heads are paid before late fee. Balance/receipt responses expose head details and `headAllocationComplete`; legacy unallocated history is never guessed and reports incomplete. Synthetic mismatch and concurrent payment tests pass. Fresh Local/Render backups were restored into isolated Local DBs, migrations rehearsed and allocation DDL applied to both; table fingerprint `6144f0cbb74eb6daa8845b4b4075fbe3` matched and fee quote/payment counts were zero at check time. Local scratch Student suite **176/176**, protected **91/91** and **41/41** passed. Code is uncommitted/undeployed and old API can produce untracked records. **Remaining:** multi-installment payment batches and configurable allocation policy, per-head inclusion/discount controls, refund/reversal audit and provider integration, real Cashfree sandbox and full client acceptance. See [evidence](testing/student/fee-head-payment-allocation/2026-11-01.md).

**Latest snapshot schema parity, 1 November:** Under the user's Local/Render alignment instruction, the additive installment-head schema was applied to Render after a fresh backup, full restore into an isolated Local database, and two successful migration rehearsals. Local and Render snapshot-table column/constraint/index fingerprint matched (`112f4756588c760208e7e0507f11c25a`); each had zero fee quotes, installments, payments and head snapshots at verification time. **Only snapshot schema parity is established.** New API source remains uncommitted and undeployed; the old deployed API can create quotes without snapshots, so recheck intervening quotes and coordinate the code release. Head-level payment allocation, balances, receipts, multiple-month posting, policies, corrections and provider acceptance remain open. See [evidence](testing/student/fee-head-snapshot/2026-11-01.md). Earlier entries saying Render migration was pending describe the preceding checkpoint.

**Render snapshot rehearsal, 1 November:** A fresh full Render backup was archive-checked and restored to separate Local database `student_fee_head_render_restore_verify_20261101`; the snapshot migration succeeded twice on that restored copy. Read-only Render counts were zero for quotes, installments and payments, and the new table was absent at check time. Render was not modified. Coordinate schema/code release: the currently deployed older API can create quotes without snapshots after an additive schema migration, so recheck any intervening quotes and verify the deployed build. Header-level payment posting, multi-installment allocation, configurable policies, receipts, reversals and real gateway acceptance remain pending. See [snapshot evidence](testing/student/fee-head-snapshot/2026-11-01.md).

**Latest fee head snapshot, 1 November:** New quote creation now persists recurring and one-time fee-head amounts per installment in the same transaction. Composite foreign keys enforce institution and quote matching. Additive migration `database-scripts/AddStudentFeeInstallmentHead.sql` was tested twice on Local scratch, then applied to `SkolenzaDB` after fresh backup and full separate restore; Local had zero fee quotes/payments. Initial migration fails closed if historical quotes exist without snapshots. Student suite **175/175**, protected gates **91/91 and 41/41**, focused fee **21/21** passed. **Render migration not applied; Local is ahead of Render.** No API exposes these heads yet and payment allocation, multi-month posting, receipt breakdown, inclusion/discount policies, refund/reversal and Cashfree acceptance remain unimplemented. No deployment or Angular work. See [scenario](testing/student/fee-head-snapshot/2026-11-01.md).

**Fee head schedule foundation, 1 November:** Added a standalone, deterministic minor-unit schedule calculator reconciling fee-head and installment totals. Fee tests **21/21**, isolated Local Student suite **174/174**, protected gates **91/91 and 41/41** passed. This calculator is **not yet wired to quote persistence, payment APIs or the database**; no schema migration or Render write was made. Continue all eight requested gaps and do not represent the module as complete. [Evidence](testing/student/fee-head-allocation/2026-11-01.md).

**Local connection follow-up, 1 November:** Local `SkolenzaDB` connection succeeded; read-only counts were zero for Student fee quotes, installments and payment transactions. The standalone calculator now rejects inconsistent due dates within an installment and a fully paid preferred installment. Fee tests 16/16, scratch Student suite 169/169 and protected gates 91/91 and 41/41 passed. **The calculator is still not connected to payment APIs or persistence. No Local/Render migration has been made; the eight requested end-to-end gaps remain open.** See [fee allocation evidence](testing/student/fee-head-allocation/2026-11-01.md).

**Latest fee-head allocation work, 1 November:** User requested gaps 1–8 and Local/Render DB alignment, and explicitly approved fixing existing small-amount quote rounding. A standalone calculation helper now supports oldest-due-first and selected-installment-first using input header priorities; it is **not connected to an API or ledger**. Quote rounding now prevents a negative last installment when repeated rounding would exceed the total. Fee tests **16/16**, Student suite on isolated Local scratch **169/169**, protected gates **41/41 and 91/91** passed. Local application DB had zero fee quote and payment rows at read-only check; Render contents were not checked. **No migration or Local/Render schema change was made.** All eight end-to-end gaps remain open except this tested foundation and rounding fix; see [evidence](testing/student/fee-head-allocation/2026-11-01.md). Continue with exact head/installment snapshot and paid-data migration rules, authorized transactional batch payments, immutable allocation and reversal audit, receipt/balance APIs, configurable inclusion/concession/priority, permission pipeline, gateway and provider tests, then verified backups and paired Local/Render migration. Never fabricate allocations for historical payments.

**Latest Local database update, 1 November:** A fresh Local backup was restored into newly created, separate localhost database `student_concurrency_codex_20261029` without any clean/drop operation. After identity, Student tables and fixture checks, the full Student suite passed **165/165, 0 failed, 0 skipped**. A repeat initially failed because a fixture left a synthetic unique domain behind; after making the fixture safely reuse verified `.example.test` domains, two consecutive full runs passed 165/165. The extended synthetic HTTP harness also tested verified-origin CORS and cross-origin login against the real repository and scratch PostgreSQL. The scratch DB and sensitive Local backup were retained; no Render data or Angular file was modified. Earlier entries reporting 13 skipped cases describe the previous run. See [database evidence](testing/student/disposable-database-regression/2026-11-01.md). Real authenticated workflows, DNS/TLS and portal UI, invitation email, Cashfree sandbox, safe shared-key rotation, user-led deployment and Render end-to-end acceptance are still pending.

# Latest Student portal cross-origin and readiness update — 2026-11-01

- User approved an optional `portalHostname` for six portal authentication requests and scoped verified-school CORS. API source, focused tests and [UI request examples](UIdeveloperDoc/STUDENT_ADMISSION_API_HANDOFF.md) are updated; existing Host fallback and staff authorization remain. Angular was not changed.
- Focused portal tests **30 passed, 0 failed, 0 skipped**, including synthetic Kestrel preflight. Protected regression gates **41/41**, **91/91** and **117/117** passed; Release build **0 errors, 22 warnings**. Full Student filter **152 passed, 0 failed, 13 skipped / 165**: the designated disposable Local DB `student_concurrency_codex_20261029` did not exist during this run. Never call the skips passes. [Scenario evidence](testing/student/portal-cross-origin-hostname/2026-11-01.md).
- The user agreed to prepare/restore that separate Local database. Check its identity and restore completeness before running DB-dependent tests; never point destructive or concurrency tests at the Local application DB or Render. Do not retry the previously rejected combined restore-and-cleanup command. Use the existing user-provided Local connection only in process-scoped configuration; never record credentials.
- Remaining client acceptance: the 13 skipped DB tests; authenticated real HTTP flows and effective permission grants; verified real school DNS/TLS and portal UI integration; invitation/reset email delivery; Cashfree sandbox checkout and webhook; shared institution-key safe rotation; deployment by the user and authenticated Render end-to-end acceptance. Local synthetic tests do not prove these. Do not claim client readiness until these are verified.
- Code and docs remain uncommitted; do not reset or overwrite existing work. Local Word handoff is excluded from Git. Word render/visual inspection failed because bundled `soffice.exe` is unavailable; the user previously accepted this limitation. No current deployment was performed.

**31 October key-rotation inventory:** Read-only Local table counts for Staff Bank, Institution email, Student identifiers/gateway, institution cards, device commands and email queue were all zero. This is only a Local snapshot, not an assumption about Render or all encrypted data. A source-based consumer inventory and concrete safety checkpoints are recorded in [scenario](testing/student/portal-email-key-rotation/2026-10-31.md). No actual key, data, schema or implementation was changed; safe multi-instance cutover, legacy-token compatibility and disposable-database tests remain pending.

Local email readiness also has one active default email-configuration row; this checks presence only and does not establish valid credentials, SMTP connectivity or successful delivery. [Local prerequisites](testing/student/local-portal-prerequisites/2026-10-31.md).

**31 October shared principal boundary:** With explicit approval, preserved signed `UserType` through the existing token DTO and request-claim mapping so the existing staff validator rejects explicit StudentPortal/Host principals before staff lookup; legacy type-less staff behavior is unchanged. Before correction, 5/8 new synthetic tests failed; after correction all 9 targeted cases passed, including wrong signature/expiry. Protected Staff/Curriculum 41/41, Bank/profile/role 91/91 and combined auth/refresh-V2/registration 80/80 passed. Real staff/portal login, refresh database coexistence, deployed acceptance and the complete Student database suite remain pending. Do not alter either refresh endpoint. [Scenario](testing/student/portal-principal-boundary/2026-10-31.md).

**31 October document review permissions:** Seven new in-process tests verify controller forwarding and the existing Admissions Update pipeline. A synthetic bearer HTTP host uses the real shared request validator and returns 401/200/403/403/401/200 for anonymous, allowed staff, View-only, denied role, StudentPortal and legacy type-less staff respectively. Staff/module/permission lookups, ID decoder and review repository are test doubles. Focused 7/7 and protected gates 41/41 and 91/91 passed. Student suite without the isolated DB passed 121, skipped 13; do not count the skips as passes. Actual staff login, real role grants and PostgreSQL-backed HTTP remain pending. The separate approved principal-mapping code fix is tracked above. [Scenario](testing/student/document-review-permission/2026-10-31.md).

**31 October Local prerequisites:** User supplied Local PostgreSQL access. The protected Staff/Curriculum plus institution rollback gate passed 41/41 against localhost `SkolenzaDB`. Read-only checks found one active invitation template and one active password-reset template, but zero configured or active verified portal domains and zero active Student tuition gateway configurations (sandbox or production). A disposable restore-and-cleanup command was rejected by automatic policy review without a detailed reason, so the full Student database suite was not rerun in this check. No Render database or Angular work was performed. See [scenario](testing/student/local-portal-prerequisites/2026-10-31.md).

**31 October development storage:** A synthetic file was uploaded with the existing FileStorageService using API User Secrets, downloaded through a signed URL with exact byte comparison and deleted. This proves the development provider lifecycle only, not Student endpoint or Render acceptance. The protected S3 Release build passed (0 errors, 16 warnings), and Bank/contact/profile/role and portal contract checks passed 91/91. Staff/Curriculum's real-DB rollback gate was blocked before execution: the Development connection did not match the guarded Local target. No database writes or Angular work were performed. See [scenario](testing/student/portal-document-storage/2026-10-31.md).

**Student portal documents, continued 31 October:** Fixed an EF document-list translation error, applied existing active/verified portal profile checks, required Guardian ID for Guardian upload, enforced current enrollment on download, corrected UTC timestamp mapping and serialized competing reviews. Upload persistence rechecks access and version under database locks. Disposable PostgreSQL with synthetic authenticated HTTP and fake storage verified forced competing uploads, account deactivation and both Guardian-link revocation race orderings, besides upload/list/download, denial cases, versioning and competing reviews; Student 118/118 and protected gates 41/41 and 91/91 passed. Real storage, staff review HTTP and deployed acceptance remain open. Source changes are uncommitted and not deployed. See [scenario](testing/student/portal-document-runtime/2026-10-30.md).

The Local Development JSON AWSUploads section does not contain populated credentials, bucket or region; other configuration providers have not been ruled out. No real storage call was made. The local-only Word handoff was updated, but page rendering could not run because LibreOffice `soffice.exe` was unavailable on PATH; visual layout remains unverified.

**30 October Render publish smoke:** Remote `main` and local `main` were observed at `5141e98`. Live Render Swagger now lists all five Guardian fee routes, and anonymous admissions GET and checkout POST return 401. This verifies published routes and basic authorization only; running commit identity, authenticated flows, providers and UI acceptance remain unverified. See [scenario](testing/student/render-guardian-fee-publish/2026-10-30.md). Older uncommitted/not-deployed notes below are historical.

**30 October key compatibility:** Four synthetic characterization tests verified that existing portal/gateway AES-GCM envelopes require the original key, Bank CBC is unauthenticated, numeric IDs currently use a global salt, and invalidating one process cache does not invalidate another. Protected checks 104/104 before, 108/108 including the new tests after, and Staff/Curriculum 41/41 before/after. These tests do not implement rotation. No actual keys or schema were changed; key history, token/write coordination, legacy-data migration and provider/deployed acceptance remain open. See [scenario](testing/student/portal-email-key-rotation/2026-10-30.md).

**30 October Guardian fees:** Added portal-policy Guardian admission list, quote, balance, receipts and Cashfree checkout source routes. Checkout now locks and rechecks portal authorization, approved admission and installment ownership in the payment-attempt transaction. Forced PostgreSQL/HTTP races passed for both revoke-first and checkout-first ordering; an attempt accepted before revocation may finish its provider call, while later requests are denied. Full Student tests 114/114 with disposable Local PostgreSQL and synthetic HTTP using a fake provider, protected gates 41/41 and 87/87. Real Cashfree provider/webhook and deployed acceptance remain open. See [scenario](testing/student/guardian-fee-portal/2026-10-30.md). No Angular or target schema change; uncommitted and not deployed.

**Last updated:** 2026-10-30 (Asia/Kolkata)
**Current state:** API-only source changes are uncommitted and not deployed. Local and Render schemas are applied and structurally matched.  
**Resume instruction:** If the user says only **“continue”**, begin with the open acceptance gates at the end of this file. Do not ask the user to repeat approved design decisions.

## Completed and pending snapshot — 2026-10-29

**30 October Local runtime:** User started API and UI. IPv4 loopback Swagger returned 200 and anonymous Student options returned 401; authenticated institution-admin testing remains pending. The running binary revision was not verified. [Evidence](testing/student/local-runtime-smoke/2026-10-30.md).

**30 October Local/Render alignment:** Selected Student-related catalog comparison: 1,198 entries each, no differences. Render was missing Guardian invitation and portal password-reset templates; a fresh full Render backup was archive-checked and restored locally, both seed scripts were dry-run twice, then applied to Render. Each target now has one active template per code with matching content. Full Student tests on a disposable Local restore of the fresh Render backup passed 106/106; protected Staff/Curriculum 41/41 and Bank/profile/role 79/79 passed before and after. Both targets had zero active verified portal domains. Real DNS, email, role grants, authenticated running-product and deployment acceptance remain pending. See [scenario](testing/student/render-template-schema-sync/2026-10-30.md).

**30 October portal HTTP harness:** A temporary Local HTTP host using the real portal controller, handlers, repository, password hasher, JWT signer and bearer policy passed synthetic Guardian login, profile, refresh/replay, logout/revoked refresh, anonymous profile and unverified-domain checks. Full Student suite passed 106/106 with disposable PostgreSQL; protected Staff/Curriculum 41/41 and Bank/profile/role 79/79 passed; Release build 0 errors/22 warnings. This test uses a synthetic verified-domain row and a dedicated host, not production `Program`, real DNS/SMTP, Staff permission grants or Render. See [scenario](testing/student/portal-auth-http/2026-10-30.md).

**30 October login lockout:** Fixed failure counting when the portal account had never previously been locked. A guarded disposable PostgreSQL test verified that five failed login attempts persist a lockout. Student suite 106/106, protected Staff/Curriculum 41/41 and Bank/contact/profile/role 79/79 passed. This does not constitute actual HTTP, verified-domain or deployed acceptance. See [scenario](testing/student/portal-login-lockout/2026-10-30.md).

**30 October concurrent reset:** A forced Local PostgreSQL race previously returned 200/500. The reset repository now maps the observed serialization failure and its EF wrapper to the existing safe 409 Conflict response, without changing unrelated database errors. Student tests passed 102/102 including the forced account-lock race through the actual error middleware. This is not a full running-product HTTP or deployment test. See [scenario](testing/student/password-reset-token-revocation/2026-10-30.md).

**Portal password-reset fix:** A disposable DB regression exposed that a second outstanding reset link could change the password after the first had been used. The working-tree repository now invalidates remaining reset links for the same account and institution in the password-change transaction. The Student suite passed 102/102 with disposable DB, including parallel repository attempts and same-institution cross-account token isolation; protected Staff/Curriculum 41/41 and Bank/contact/profile/role 79/79 passed. Forced lock interleavings, HTTP handling of serialization conflicts, authenticated HTTP and deployment remain pending. See [scenario](testing/student/password-reset-token-revocation/2026-10-29.md).

**Shared key rotation update:** The user approved a Local-only safe design and tests preserving Bank data and login. The lock registry now documents the current Skolenza selectors while retaining the old commands as historical references: Bank 1/1 and contact/profile 52/52 passed. Locality 9/9, institution creation/update 15/15, role 26/26 and Staff/Curriculum including Local rollback 41/41 also passed; Release build succeeded with 0 errors and 16 warnings. Key-rotation implementation and its tests remain pending. The scope includes old JWT claims and multi-instance cache lifetime, card lookup hashes and device commands as well as Bank, SMTP and Student data. Do not change or retire an existing refresh endpoint. No Local or Render keys were changed. See [rotation investigation](testing/student/portal-email-key-rotation/2026-10-29.md).

**Completed / locally tested:** Student, Guardian, admission, document, fee,
portal and options API source is in the working tree. The Local and Render
Student schema fingerprints matched on 28 October. Local invitation and
password-reset templates are active. New portal email queue placeholders are
encrypted; disposable-database and worker checks passed. Latest Student suite:
102 passed in a fresh disposable Local database; protected plus focused security tests: 50 passed, no skips. The 102-case run is not a live DNS, SMTP, Cashfree, authenticated HTTP or rotation acceptance test.

**Pending:** DNS-verified school portal domain and actual SMTP delivery;
authenticated Local HTTP/permission, storage and Cashfree sandbox acceptance;
UI integration by the UI developer; reviewed commit, API deployment and Render
acceptance. The current encryption-key resolver only returns the active key;
queued email may become unreadable if that key is rotated. Earlier plaintext
Student portal queue entries in other environments have not been audited.
Code in the working tree and schema parity do not establish live readiness.

Read-only Local check for the `RRSMS` institution found zero active, granted
role-permission rows for each of `STUDENT_ADMISSIONS`, `STUDENT_FEES` and
`STUDENT_PORTAL_DOMAINS`. Assign and verify required grants using the existing
permission-management pipeline; module-operation catalogue mappings alone do
not give a role access. Do not directly seed grants to bypass that pipeline.

**Key-rotation investigation:** A disposable PostgreSQL test failed with
`UQ_InstitutionKey_InstitutionId`: the existing shared key table permits only
one row per institution, including inactive rows. The attempted inactive-key
fallback was removed. Current Student email encryption still relies on the
active key; rotation is not fixed. The shared key is also used by Bank data,
identifiers and other existing flows. Before changing that reference/schema,
follow the repository's explicit approval and locked-regression gates. See
[rotation investigation](testing/student/portal-email-key-rotation/2026-10-29.md).
Post-revert protected/security gate: 50 passed, 0 failed, 0 skipped; Release
build: 0 errors and 22 warnings. The rejected experiment was 105 passed and
1 failed, and is not a rotation acceptance result.

## Permission behavior correction — 2026-10-29

- `GET /api/Student/admissions/{id}` now receives the dynamically resolved
  `moduleId` and `operationId` as query parameters and forwards the existing
  `PermissionRequestDTO` to the Student permission pipeline. Previously the
  request had no permission DTO, so the behavior rejected even valid users.
- Cashfree reconciliation can write a payment, installment and receipt, so it
  now requires `STUDENT_FEES` **Update** rather than View. Clients resolve the
  Update operation from the existing authenticated menu/permission flow.
- Student tests **84 passed / 0 failed / 0 skipped**; protected gate **41 passed
  / 0 failed / 0 skipped** with Local PostgreSQL rollback probes, both before
  and after the change; Release build **0 errors, 22 warnings**. See the
  [permission behavior scenario](testing/student/permission-behavior/2026-10-29.md).
- Tests exercise the controller forwarding and MediatR behavior using test
  doubles. Local Swagger exposes `id`, `ModuleId` and `OperationId`; both
  admission detail and reconciliation returned HTTP 401 without a token.
  Authenticated HTTP, external Cashfree and deployed acceptance are still open.
  No schema or role grant was changed in this phase.

## Admission concurrency ordering — 2026-10-29

- Admission transitions and edits, admission Guardian link add/update/remove,
  admission document version/review persistence and fee quote creation acquire
  the same institution-scoped admission row lock inside their transactions and
  recheck state before writing.
- A disposable PostgreSQL database restored from the Local backup was used for
  eight competing-write interleavings and one uncontended Guardian/document/
  submission flow: **9/9 passed**. The full Student suite passed **93/93**
  with that scratch database enabled; the protected gate passed **41/41** and
  Release build had **0 errors / 22 warnings**. The scratch DB was then dropped
  and its absence verified. See the [concurrency scenario](testing/student/admission-concurrency/2026-10-29.md).
- The tested first writer in the contention cases is synthetic. Full approval
  readiness, enrollment/invitation rollback, independent master-data changes,
  authenticated HTTP, storage, email and payment provider flows remain open.
  Without `SKOLENZA_STUDENT_TEST_CONNECTION`, the nine database cases skip;
  never report those skipped cases as passes.

## Current verified phase audit — 2026-10-28

| Phase | Status | Verified result / remaining gate |
| --- | --- | --- |
| 0. Scope, decisions and protected baseline | DONE | API-only; Angular untouched. Protected gate: 41 passed, 0 skipped, 0 failed. |
| 1. Student/Guardian/admission/exit/document/identifier API source | SOURCE VERIFIED | Student-filtered tests: 75 passed, 0 failed, 0 skipped. |
| 2. Fee configuration/quotes/ledger/offline/Cashfree API source | SOURCE VERIFIED | Included in the 75 Student-filtered tests. External Cashfree sandbox remains untested. |
| 3. Portal auth/domain/currency/document APIs | SOURCE VERIFIED | Included in Student source suite. DNS, mail and object-storage provider round trips remain untested. |
| 4. Release solution build | PASS | 0 errors, 22 warnings. Existing dependency advisories/compiler warnings remain. |
| 5. Local and Render recovery backups | RESTORE VERIFIED | Both archives pass `pg_restore --list` and were fully restored with `--exit-on-error` into disposable scratch databases; each restored 216 `skolenza` tables; scratch databases were removed. |
| 6. Local + Render schema and module mappings | PASS | All 38 expected Student tables exist in both databases; columns/constraints/index fingerprint matches (`d11f7c18a9ca97080d89fcd57cedbcbe`). Four modules each have active View/Add/Update/Delete operation mappings on both. No role grants are seeded by design. |
| 7. Local API anonymous smoke | PASS | Swagger JSON HTTP 200, 59 Student/currency routes discovered; protected Student options endpoint returns 401 without a bearer token. The test process used temporary process-only JWT/storage/DNS settings and was stopped. |
| 8. Authenticated Local HTTP/DB workflow | OPEN | No valid staff/portal bearer session was used. CRUD, tenant permission grants, transaction rollback, upload/review, fees/payments and portal login still need authenticated acceptance. |
| 9. Provider/deployed acceptance | OPEN | Email, live DNS TXT, S3, Cashfree sandbox/webhook, API deployment and authenticated Render acceptance remain unverified. User publishes/deploys; no deployment was performed. |
| 10. UI developer handoff | UPDATED | Markdown contract and local DOCX now carry current routes/status. DOCX conversion/visual inspection limitation is recorded separately. |

**Readiness:** Backend source, both database schemas and anonymous API bootstrap are verified. This is **not yet an end-to-end client acceptance**: authenticated workflow checks and provider/deployed acceptance remain open. Local and Render schema structure matches; application data is environment-specific and was not synchronized.

Evidence: [schema parity and local API smoke report](testing/student/schema-parity-and-runtime-smoke/2026-10-28.md), [phasewise source audit](testing/student/phasewise-source-audit/2026-10-28.md), and [Student API handoff](UIdeveloperDoc/STUDENT_ADMISSION_API_HANDOFF.md).

## Non-negotiable scope and repository gates

- Work on the **Skolenza API/backend only**. Do not edit, format, build, test, install dependencies in, or otherwise touch the Angular workspace without fresh explicit user authorization. The UI developer owns Angular.
- Do not deploy without explicit authorization. Student schema migrations are already applied after restore-tested backups; do not rerun destructive data operations. Any further database mutation requires a verified backup and the documented migration sequence. Assign role grants only through the existing permission-management pipeline.
- Before code, database, configuration, or test work, read the complete `LOCKED_FIXES.md`, repository `AGENTS_old.md`, and applicable feature references. Run the applicable locked **pre-change** gates exactly as documented; stop and report if a protected baseline regresses. Rerun gates after implementation. Skipped tests are not passes.
- Follow existing API patterns: MediatR handlers, controller/repository conventions, `#region` structure and endpoint documentation, central constants/enums, `ICommonRequestService` actor/tenant context, and the existing permission pipeline. Do not bypass permission behavior or seed role grants.
- Reuse authoritative catalogues and services; do not hardcode business lists, permission IDs, country rules, fee labels, or values. Keep IDs owned by Student aggregates as .NET `long` / PostgreSQL `bigint`; shared catalogue IDs keep their source type. Use `:long` route constraints.
- For every new endpoint, update `docs/UIdeveloperDoc/STUDENT_ADMISSION_API_HANDOFF.md` with route, inputs, copyable JSON/FormData/query examples, permission discovery, response/errors, enums, persistence, retries/cancellation, and exact tested/deployed state.
- For every tested scenario, create `docs/testing/student/<scenario>/2026-10-28.md` (use the actual test date), follow `docs/testing/SCENARIO_TEMPLATE.md`, and link it from `docs/testing/README.md` and the UI handoff. Record exact environment, command, pass/fail/skip counts and evidence. Never put credentials, tokens, real identity numbers or personal documents in docs.

## Approved design decisions

These are already confirmed by the user and should not be reopened:

1. **Identity and login:** Student registration/records are separate from `StaffMember`. Student/Guardian portal credentials use a separate institution-scoped credential table and separate API surface. Guardian portal is the default; Student login is optional. Staff login and existing refresh-token contracts remain unchanged. One institution-scoped person/account may carry Guardian and optional Student personas, subject to the designed identity/link rules. Admission approval automatically triggers the Guardian invitation. Do not infer additional account linking.
   - Login tenant selection is email/password on an institution portal domain. The user approved a separate verified domain master/API, owned by each Institution Administrator and verified through a DNS TXT challenge. `InstitutionCode` and `CompanyEmailDomain` are not DNS proof. Domain-management API source and additive SQL are now present; portal login tenant resolution must consume only active, verified mappings and remains pending.
2. **Student identity:** Generate an institution-unique internal `StudentCode`. Store APAAR, UPN and other authority-issued IDs separately, securely, and with authority/country/status/verification provenance. Never fabricate official identifiers.
3. **Student exit:** “Delete student” means non-destructive exit: close the active enrollment, mark the Student exited/inactive, and append `StudentExitHistory` in one transaction. Preserve student, admissions, guardian, documents, education IDs, fees and academic history. Repeated exit must not duplicate history. Capture previous and destination school/country when known. No physical-delete route.
4. **Admission statuses:** `Draft=1`, `Submitted=2`, `UnderReview=3`, `Approved=4`, `Rejected=5`, `ReturnedForCorrection=6`, `Cancelled=7`. Allowed transitions: Draft → Submitted/Cancelled; Submitted → UnderReview/Cancelled; UnderReview → Approved/Rejected/ReturnedForCorrection; ReturnedForCorrection → Submitted/Cancelled. Rejection, return and cancellation require a reason. Preserve append-only status history.
5. **Guardian relationships:** `Father=1`, `Mother=2`, `Guardian=3`, `Grandparent=4`, `Other=5`. Values must be served from central enums through the Student options API. Guardian is the default portal persona; Student persona is optional.
6. **Location/document policy:** School administrators configure document requirements by the applicable institution/location/session context. Use the existing approved object-storage service, not DB blobs or public permanent URLs. Preserve/archive uploads until retention/deletion rules are configured.
7. **Fees:** Fee structure scope is campus + academic session + required Academic Group; no section-specific pricing. Programme derives from the Group. School configures allowed frequencies; parent selects from those allowed. At most one benefit scheme per admission. Scheme min/max bounds are percentages over recurring annual charges only; one-time admission/exam charges are excluded. No scheme means 0% discount. Staff selects the final percentage within scheme bounds. Currency comes from the active primary `CountryCurrencyMaster` row for the Campus country; amounts use its ISO minor-unit precision (supported values 0, 2, or 3), midpoint away from zero. Fee structure and immutable quote snapshot that precision. Late fee is configurable fixed amount per overdue installment after configurable grace days, with optional cap. Support staff-recorded offline and online gateway payments in a Student ledger separate from subscription billing.
11. **Tuition merchant ownership:** The user confirmed school-owned Cashfree merchant accounts. Do not reuse Host-level subscription billing credentials for tuition; design institution-scoped credential configuration and isolate its encrypted secrets. Cashfree sandbox/production environment and secrets must never be returned to callers.
12. **Credential protection:** User approved AES-GCM and confirmed the existing active institution key as the key source. New `IStudentGatewaySecretProtector` uses a versioned AES-GCM envelope, random 96-bit nonce, 128-bit tag, and institution-bound associated data; the existing Staff `IEncryptionService`/bank CBC path is unchanged. Institution-key rotation/replacement requires re-encryption before replacing a key that protects saved gateway values.
13. **Cashfree decimal precision:** User approved rejecting Cashfree checkout for 3-decimal currencies. Preserve the fee amount as-is; do not round it to fit Cashfree. Staff may use the existing offline payment route.
8. **Approval consistency:** Approval now validates one active primary Guardian, active Guardian links, current approved/unexpired required Student and Guardian documents, and a saved quote with its full installment schedule before creating the Student. These checks and Student + first enrollment + Guardian-link copy + admission transition/history run inside the existing database transaction. Source contract tests pass; actual PostgreSQL rollback/atomicity remains unverified until DB schema is applied and authenticated HTTP scenarios are run.
9. **Options and permissions:** UI-facing enum values must come from the central Student options API; configurable lists come from their master APIs. Module and Operation IDs are discovered dynamically from authenticated menu/permission data. SQL maps canonical operations (`View`, `Add`, `Update`, `Delete`) dynamically and must not grant roles.
10. **Teacher boundary:** A Teacher remains a StaffMember with Position/Role and separate academic teaching assignments. Do not create a Teacher-as-Student or duplicate Teacher person table.

## Current implementation and verification

| Area | Source status | Database / runtime status |
|---|---|---|
| Document-requirement master CRUD | Implemented in working tree; `STUDENT_ADMISSIONS`; paged API | Its table/module and canonical View/Add/Update/Delete mappings are applied and verified on Local and Render. Authenticated HTTP CRUD remains open. |
| Fee-frequency and fee-component masters | Implemented in working tree; `STUDENT_FEES`; paged APIs | Tables/modules and canonical View/Add/Update/Delete mappings are applied and verified on Local and Render. No role grants or authenticated HTTP CRUD are verified. |
| Benefit-scheme master CRUD | Implemented in working tree; `STUDENT_ADMISSIONS`; paged API | Table and canonical View/Add/Update/Delete mappings are applied and verified on Local and Render. No role grants or authenticated HTTP CRUD are verified. |
| Student options API | Implemented in working tree; currently exposes central workflow enums including admission/document/fee/exit and Guardian relationship choices | Unit contract only; no authenticated HTTP permission/payload check. New workflow enums must be added centrally as later flows are implemented. |
| Admission draft/list/review/approval and Student list/exit-history APIs | Implemented in working tree; authenticated routes, institution scope and `long` IDs | `CreateStudentAdmissionsAndRecords.sql` is applied to Local and Render. Approval readiness now checks active primary Guardian, required approved/unexpired Student/Guardian documents, and immutable quote/installment schedule before Student creation inside the existing transaction. Source-contract only; no authenticated HTTP/DB rollback acceptance. |
| Admission detail and status-history reads | Implemented in working tree: `GET /api/Student/admissions/{id:long}` and paged `GET .../status-history`; both use existing `STUDENT_ADMISSIONS` View pipeline and status history pagination defaults 1/10. | Uses existing registry tables; schemas are applied to both targets. Source tests only; authenticated Local/Render HTTP acceptance pending. See [admission detail/history report](testing/student/admission-detail-status-history/2026-10-28.md). |
| Guardian profiles, admission capture and Student links | Implemented in working tree: paged institution directory, profile create/update, admission and Student link list/create/update/close; normalized email; active-primary rule; closing link retains history and disables relation portal access | Guardian/registry schema is applied to Local and Render. No authenticated HTTP/DB acceptance. |
| Country/authority education identifier type master | Implemented: institution-scoped paged CRUD at `/api/Student/education-identifier-masters`, country validation, shared permission pipeline | Uses existing registry table; registry SQL is applied to Local and Render. No authenticated HTTP acceptance. |
| Issued education identifier values and verification | Implemented in source: paged masked GET, encrypted create/update with active institution key, verified/rejected review, reason-required rejection, update resets status, append-only verification/reset history | Registry SQL includes the value and history tables and is applied to Local/Render. Focused contract tests: 5 passed. No authenticated HTTP or deployed acceptance. External authority verification/issuance is not integrated; APAAR/UPN must be supplied by the authority/school. |
| Admission documents: staff-assisted upload, review, signed download | Implemented in working tree: requirement-scoped MIME/size/expiry validation; versioned S3 uploads; approval/rejection/return decisions; retained review history; five-minute signed download URLs; central status options and existing permission pipeline | Document schema is applied to Local and Render; authenticated HTTP, real S3, and deployment acceptance remain pending. Portal self-upload source is implemented but not accepted end to end. Required document approvals are checked before admission approval. |
| Fee structure, quote, installments, offline payment, receipt, balance and Cashfree tuition | Source implemented in working tree: campus/session/group fee structure, currency precision, immutable quote/schedule, offline payment, school-owned credentials, idempotent provider order/session, server-to-server reconciliation, signed raw-body webhook, deduplicated event and atomic payment/receipt/installment posting; Cashfree rejects 3-decimal currency | Fee/currency and gateway schema scripts are applied to Local and Render. Provider + AES-GCM/API contract tests pass. Authenticated HTTP/database, Cashfree sandbox, webhook and deployed acceptance remain pending. No live credential has been persisted. |
| Guardian-default / optional Student portal authentication | WIP source implemented: separate institution-scoped account/persona/one-time-token/refresh-token entities; login/refresh/logout/`/me`; anonymous invitation accept and authenticated claim; generic password-reset request and one-time completion; verified-host resolution; central JWT signer with StudentPortal claims; token-table hashes; lockout; and approval-triggered Guardian invitation/email queue. The email queue contains token-bearing URLs. Focused tests: 5/5. | `CreateStudentPortalAuthentication.sql` is applied to Local and Render; authenticated HTTP/database/email delivery/deployed acceptance remain unverified. Invitation requires verified domain and active global email template; reset returns generic success and silently sends no message when account/persona/template is unavailable. Staff login/refresh remains untouched. |
| Guardian/Student portal self-service documents | Implemented in working tree: portal-auth scoped requirements, versioned multipart upload, paged document list, exact-document signed download, linked Guardian/Student persona checks, current enrollment/location/session scope, existing object-storage service, and central requirement/review options. Staff review uses existing `STUDENT_ADMISSIONS` Update pipeline. Focused tests 4/4. | `CreateStudentPortalDocuments.sql` was applied to Local and Render on 2026-10-28 and included in the schema parity check. No authenticated HTTP, object-storage round trip, malware scan or deployment acceptance. See [portal document scenario](testing/student/portal-document-upload/2026-10-28.md). |
| Institution portal domain master and DNS ownership verification | Implemented in working tree: paged tenant-scoped list, create/renew one-time challenge, DNS TXT verification and soft-delete; challenge hash only is persisted | `CreateInstitutionPortalDomainMaster.sql` is applied to Local and Render. Focused source tests pass 6/6. No authenticated HTTP, DNS-provider acceptance, role grant or deployment. This API does not itself implement portal login or tenant resolution. |
| Atomic admission approval integrating Guardian, documents, fees | Implemented in source; transaction readiness validation precedes Student creation | Source contract test only; PostgreSQL transaction/rollback and authenticated HTTP acceptance pending; registry and fee/currency SQL applied to Local and Render. |
| Dynamic role grants, authenticated Local HTTP, Render API acceptance | Pending | Module-operation mappings were applied on both targets; effective role grants still use the existing permission flow. API is not deployed/accepted. |
| Angular | Explicitly out of scope | Untouched. |

Latest Student Cashfree verification (2026-10-28): provider + AES-GCM + gateway contract tests — **10 passed, 0 failed, 0 skipped**. Earlier combined run: **86 passed, 0 failed, 9 skipped / 95 total**. Latest protected gate with DB rollback probes enabled: **41/41 passed**. Test/API build has 0 errors; existing package advisory/compiler warnings remain. Tests cover AES-GCM round-trip/tamper/institution binding, secret-free config contract, sandbox URL and provider session response, 3-decimal rejection before provider I/O, and timestamp-plus-raw-body HMAC verification. No DB, authenticated HTTP, Cashfree sandbox, webhook endpoint, or deployed acceptance ran. See [Cashfree config report](testing/student/cashfree-gateway-configuration/2026-10-28.md) and [Cashfree tuition report](testing/student/cashfree-tuition-payments/2026-10-28.md).

Latest education identifier verification (2026-10-28): focused identifier source-contract suite — **5 passed, 0 failed, 0 skipped**. Earlier combined run: **91 passed, 0 failed, 9 skipped / 100 total**. Latest protected gate with DB rollback probes enabled: **41/41 passed**. Authenticated Student API HTTP and deployment remain open. See [identifier value and verification report](testing/student/education-identifiers/2026-10-28.md).

Latest portal-domain API verification (2026-10-28): focused source-contract tests — **6 passed, 0 failed, 0 skipped**. They cover authenticated route metadata, hostname validation, challenge entropy/hash and split TXT records, mocked DNS-over-HTTPS TXT parsing and unavailable-provider handling, EF table/index mapping, and dynamic permission-pipeline operation selection. A DTO namespace compile error was found and fixed before the passing run. No live DNS, portal-domain DB CRUD, authenticated HTTP or deployment was exercised. See [portal-domain scenario report](testing/student/portal-domain-master/2026-10-28.md).

Latest portal document and admission-read verification (2026-10-28): focused portal document tests **4/4**; admission detail/history contract tests **5/5**; all Student-filtered tests **75 passed, 0 failed, 0 skipped**; protected Staff/Curriculum gate **41 passed, 0 failed, 0 skipped / 41 total**, including Local PostgreSQL rollback probes; current Release solution build **0 errors, 22 warnings**. Build warnings were not individually triaged. No authenticated Student HTTP, S3 round trip, email, DNS or deployed acceptance was run; Local/Render schema parity is verified. See [portal document report](testing/student/portal-document-upload/2026-10-28.md) and [admission detail/history report](testing/student/admission-detail-status-history/2026-10-28.md).

Latest post-auth-change focused source tests: **5 passed, 0 failed, 0 skipped**; broader Student suite: **70 passed, 0 failed, 0 skipped**. Latest protected-only gate: **41 passed, 0 failed, 0 skipped / 41 total**; DB-dependent rollback and database-backed contracts were skipped, so those checks remain unverified. Full Release solution build succeeded with **0 errors, 22 warnings** (existing compiler/package-advisory warnings). Source/regression checks do not apply schemas, prove DNS, verify email delivery, or count as Render acceptance. See [portal-auth scenario report](testing/student/portal-authentication/2026-10-28.md).

Earlier recorded Student source verification before the Cashfree provider change (2026-10-28): `dotnet test .\Skolenza.automationtests\Skolenza.automationtests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Student' /p:UseAppHost=false` — **44 passed, 0 failed, 0 skipped**. An earlier post-change combined gate passed **83**, failed **0**, skipped **9 / 92 total**; these skips were later rerun with DB settings and the protected 41-test gate passed without skips. These are source/unit-contract results, not authenticated HTTP, database application, S3 or deployed acceptance. See the dated reports below.

The current working tree has substantial uncommitted Student API, tests, SQL and docs changes. Do not reset, clean, overwrite or commit unrelated existing changes. No commit or deployment has been verified. The DOCX is intentionally local-only and excluded from Git.

Latest education identifier verification (2026-10-28): focused identifier source-contract suite — **5 passed, 0 failed, 0 skipped**. Earlier combined run: **91 passed, 0 failed, 9 skipped / 100 total**. Latest protected gate with DB rollback probes enabled: **41/41 passed**. Authenticated Student API HTTP and deployment remain open. See [identifier value and verification report](testing/student/education-identifiers/2026-10-28.md).

## Remaining acceptance — proceed without reopening approved design

1. Obtain a real authorized Local staff session through the existing login and permission pipeline. Assign the appropriate View/Add/Update/Delete grants through the existing admin permission flow; the schema only provides dynamic module-operation mappings.
2. Execute authenticated Local API scenarios with isolated/sanitized records: admissions and Guardian links; documents/review and signed downloads; approval atomicity/rollback; Student registry/exit history; official identifier encrypt/mask/review; fee masters/structure/quote/installments/balance/offline receipt; portal account invite/login/refresh/logout/password reset; portal self-service documents; domain challenge/verification; currency lookups. Record each tested scenario per `docs/testing/SCENARIO_TEMPLATE.md`.
3. Validate local object storage, mail delivery, DNS TXT resolution and Cashfree sandbox using approved test credentials and test data. Do not expose credentials or use production charges.
4. Release API code is not deployed. After the user publishes the latest API build, run equivalent authenticated Render acceptance and compare deployed build/schema contract. Database schema parity alone does not mean the currently deployed API has this code.
5. Only after these checks pass, mark client end-to-end acceptance complete. Do not report skipped DB tests as passes; the all 41 protected-gate tests ran, including the rollback probes.

## Latest exact verification

- Protected regression gate: **41 passed, 0 failed, 0 skipped / 41 total**.
- Student API tests: **102 passed, 0 failed, 0 skipped** with disposable DB enabled (2026-10-29); without it, database-dependent cases skip. Approval, reset and encrypted queue persistence in scratch are recorded in [portal email protection evidence](testing/student/portal-email-encryption/2026-10-29.md).
- Readiness (2026-10-30): both Local and Render have the Guardian invitation and password-reset templates, and both have **0 active verified portal domains**. Actual email delivery is unverified.
- New Student invitation/reset queue placeholders are AES-GCM encrypted using the existing institution key and a distinct portal-email purpose. The worker remains compatible with previously queued plaintext messages; Local had none when checked, while other environments were not checked. The active-key resolver has no historical key version, so rotation before delivery remains a risk. See [portal email protection evidence](testing/student/portal-email-encryption/2026-10-29.md).
- Local Guardian invitation and Student password-reset templates are both active and were seeded idempotently. DNS, real SMTP, authenticated HTTP and deployment acceptance remain pending; no Render change was made.
- Final focused portal email/gateway encryption and worker suite: **9 passed, 0 failed, 0 skipped**. Final combined protected/security suite: **50 passed, 0 failed, 0 skipped**. Actual SMTP sending was not performed.
- Protected gate after approval fix: **41 passed, 0 failed, 0 skipped**; Release build **0 errors, 22 warnings**; Debug build **0 errors, 583 warnings**. The approval scratch database was dropped and its absence verified.
- Release solution build: **0 errors, 22 warnings**.
- Local API smoke: `GET /swagger/v1/swagger.json` → 200; 59 Student/CountryCurrency routes; unauthenticated `GET /api/Student/options` → 401.
- Database: Local/Render each 38/38 target tables; identical schema fingerprint; 4/4 Student-related dynamic modules have View/Add/Update/Delete mappings. Role grants are intentionally left to the existing permission UI.
- Backups: full restore to disposable scratch DB succeeded on both; no production data was replaced, and scratch DBs were removed.
- Angular: untouched. Deployment: not performed. Authenticated HTTP and external provider acceptance: not performed.
# Latest fee payment progress 2026-10-30

Guarded Local PostgreSQL testing found and corrected missing scalar aliases in offline and Cashfree preparation locks, inconsistent UTC storage in payment/receipt/checkout-expiry timestamp columns, same-key serialization failures, stale tracked installment balances and a webhook event queried before it had been saved. Forced same-key and different-key offline races, simulated failure and duplicate success webhooks, overpayment rejection and receipt totals passed in a disposable DB; full Student suite **106 passed, 0 failed, 0 skipped**. Existing Student payment, attempt and receipt counts were zero in Local and Render when checked. These source changes are uncommitted and not deployed. Full hosted/API/provider verification remains pending; other Student timestamp fields require separate verification. See [fee-payment database scenario](testing/student/fee-payment-database/2026-10-30.md).

Further Local tests reproduced and fixed duplicate concurrent Student exits, Guardian linking after a concurrent exit, a concurrent primary-Guardian database exception, and education identifier verification timestamp writes. The disposable DB verified one exit/history, retained school history, tenant isolation and synthetic identifier encryption/masking/verification reset; Student **106/106**, protected gates **41/41** and **79/79**. A mixed concrete-master EF tracking collision appeared in a shared test fixture; separate request-like contexts passed, but the broader tracking concern remains an investigation item. [Scenario report](testing/student/exit-guardian-identifier/2026-10-30.md). Authenticated, provider and deployed acceptance remain pending.
# Fee-head discount checkpoint, 1 November

Source now supports per-recurring-head `isDiscountEligible`, preserving the
previous recurring-default and one-time exclusion. Unit and scratch DB tests
passed (Student **178/178**, protected **91/91** and **41/41**). The additive
script was rehearsed against separately restored Local and Render backups and
applied to both; both affected columns have matching type, nullability and
default. The source remains undeployed. No Angular changes or API
deployment. Multi-installment batches, reversals/refunds, real Cashfree tests and
end-to-end acceptance remain open. See [test evidence](testing/student/fee-head-discount/2026-11-01.md).

