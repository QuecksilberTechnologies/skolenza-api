# Refresh-token 500 and authentication-status regression

Local implementation and verification completed. Deployment/live acceptance is pending.

## Reproduced refresh failure

The actual Tenant RefreshTokenCommandHandler failed against the isolated PostgreSQL
fixture with `InvalidCastException: Column 'ParentModuleId' is null` in
StoreProcedureRepository.GetActiveRoleModuleOperationsAsync. Root/shallow modules
are legitimate, but RoleModuleOperationResponseDTO required a non-null integer.
Host refresh passed with the same supplied proxy-chain IP format.

ParentModuleId/MainModuleId and the corresponding response grouping IDs now accept
null. This preserves the real hierarchy, including root modules, and also protects
the login path using these shared DTOs. No fabricated parent IDs or DB changes.

Both Tenant and Host handler tests now issue and persist rotated tokens successfully.
The tests verify the predecessor is revoked, replacement hashes match, a new access
token is issued, and invalid/reused refresh tokens are rejected. They use generated
fixture tokens, not the token from the user's screenshot.

## HTTP status contract

| Condition | Expected HTTP status |
|---|---:|
| Valid authentication but permission function returns PERMISSION_DENIED (0) | 403 |
| Invalid/stale authentication context (-1/-2) | 401 |
| Missing/non-positive requested ModuleId or OperationId | 400 |

The permission-denied branch already maps to ForbiddenAccessException. A separate
false-401 path was found: the Tenant permission repository sent missing action IDs
to the stored function, whose INVALID_ROLE_CONTEXT response became 401. The
repository now rejects those request fields as validation errors before querying.
This applies to all callers of the established Tenant permission repository.
No permission bypass and no blanket conversion of authentication failures to 403.

The user has not supplied the specific failing 401 API URLs/response bodies yet.
Their individual production failures therefore remain unverified; the above are
the concrete paths inspected and tested.

## Evidence and release

11 focused cases passed across the final runs: two real PostgreSQL refresh cases,
six HTTP status regressions, and three existing employee bank permission cases.
The initial missing-ID test fixture incorrectly passed a null DbContext to a
guarded constructor; that fixture was corrected and all three failed cases rerun
successfully. Compilation passed with existing repository warnings.

- Baseline reproduction: artifacts/refresh-database-regression.log
- Tenant/Host null-hierarchy fix: artifacts/refresh-database-regression-fixed.log
- Refresh/status/employee permission results: artifacts/auth-refresh-final-tests.log
- Corrected missing-ID cases: artifacts/auth-missing-id-tests.log

Tests are in the existing automation project: RefreshTokenDatabaseRegressionTests
and AuthenticationStatusRegressionTests. Database tests require the isolated local
skolenza_bulk_test connection and refuse another database name or remote host.

Republish the current API source to deploy this fix. No schema migration is needed.
Older publish output, including the earlier Host bulk release, predates this fix.
After deployment verify the authorized Tenant refresh request and the reported
permission-denied endpoints. Production root cause/live success is not claimed
solely from the local reproduction.
# Opt-in V2 coexistence decision — 2026-09-13

See [AUTH_REFRESH_TOKEN_V2.md](AUTH_REFRESH_TOKEN_V2.md) before future refresh work. The user requested a **new API on a separate branch**. Existing `/api/Auth/refresh-token` must remain unchanged until the UI developer confirms consistency and the user approves retirement. V2 implementation is not authorization to remove the old endpoint or switch UI consumers.
