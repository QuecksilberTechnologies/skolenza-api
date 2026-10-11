# Retired legacy policy APIs

Status: backend source removed on 2026-10-08; Local and Render database artifacts removed. Cleaned API binary deployment is pending.

The old employee-insurance, insurance-policy, policy-type mapping/document,
leave-policy mapping/rule and sandwich-rule endpoints have been retired. Their
DTOs, handlers, repositories and database tables no longer form a supported
contract. The `EMP_INSURANCE` menu and its permissions were also removed.

UI policy work must use the authenticated generic policy APIs documented in
the existing policy handoff files. Resolve current ModuleId and OperationId
through the existing authenticated menu/permission response; never store the
old numeric module ID or the removed `EMP_INSURANCE` code.

The generic flow is:

1. Load policy types and category-scoped rule metadata.
2. Create or clone a draft policy version.
3. Save rules, applicability, assignments and documents on that version.
4. Submit, approve and publish through the generic lifecycle.
5. Read acknowledgements, exceptions and audit data from their generic policy
   endpoints.

The cleanup did not add a replacement request format or endpoint. Existing
generic policy request/response examples remain authoritative. Any Angular
screen still calling `/api/Employee/Insurance`, `/api/Insurance`, legacy
`/api/PolicyType`, leave-policy mapping/rule, or sandwich-rule routes must be
migrated before the cleaned backend is deployed.
