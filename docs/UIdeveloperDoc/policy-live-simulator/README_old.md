# Tenant Policy Live Flow Simulator

This standalone HTML sample explains and exercises the complete Tenant Policy API contract without changing the Angular project. It reads live dropdown data after the user supplies a runtime bearer token, builds rules/applicability, shows the exact request and database impact before execution, and stores only sanitized policy drafts locally.

## Run (shareable folder)

Copy or ZIP this complete folder. On Windows, double-click `START-HERE.bat`. It uses built-in Windows PowerShell, starts the local server at `http://localhost:4200`, and opens the browser. Keep its console window open while using the simulator.

No installation, Node package, Python package or build command is required.

Optional self-check: double-click `VERIFY.bat`. It verifies the folder, 37 endpoint contracts, 13 rule families, table mappings and the no-token-persistence rule.

Alternative command:

From the repository root:

```powershell
python -m http.server 4200 --directory docs/UIdeveloperDoc/policy-live-simulator
```

Open `http://localhost:4200`. Port 4200 matches the local UI origin normally allowed by API CORS. Stop another process already using that port before starting the sample.

## Live connection

1. Paste the current bearer token and current `UserEmployeeId`.
2. Click **Connect & load DDL**.
3. The simulator first calls `/api/Navigation/my-menu`, flattens the tree, and resolves current `ModuleId` and `OperationId` by module code and operation name.
4. It then calls the lookup endpoints listed in `policy-constants.js`. Each source card shows API, source table and actual load status.

The bearer token stays in the password input in browser memory. It is excluded from localStorage and exported JSON. Do not commit tokens or personal data.

## Included flow

- Policy Type lookup/create/update/status.
- Definition list/detail/create/update, version clone and lifecycle transitions.
- All 13 seeded rule families with editable JSON-object templates.
- Include/exclude applicability across geography, tenant location, employee type, department, designation, employee, gender, work arrangement, employment status, service days, priority and dates.
- Effective-policy resolution, assignments, exceptions, acknowledgements, documents, approval stages/progress and audit.
- Durable bulk template/preview/confirm/status/list/retry/cancel/report for `types`, `definitions` and `assignments`.
- All 37 controller operations in a searchable catalogue.
- Request review popup with endpoint, payload, permission and database impact; execution requires an explicit second action.
- Local save/load and JSON export/import with Travel and Leave examples.

## Contract notes

- Rule templates are UI examples. The backend currently guarantees JSON-object validation; business consumers determine supported keys.
- `OwnerDepartmentId` is the governing department. Targeting belongs in `PolicyApplicability.DepartmentId`.
- Create writes `Policy`, Version 1 in `PolicyVersion`, `PolicyRule`, `PolicyApplicability`, and `PolicyChangeAudit` atomically.
- Published/Archived versions are immutable. Clone creates the next Draft.
- Resolve is read-only. Manual assignment has precedence over applicability.
- Numeric module/operation IDs are never constants; the authenticated menu supplies them.
- System seed: `PolicyCategory`, `PolicyStatus`, `PolicyRuleType`, `PolicyDocumentType`. Tenant master data: `PolicyType`, `PolicyApprovalStage`. Transaction tables are not seeded.
- Multipart operations remain review-only unless the real UI provides a selected file; the simulator does not invent one.

## Files

- `index.html`: interactive sample.
- `styles.css`: responsive UI.
- `policy-constants.js`: routes, module codes, tables, enums and templates.
- `policy-simulator.js`: live loading, builders, request inspector, execution and local storage/file handling.
