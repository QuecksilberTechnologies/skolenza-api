# <Module>: <scenario> — <YYYY-MM-DD>

## Scope and status

- Scenario ID:
- User-requested behavior / acceptance criteria:
- Overall status: PASS / FAIL / BLOCKED / PENDING
- Environment and API base URL:
- Branch, code commit and independently verified deployed build:
- Run time and timezone (unknown if not captured):
- Prior report / related UI handoff:

## Prerequisites and test data

State the authorized tenant/role, required active master data and permissions.
Resolve ModuleId/OperationId through the existing authenticated menu/permission
flow. Include disposable record identifiers, input formats, required/optional
fields, and sanitized JSON/query/FormData/CSV examples as applicable. Label
examples separately from the actual captured payload. Do not store passwords,
access/refresh tokens, real identity numbers or personal document contents.

## Steps and results

| Case | Action / method / endpoint | Expected result | Actual result / HTTP status | Environment | Status | Evidence |
| --- | --- | --- | --- | --- | --- | --- |
| <ID> | <step> | <assertion> | <observed result> | <local/live> | <status> | <link> |

Include response JSON or a sanitized excerpt when captured. Otherwise state
that the raw response was not retained. Record negative/permission/isolation
checks only if run; list outstanding cases explicitly.

## Investigation and changes

- Observed failure:
- Confirmed cause and evidence:
- Suspected cause / remaining uncertainty:
- Changed files and commit:
- What the executed regression tests establish and do not establish:

## Automated verification

- Test project, exact test names/categories and command:
- Counts: passed / failed / skipped / total:
- Environment prerequisites and skip reasons:
- Sanitized output location or recorded summary:
- Build result, if executed:

## Persistence and cleanup

State records created/updated, relevant tables, whether confirmed through API
read-back or direct DB reconciliation, any file storage/retention involved,
cleanup performed and records intentionally retained. Mark unverified facts.

## Remaining acceptance and completion criteria

List only remaining checks, prerequisites/owner, and the evidence required to
close them. A pushed fix or passing unit test alone is not live acceptance.
