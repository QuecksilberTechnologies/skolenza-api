# Tenant Policy API flow — zero-level Hinglish guide

Ye document UI developer ko ye samjhane ke liye hai ki Tenant Policy ki kaunsi
API kis kaam ke liye hai aur normal business flow mein APIs kis order mein call
hongi. Exact request/response payloads ke liye
[Tenant Policy endpoint catalogue](TENANT_POLICY_ENDPOINT_CATALOG.md) dekhein.
Database mein kis situation par kis table mein data jaata hai, uske diagram aur
field-level explanation ke liye
[Tenant Policy table/data-flow guide](TENANT_POLICY_TABLE_DATA_FLOW_HINGLISH.md)
pehle padhein.

## 1. Sabse pehle har basic cheez ko detail mein samjhein

Policy module samajhne ke liye pehle ye samajhna zaroori hai ki Policy Type,
Policy, Version, Rule aur Applicability alag-alag records hain. Inko ek hi cheez
maan lene se UI mein galat dropdown, galat edit button aur galat assignment flow
banega.

### 1.1 Policy Type kya hai?

Policy Type ek tenant-level master hai. Ye actual employee policy nahi hota; ye
batata hai ki banne wali policy kis broad family/category ki hai. Policy create
request mein `policyTypeId` isi master se aata hai.

Policy Type ke 5 examples:

| # | Policy Type | Iske andar banne wali actual policies ke examples |
| --- | --- | --- |
| 1 | Leave Policy | Maharashtra Annual Leave, Karnataka Special Leave |
| 2 | Attendance Policy | Hybrid Attendance, Remote Attendance |
| 3 | Insurance Policy | Employee Health Insurance, Group Accident Cover |
| 4 | Travel Policy | Domestic Travel, International Travel |
| 5 | Reimbursement Policy | Meal, Internet, Mobile ya Local Conveyance reimbursement |

Example: `Leave Policy` ek Policy Type hai. `Maharashtra Annual Leave 2027` us
type ke andar actual Policy hai. Policy Type inactive karne ka matlab type ko
nayi policy selection se rokna hai; iska matlab existing Published policy ko
archive karna nahi hai.

### 1.2 Policy kya hai?

Policy stable business identity hai. Iska code aur name batata hai business rule
kaunsa hai. Ek Policy ke multiple versions ho sakte hain, lekin `policyId` same
reh sakta hai.

Policy ke 5 examples:

| # | Policy code | Policy name | Business meaning |
| --- | --- | --- | --- |
| 1 | `MH-ANNUAL-LEAVE` | Maharashtra Annual Leave | Maharashtra permanent staff ki leave policy |
| 2 | `HYBRID-ATTENDANCE` | Hybrid Attendance | Web/Mobile/Device attendance rules |
| 3 | `EMP-HEALTH-INS` | Employee Health Insurance | Employee aur dependant health coverage |
| 4 | `KA-WOMEN-LEAVE` | Karnataka Women Special Leave | Karnataka ki eligible women employees |
| 5 | `IN-DOMESTIC-TRAVEL` | India Domestic Travel | Domestic travel eligibility aur reimbursement limit |

Policy ko `POST /api/TenantPolicy` se create karne par Policy identity ke saath
Version 1 Draft bhi ek transaction mein banta hai.

### 1.3 Policy Version kya hai?

Version Policy ke time-based revision ko represent karta hai. Published version
immutable hota hai, taaki payroll, attendance, leave ya audit evidence baad mein
change na ho. Change chahiye to Published version clone karke naya Draft banega.

Version ke 5 examples:

| # | Version example | Meaning |
| --- | --- | --- |
| 1 | Leave v1, Draft | Policy likhi ja rahi hai; edit allowed |
| 2 | Leave v1, Under Review | Approvers review kar rahe hain; normal edit allowed nahi |
| 3 | Leave v1, Published | Employees ke liye current effective version |
| 4 | Leave v2, Draft | Published v1 ko clone karke next revision |
| 5 | Leave v1, Archived | Purana/retired version; history ke liye retained |

Example flow:

```text
Policy ID 42
  ├─ Version 1: Published, effective 01-Jan-2027
  └─ Version 2: Draft, proposed effective 01-Jan-2028
```

### 1.4 Rule kya hai?

Rule batata hai Policy employee ko kya benefit, limit, condition ya channel
deti hai. Har rule ka lookup-based `policyRuleTypeId`, readable `ruleName`,
execution/display order aur JSON-object string `ruleConfiguration` hota hai.

Rule ke 5 examples:

| # | Rule Type | Rule name | Configuration ka example |
| --- | --- | --- | --- |
| 1 | Entitlement | Annual leave entitlement | 18 days per year |
| 2 | Carry Forward | Leave carry forward | Maximum 5 days, expiry 3 months |
| 3 | Sandwich | Weekly-off sandwich | Weekly off aur holiday include |
| 4 | Attendance Channel | Hybrid channels | Web, Mobile aur Device allowed |
| 5 | Limit | Insurance/travel limit | INR 500,000 coverage ya INR 5,000 daily limit |

API `ruleConfiguration` ko valid JSON object ke roop mein validate aur store
karti hai. API har custom JSON key ka business calculation automatically nahi
karti. Isliye UI ko approved rule schema se guided fields banana hoga; random key
invent nahi karni.

### 1.5 Applicability kya hai?

Applicability batati hai Published Policy kin logon par apply ya exclude hogi.
Ek row Include (`1`) ya Exclude (`2`) ho sakti hai aur geography, organization,
employee ya employment attributes ko combine kar sakti hai.

Applicability ke 5 examples:

| # | Applicability example | Result |
| --- | --- | --- |
| 1 | Country India + State Maharashtra + Permanent employee | Maharashtra permanent employees include |
| 2 | Tenant Location Mumbai Office | Sirf Mumbai Office employees include |
| 3 | Department Sales + Designation Manager | Sales Managers include |
| 4 | Gender Women + State Karnataka | Karnataka women employees include |
| 5 | Employee ID 201 + Exclude | Employee 201 general match ke baad bhi exclude ho sakta hai |

Manual assignment sabse high precedence rakhta hai. Uske baad employee-specific,
location/locality, district, state, country, employee type, department/designation
aur tenant-default specificity evaluate hoti hai. Same specificity mein lowest
numeric priority jeetti hai; exact tie mein Exclude jeetta hai.

### 1.6 Approval Stage kya hai?

Approval Stage batata hai Policy publish hone se pehle kaunsa role review karega
aur kitni approvals required hain. Stage order sequential hota hai.

Approval Stage ke 5 examples:

| # | Stage | Approver | Minimum approvals | Use |
| --- | --- | --- | --- | --- |
| 1 | HR Review | HR Manager role | 2 | Employee impact check |
| 2 | Legal Review | Legal role | 1 | Statutory/legal wording check |
| 3 | Finance Review | Finance role | 1 | Currency/limit/cost check |
| 4 | IT Security Review | Security role | 1 | Device/mobile/data rule check |
| 5 | Management Approval | Management role | 2 | Final organizational approval |

Stage category-specific ya tenant-wide ho sakta hai. `approverRoleId=null` ka
matlab koi bhi caller nahi; iska matlab woh caller jiske paas endpoint ka valid
Approve permission already hai.

### 1.7 Resolve kya karta hai?

Resolve API given employee aur date ke liye effective Published/current policies
calculate karti hai. Ye preview/read operation hai; assignment create nahi karti.

Resolve ke 5 examples:

| # | Employee/date situation | Possible result |
| --- | --- | --- |
| 1 | Employee 201 manually assigned Leave v1 | `MANUAL_ASSIGNMENT` source |
| 2 | Maharashtra permanent employee | State + Employee Type applicability match |
| 3 | Mumbai office contractor | Location/Employee Type specific policy match |
| 4 | Karnataka woman employee | Gender + State policy match |
| 5 | Archived ya future-date version | Result mein nahi aayega |

Resolve result empty hona API failure nahi; iska matlab selected date par koi
effective Published/current match nahi mila.

### 1.8 Assignment kya hai?

Assignment Published Policy Version ko employee ke saath explicitly link karti
hai. Manual assignment applicability result se higher precedence rakhti hai.

Assignment ke 5 examples:

| # | Assignment example | Behavior |
| --- | --- | --- |
| 1 | Leave v1 → Employee 201 | Nayi assignment insert |
| 2 | Same version/employee/start date repeat | Duplicate nahi; `existing` count |
| 3 | Removed matching assignment dobara bheji | Existing row reactivate |
| 4 | Draft version assign karne ki koshish | Validation/conflict; allowed nahi |
| 5 | Bulk assignment Employee 202 | Worker row create aur acknowledgement Assigned |

Assignment create hote hi missing acknowledgement row bhi `Assigned` status mein
banti hai. Assignment removal hard delete nahi; row `IsActive=false` hoti hai.

### 1.9 Exception kya hai?

Exception base Policy ke upar employee-specific, date-bounded override request
hai. Create hone ke baad decision required hai.

Exception ke 5 examples:

| # | Exception example | Override idea |
| --- | --- | --- |
| 1 | Temporary medical accommodation | Remote attendance allow |
| 2 | Disability accommodation | Attendance/location condition relax |
| 3 | Project travel period | Temporary travel limit override |
| 4 | New joiner approval | Minimum service restriction override |
| 5 | Special leave approval | Defined date range mein extra entitlement metadata |

Rejected exception delete nahi hota; evidence retained rehta hai, lekin approved
override ki tarah treat nahi karna chahiye.

### 1.10 Acknowledgement kya hai?

Acknowledgement employee ke policy receive/view/accept karne ka evidence hai.
Employee sirf apni active assigned policy acknowledge kar sakta hai.

Acknowledgement ke 5 examples:

| # | State/example | Meaning |
| --- | --- | --- |
| 1 | Assigned | Policy employee ko assign hui |
| 2 | Viewed timestamp available | Employee ne policy dekhi |
| 3 | Acknowledged | Employee ne accept/acknowledge ki |
| 4 | `acceptedFrom: WEB` evidence | Web UI se acknowledgement |
| 5 | Assignment removed before acknowledgement | Acknowledge endpoint forbidden karega |

### 1.11 Audit kya hai?

Audit immutable change evidence hai. Isko edit/delete UI nahi deni.

Audit ke 5 examples:

1. Policy Draft create hua.
2. Draft rules/applicability update hue.
3. Version Submit/Approve/Reject/Publish/Archive hua.
4. Version clone hua.
5. Employee assignments create hue.

Base route:

```text
/api/TenantPolicy
```

> **Legacy API warning:** Purane `/api/PolicyType`, `/api/Insurance`,
> `/api/PolicyTypeInsuranceMap`, `/api/EmployeeLeavePolicy`, `/api/Leave`,
> `/api/LeaveRule`, `/api/PolicyMappingLeaveType`, `/api/Rule` aur
> `/api/AttendancePolicy` controller routes source mein disabled hain. Nayi policy
> UI in routes ko call na kare; sirf `/api/TenantPolicy` contract use kare.

Har request ke saath valid tenant JWT bhejein:

```http
Authorization: Bearer <tenant-access-token>
```

`moduleId` aur `operationId` authenticated menu/permission response se dynamically
resolve karein. Documentation ke numeric IDs sirf examples hain; UI constants
mein hard-code na karein. API token se TenantId, current employee aur role nikalti
hai. UI `TenantId` ya `AddedById` na bheje.

## 2. Short answer: policy create se assignment tak exact order

Normal one-by-one happy path:

```text
Authenticated menu/permissions
  -> GET /lookups
  -> GET /types
  -> POST /types                         (sirf type missing ho to)
  -> GET /approval-stages
  -> POST /approval-stages               (sirf setup missing ho to)
  -> POST /                              (Policy + Version 1 Draft)
  -> GET /{policyId}                     (saved draft verify)
  -> PUT /{policyId}/versions/{versionId} (correction ho to)
  -> POST /documents                     (optional attachment)
  -> POST /versions/{versionId}/transition, action=SUBMIT
  -> GET /versions/{versionId}/approval-progress
  -> POST /versions/{versionId}/transition, action=APPROVE
     (required stages/approvers ke hisaab se repeat)
  -> POST /versions/{versionId}/transition, action=PUBLISH
  -> GET /resolve                        (employee/date par result check)
  -> POST /assignments                   (manual assignment required ho to)
  -> GET /versions/{versionId}/assignments
  -> POST /acknowledgements              (employee action)
  -> GET /versions/{versionId}/acknowledgements
  -> GET /{policyId}/audit
```

Mandatory lifecycle:

```text
Draft -> Under Review -> Approved -> Published -> Archived
                  \-> Rejected -> Under Review
```

Draft ko direct Publish nahi kar sakte. Manual assignment sirf Published version
par hoti hai.

## 3. Kaunsa module kis API group ke liye hai

Har leaf module ka apna `moduleId` use hoga. Ek leaf ka View/operation permission
doosre leaf par reuse nahi hoga.

| API group/action | Menu/permission leaf |
| --- | --- |
| Lookups aur Policy Type CRUD | Policy Types |
| Policy list, detail, create, update, clone, resolve, documents | Policy Definitions |
| `SUBMIT` transition | Policy Definitions |
| Approval stages, progress, `APPROVE`, `REJECT`, `PUBLISH`, `ARCHIVE` | Policy Approvals |
| Assignment create/remove/list | Policy Assignments |
| Exception create/decision/list | Policy Exceptions |
| Acknowledge/list | Policy Acknowledgements |
| Audit list | Policy Audit |

Invalid/expired token par `401`; valid token lekin required permission na hone par
`403`; invalid lifecycle transition par `409` milega.

## 4. Screen open aur master setup APIs

### 4.1 `GET /lookups`

**Kaam:** Policy form ke dropdowns ke liye categories, statuses, rule types aur
document types lana.

**Kab call karein:** Policy Type ya Policy Editor screen load par.

**Next:** `GET /types`.

### 4.2 `GET /types`

**Kaam:** Tenant ke active/inactive Policy Types lana.

**Kab call karein:** Policy Type list aur Policy Editor ke type dropdown mein.

**Next:** Required type mil gaya to policy create karein; nahi mila to
`POST /types`.

### 4.3 `POST /types`

**Kaam:** Leave, Attendance jaise Policy Type master ko create karna.

**Output ka use:** Response ka type `id`, `POST /api/TenantPolicy` ke
`policyTypeId` mein jayega.

### 4.4 `PUT /types/{id}`

**Kaam:** Existing Policy Type ka code/name/description/category/currency update
karna.

### 4.5 `PATCH /types/{id}/status`

**Kaam:** Policy Type ko active ya inactive karna. Ye actual Policy Version ko
publish/archive nahi karta.

## 5. Approval setup APIs

### 5.1 `GET /approval-stages`

**Kaam:** Global ya category-specific approval stages dekhna.

**Kab call karein:** Approval setup screen aur submit confirmation se pehle.

### 5.2 `POST /approval-stages`

**Kaam:** Approval stage banana, jaise HR Review ya Legal Review.

Important fields:

- `stageOrder`: stage kis order mein chalega.
- `approverRoleId`: kaunsa role approve kar sakta hai; `null` ho to endpoint ka
  Approve permission rakhne wala caller approve kar sakta hai.
- `minimumApprovals`: stage complete karne ke liye required approval count.
- `isMandatory`: stage required hai ya nahi.
- `policyCategoryId`: category-specific stage; `null` ka matlab tenant-wide stage.

### 5.3 `PUT /approval-stages/{id}`

**Kaam:** Stage ka name, order, role, minimum approvals ya active status update
karna.

### 5.4 `DELETE /approval-stages/{id}`

**Kaam:** Stage ko disable karna. Historical approval references preserve rehte
hain.

## 6. Policy Draft APIs

### 6.1 `POST /api/TenantPolicy`

**Kaam:** Ek transaction mein Policy identity, Version 1 Draft, rules aur
applicability create karna.

**Is response se save karein:**

- `id` = `policyId`
- `versionId` = current draft version ID
- `versionNumber`
- `status`

**Next:** `GET /{policyId}` se saved data verify karein.

Rules:

- Dates `yyyy-MM-dd` format mein hon.
- `ruleConfiguration` JSON object ki string ho; scalar/array accepted nahi hai.
- `applicabilityMode`: `1=Include`, `2=Exclude`.

### 6.2 `GET /api/TenantPolicy`

**Kaam:** Paged Policy list dikhana.

**Use:** Policy list/dashboard search, type/status filters aur pagination.

### 6.3 `GET /api/TenantPolicy/{id}`

**Kaam:** Ek Policy ki selected/current version, rules aur applicability lana.

**Use:** Editor, detail page, review page aur saved draft verification.

### 6.4 `PUT /{policyId}/versions/{versionId}`

**Kaam:** Editable Draft ki details, rules aur applicability atomically replace
karna.

**Important:** Ye Draft edit API hai. Published version immutable hai.

**Next:** Data ready ho to transition API se `SUBMIT`.

### 6.5 `POST /{policyId}/versions/clone`

**Kaam:** Existing version ko copy karke next version ka naya Draft banana.

**Kab use karein:** Published policy badalni ho. Published version ko direct edit
na karein.

Example flow:

```text
Published Version 1 -> Clone -> Draft Version 2 -> Submit -> Approve -> Publish
```

## 7. Documents APIs

### 7.1 `POST /documents`

**Kaam:** Draft Policy Version par PDF/DOC/DOCX document upload karna.

**Request:** `multipart/form-data`; maximum size 10 MB.

Fields: `moduleId`, `operationId`, `policyVersionId`, `policyDocumentTypeId`,
`documentTitle`, `languageCode`, `isEmployeeVisible`, `file`.

### 7.2 `GET /versions/{versionId}/documents`

**Kaam:** Version ke active documents aur temporary download URLs lana.

### 7.3 `DELETE /documents/{documentId}`

**Kaam:** Document metadata soft-delete aur stored object remove karna.

Published/Archived version documents immutable hain. Current deployed environment
mein successful document storage S3 access-key failure se blocked report hua hai;
UI failure ko saved state na dikhaye.

## 8. Lifecycle transition API

Sab lifecycle actions ek hi route se hote hain:

```http
POST /versions/{versionId}/transition
```

Body ka `action` decide karta hai kaam:

| Action | Kaam | Valid result |
| --- | --- | --- |
| `SUBMIT` | Draft/Rejected version review ke liye bhejna | Under Review |
| `APPROVE` | Current approval stage ki approval record karna | Next stage/Approved |
| `REJECT` | Current review cycle reject karna | Rejected |
| `PUBLISH` | Approved version ko effective/current banana | Published |
| `ARCHIVE` | Published version retire karna | Archived |

`SUBMIT` Policy Definitions module use karta hai. `APPROVE`, `REJECT`, `PUBLISH`
aur `ARCHIVE` Policy Approvals module use karte hain.

### Approval rules

- Mandatory stages order mein complete hote hain.
- Caller ko configured `approverRoleId` hold karna hoga.
- Same employee same stage ko do baar approve nahi kar sakta.
- `minimumApprovals` complete hone ke baad next stage aata hai.
- `REJECT` current cycle end karta hai; next `SUBMIT` fresh approval count start
  karta hai.
- Koi approval stage configured na ho to simple direct approval supported hai.
- Publish previous current version ka `IsCurrent` same transaction mein remove
  karta hai.

### `GET /versions/{versionId}/approval-progress`

**Kaam:** Har mandatory stage ka required count, current approval count aur
completion status dikhana.

**Kab call karein:** Approval inbox/detail open par aur har approve/reject ke baad.

## 9. Applicability aur Resolve

### `GET /resolve`

**Kaam:** Given `employeeId` aur `effectiveDate` par kaunsi Published policies
effective hain, calculate karna.

**Ye kya nahi karti:** Assignment row create nahi karti.

Resolution precedence:

```text
Manual assignment
  -> Employee
  -> Location/locality
  -> District
  -> State
  -> Country
  -> Employee type
  -> Department/designation
  -> Tenant default
```

Same specificity par lowest numeric priority wins; priority tie mein Exclude wins.
Response ka `resolutionSource` batata hai result manual assignment se aaya ya
applicability se.

## 10. Assignment APIs

### 10.1 `POST /assignments`

**Kaam:** Ek Published Policy Version ko one or more employees ko manually assign
karna.

**Kab call karein:** Publish ke baad, jab explicit employee assignment chahiye.

Behavior:

- Sirf Published version accepted hai.
- Same version + employee + start date repeat hone par duplicate nahi banta.
- Response `inserted` aur `existing` count deta hai.
- Removed matching assignment milne par woh reactivate hota hai.
- Missing acknowledgement row same transaction mein `Assigned` status se banti
  hai.

**Next:** `GET /versions/{versionId}/assignments` se verify karein.

### 10.2 `GET /versions/{versionId}/assignments`

**Kaam:** Version ki active aur removed assignment rows dikhana.

### 10.3 `DELETE /assignments/{assignmentId}`

**Kaam:** Assignment deactivate/remove karna. Policy ya history delete nahi hoti.

## 11. Exception APIs

Exception main create/publish/assign flow ka mandatory step nahi hai. Ye temporary
employee-specific override ke liye hai.

### `POST /exceptions`

**Kaam:** Employee ke liye exception request create karna. `overrideConfiguration`
JSON object ki string honi chahiye.

### `POST /exceptions/{exceptionId}/decision`

**Kaam:** Exception approve ya reject karna. Body ka `approve` true/false decision
batata hai.

### `GET /versions/{versionId}/exceptions`

**Kaam:** Version ki exceptions aur unke decision/status dikhana.

## 12. Acknowledgement APIs

### `POST /acknowledgements`

**Kaam:** Logged-in employee apni assigned policy acknowledge karta hai.

Rules:

- Active assignment required hai.
- Employee sirf apni acknowledgement kar sakta hai.
- Optional `evidenceJson` source/device jaise evidence rakh sakta hai.

### `GET /versions/{versionId}/acknowledgements`

**Kaam:** Assigned, viewed aur acknowledged date/time evidence dikhana.

## 13. Audit API

### `GET /{policyId}/audit`

**Kaam:** Newest-first immutable policy change evidence dikhana: action, before
data, after data, actor, date/time, entity aur version.

Policy Audit leaf ka apna View permission use karein.

## 14. Saari 37 APIs ka quick purpose catalogue

| # | Method aur route | Seedha purpose |
| --- | --- | --- |
| 1 | `GET /lookups` | Policy dropdown masters lana |
| 2 | `GET /types` | Policy Types list karna |
| 3 | `POST /types` | Policy Type banana |
| 4 | `PUT /types/{id}` | Policy Type update karna |
| 5 | `PATCH /types/{id}/status` | Policy Type active/inactive karna |
| 6 | `GET /` | Paged Policy list lana |
| 7 | `GET /{id}` | Policy/version/rules/applicability detail lana |
| 8 | `POST /` | Policy + Version 1 Draft create karna |
| 9 | `PUT /{policyId}/versions/{versionId}` | Editable Draft replace/update karna |
| 10 | `POST /{policyId}/versions/clone` | Existing version se next Draft banana |
| 11 | `POST /versions/{versionId}/transition` | Submit/Approve/Reject/Publish/Archive |
| 12 | `GET /resolve` | Employee/date ki effective policies calculate karna |
| 13 | `POST /assignments` | Published version manually assign karna |
| 14 | `DELETE /assignments/{assignmentId}` | Assignment deactivate karna |
| 15 | `GET /versions/{versionId}/assignments` | Version assignments list karna |
| 16 | `POST /exceptions` | Employee exception submit karna |
| 17 | `POST /exceptions/{exceptionId}/decision` | Exception approve/reject karna |
| 18 | `GET /versions/{versionId}/exceptions` | Exceptions aur decisions list karna |
| 19 | `POST /acknowledgements` | Employee policy acknowledge karna |
| 20 | `GET /versions/{versionId}/acknowledgements` | Acknowledgement evidence list karna |
| 21 | `POST /documents` | Policy document upload karna |
| 22 | `GET /versions/{versionId}/documents` | Version documents list karna |
| 23 | `DELETE /documents/{documentId}` | Document delete karna |
| 24 | `GET /{policyId}/audit` | Policy audit evidence lana |
| 25 | `GET /approval-stages` | Approval stages list karna |
| 26 | `POST /approval-stages` | Approval stage banana |
| 27 | `PUT /approval-stages/{id}` | Approval stage update karna |
| 28 | `DELETE /approval-stages/{id}` | Approval stage disable karna |
| 29 | `GET /versions/{versionId}/approval-progress` | Stage-wise approval progress lana |
| 30 | `GET /bulk/{target}/template` | Exact CSV headers/template download |
| 31 | `POST /bulk/{target}/preview` | CSV/XLSX validate karke Draft job banana |
| 32 | `POST /bulk/{target}/confirm` | Valid preview ko worker queue mein bhejna |
| 33 | `GET /bulk/{target}/jobs/{jobId}` | Ek bulk job ka progress poll karna |
| 34 | `GET /bulk/{target}/jobs` | Target ke tenant jobs list karna |
| 35 | `POST /bulk/{target}/retry` | Failed rows ka fresh retry job banana |
| 36 | `POST /bulk/{target}/cancel` | Draft/Queued cancel ya Running cancellation request |
| 37 | `GET /bulk/{target}/jobs/{jobId}/report` | Final row-wise CSV report download |

## 15. Bulk import flow

Allowed `{target}` values:

- `types`: Policy Type imports
- `definitions`: Policy + Version 1 Draft imports
- `assignments`: Published version assignment imports

Exact order:

```text
GET  /bulk/{target}/template
  -> POST /bulk/{target}/preview
  -> UI preview errors/duplicates dikhaye
  -> POST /bulk/{target}/confirm
  -> GET  /bulk/{target}/jobs/{jobId} repeatedly poll
  -> terminal status ke baad
  -> GET  /bulk/{target}/jobs/{jobId}/report
  -> retryable failed rows hon to POST /bulk/{target}/retry
```

Statuses:

```text
Draft, Queued, Running, Completed, CompletedWithErrors,
Failed, CancelRequested, Cancelled
```

Important:

- Preview database rows create hone ka proof nahi hai.
- Confirm sirf job queue karta hai; success tab maanein jab terminal job/report
  row result verify ho.
- Definition import Draft banata hai, auto-publish nahi karta.
- Assignment import sirf Published version accept karta hai.
- Assignment import missing Assigned acknowledgement bhi create karta hai.
- Retry original job/report ko edit nahi karta; fresh job banata hai.
- Running cancel next safe worker boundary par effect leta hai.

## 16. UI screen-to-API mapping

| UI screen | Main APIs |
| --- | --- |
| Policy Types | `GET/POST/PUT /types`, `PATCH /types/{id}/status`, `GET /lookups` |
| Policy List | `GET /` |
| Policy Editor | `GET /lookups`, `GET /types`, `POST /`, `GET /{id}`, `PUT /{policyId}/versions/{versionId}`, document APIs |
| Version History | `GET /{id}`, `POST /{policyId}/versions/clone` |
| Applicability Preview | `GET /resolve` |
| Approval Setup | Approval-stage CRUD APIs |
| Approval Inbox | Transition API + approval-progress API |
| Assignments | `POST /assignments`, assignment list/remove, assignment bulk APIs |
| Exceptions | Exception create/decision/list APIs |
| Acknowledgements | Acknowledge/list APIs |
| Audit | `GET /{policyId}/audit` |
| Bulk dialog | Template, preview, confirm, poll, retry, cancel, report APIs |

## 17. UI ko kaunse IDs sambhalne hain

- `policyTypeId`: Policy Type create/list response se.
- `policyId`: Policy create response ka `id`.
- `versionId`: Policy create/detail/clone response ka version ID.
- `assignmentId`: Assignment list response se remove ke liye.
- `exceptionId`: Exception create/list response se decision ke liye.
- `documentId`: Document upload/list response se delete ke liye.
- `jobId`: Bulk preview response se confirm/poll/retry/cancel/report ke liye.
- `moduleId`/`operationId`: Authenticated menu/permission response se; kabhi
  hard-code nahi karne.

## 18. Dummy UI naksha — UI exactly kaise organize karein

Ye wireframes implementation-ready screen guidance hain, final visual design nahi.
Buttons permission response aur current lifecycle status ke according hi dikhayein.
Disabled action ko enabled dikhakar API error par depend na karein.

### 18.1 Policy dashboard aur list

```text
+--------------------------------------------------------------------------------+
| Policies                                                        [+ New Policy] |
+--------------------------------------------------------------------------------+
| Search [________________] Type [All v] Status [All v] [Apply] [Bulk Import]    |
+--------------------------------------------------------------------------------+
| Code          | Policy name                    | Version | Status       | Action |
| MH-AL-2027    | Maharashtra Annual Leave       | v1      | Published    | View   |
| HYBRID-ATT    | Hybrid Attendance              | v2      | Under Review | Review |
| HEALTH-INS    | Employee Health Insurance      | v1      | Draft        | Edit   |
+--------------------------------------------------------------------------------+
| Page 1 of 4                                                   [Previous] [Next] |
+--------------------------------------------------------------------------------+
```

Screen load:

1. Authenticated menu se Policy Definitions ka `moduleId` aur View
   `operationId` resolve karein.
2. `GET /api/TenantPolicy?pageNumber=1&pageSize=20...` call karein.
3. `Add` permission ho tabhi `New Policy` dikhayein.
4. `Import` permission ho tabhi `Bulk Import` dikhayein.
5. Row action current status se decide ho: Draft/Rejected = Edit;
   Under Review = Review; Approved = Publish; Published/Archived = View.

### 18.2 Policy editor wizard

```text
+--------------------------------------------------------------------------------+
| Create Policy                                        Status: Draft              |
| [1 Basic]--[2 Version]--[3 Rules]--[4 Applicability]--[5 Documents]--[6 Review]|
+--------------------------------------------------------------------------------+
| Policy Type *       [Leave Policy v]   Policy Code * [MH-AL-2027________]      |
| Policy Name *       [Maharashtra Annual Leave____________________________]      |
| Owner Department   [Human Resources v] Currency [INR]                         |
| Summary             [____________________________________________________]      |
| Effective From *    [2027-01-01]          Effective To [__________]            |
| Change Summary      [Initial version____________________________________]      |
+--------------------------------------------------------------------------------+
|                                              [Save Draft] [Save & Continue >]   |
+--------------------------------------------------------------------------------+
```

Rules step:

```text
+--------------------------------------------------------------------------------+
| Rules                                                        [+ Add Rule]       |
| 1. Type [Entitlement v] Name [Annual entitlement] Order [1]                    |
|    Guided fields: Annual days [18] Unit [DAY v]                   [Remove]      |
| 2. Type [Carry Forward v] Name [Carry forward] Order [2]                       |
|    Guided fields: Days [5] Expiry months [3]                      [Remove]      |
| Advanced JSON preview: {"annualEntitlementDays":18,"unit":"DAY"}            |
+--------------------------------------------------------------------------------+
```

Applicability step:

```text
+--------------------------------------------------------------------------------+
| Applicability                                               [+ Add Condition]   |
| Mode [Include v] Country [India v] State [Maharashtra v] Employee Type [Perm v]|
| Location [All v] Department [All v] Designation [All v] Employee [All v]      |
| Priority [500] From [2027-01-01] To [__________]                  [Remove]      |
| Preview employee [Search employee v] Effective date [2027-01-15] [Resolve]     |
| Result: APPLICABILITY — Maharashtra Annual Leave                              |
+--------------------------------------------------------------------------------+
```

UI safeguards:

- `ruleConfiguration` internally JSON-object string hai, lekin normal user ko
  raw JSON force na karein. Rule type ke according guided fields dikhayein aur
  advanced JSON optional rakhein.
- Policy create response milne se pehle Documents step enable na karein, kyunki
  upload ke liye `versionId` required hai.
- Save ke baad response ka `policyId` aur `versionId` route/state mein preserve
  karein; UI-generated fake IDs use na karein.
- Published/Archived detail ko read-only banayein. Edit ke badle `Clone as Draft`
  dikhayein.

### 18.3 Approval inbox

```text
+--------------------------------------------------------------------------------+
| Approval Inbox                                                                  |
+--------------------------------------------------------------------------------+
| Policy: Hybrid Attendance v2                 Status: Under Review               |
| Stage 1 HR Review       2 required / 2 approved        Complete                 |
| Stage 2 Legal Review    2 required / 1 approved        Pending                  |
| Comments [_______________________________________________________________]      |
|                                           [Reject] [Approve current stage]       |
+--------------------------------------------------------------------------------+
```

`GET approval-progress` ke response ko source of truth maanein. UI approval
count apni taraf se increment na kare. `Publish` button sirf `Approved` status
mein aur Publish permission ke saath dikhayein.

### 18.4 Assignment screen

```text
+--------------------------------------------------------------------------------+
| Policy Assignments                                                              |
| Policy version [Maharashtra Annual Leave v1 — Published v]                      |
| Employees [Asha (201), Ravi (202)____________________________________]          |
| From [2027-01-01] To [__________] Mandatory [x]             [Assign]            |
+--------------------------------------------------------------------------------+
| Employee | Source | From       | Mandatory | State   | Action                   |
| Asha     | Manual | 2027-01-01 | Yes       | Active  | [Remove]                 |
| Ravi     | Bulk   | 2027-01-01 | Yes       | Active  | [Remove]                 |
+--------------------------------------------------------------------------------+
```

Published versions hi selectable hon. Assignment response ka `inserted` aur
`existing` dono result toast mein dikhayein, jaise “2 inserted, 1 already
existing”.

### 18.5 Exceptions, acknowledgements aur audit

```text
+-------------------------------- Exceptions ------------------------------------+
| Employee | Reason                 | Window                  | Status | Action     |
| Asha     | Medical accommodation  | 01-Feb to 28-Feb 2027  | Review | Approve/Reject |
+----------------------------- Acknowledgements ---------------------------------+
| Employee | Assigned at | Viewed at | Acknowledged at | Status                  |
+---------------------------------- Audit ----------------------------------------+
| Time | Actor | Entity | Action | [Before] [After]                               |
+--------------------------------------------------------------------------------+
```

Exception decision aur policy lifecycle approval ko mix na karein; dono alag
child modules aur permissions hain. Audit read-only rahega.

### 18.6 Bulk-import dialog

```text
+--------------------------------------------------------------------------------+
| Import Policy Definitions                                                       |
| [1 Download Template] -> [2 Upload] -> [3 Preview] -> [4 Confirm] -> [5 Report]|
| File: policies.xlsx   Sheet: PolicyDefinitions                                  |
| Valid 48 | Existing 1 | Failed 1                                                |
| Row 17: PolicyTypeCode does not match an active type                            |
|                                         [Cancel] [Confirm 48 valid rows]         |
+--------------------------------------------------------------------------------+
| Job: Running  32/48 processed                                  [Request Cancel] |
+--------------------------------------------------------------------------------+
```

Confirm ko “Import completed” na label karein. Confirm ke baad poll karein aur
terminal status/report ke baad hi final result dikhayein.

## 19. Child-module CRUD contract — kya available hai aur kya nahi

Yahan “CRUD” ka matlab API mein actually supported operations hai. Jahan Update
ya Delete endpoint nahi hai, UI apni taraf se endpoint invent na kare.

| Child module | Create | Read | Update/decision | Delete/deactivate | Important UI rule |
| --- | --- | --- | --- | --- | --- |
| Policy Types | `POST /types` | `GET /types` | `PUT /types/{id}` | `PATCH /types/{id}/status` | Hard delete nahi; active/inactive toggle |
| Policy Definitions | `POST /` | `GET /`, `GET /{id}` | `PUT /{policyId}/versions/{versionId}` only Draft/Rejected | Direct delete nahi; Published ko `ARCHIVE` transition | Published ko edit na karein; clone karein |
| Policy Versions | Initial version create ke saath; next version `POST /{policyId}/versions/clone` | `GET /{id}` selected/current detail | Transition endpoint | `ARCHIVE` only Published | Version history delete endpoint nahi |
| Rules | Policy create/update payload ke andar | `GET /{id}` detail ke andar | Draft update complete rule list replace karta hai | Draft update list se remove | Separate Rule CRUD route nahi |
| Applicability | Policy create/update payload ke andar | `GET /{id}` detail ke andar; `GET /resolve` calculation | Draft update complete applicability list replace karta hai | Draft update list se remove | Separate applicability CRUD route nahi |
| Documents | `POST /documents` | `GET /versions/{versionId}/documents` | Update endpoint nahi; delete then re-upload only while editable | `DELETE /documents/{documentId}` | Published/Archived documents immutable |
| Approval Stages | `POST /approval-stages` | `GET /approval-stages` | `PUT /approval-stages/{id}` | `DELETE /approval-stages/{id}` actually disables | Historical stage references preserve |
| Approval Lifecycle | `SUBMIT` starts cycle | `GET approval-progress` | `APPROVE`, `REJECT`, `PUBLISH`, `ARCHIVE` | Hard delete nahi | Status-driven buttons only |
| Assignments | `POST /assignments` | `GET /versions/{versionId}/assignments` | Direct update endpoint nahi; remove and reassign | `DELETE /assignments/{id}` deactivates | Only Published version assign |
| Exceptions | `POST /exceptions` | `GET /versions/{versionId}/exceptions` | Decision endpoint with `approve=true/false` | Delete/deactivate endpoint nahi | Rejected exception active record reh sakta hai but apply nahi maana jaye |
| Acknowledgements | `POST /acknowledgements` | `GET /versions/{versionId}/acknowledgements` | Update/delete endpoint nahi | None | Logged-in employee only, active assignment required |
| Audit | None | `GET /{policyId}/audit` | None | None | Immutable read-only evidence |
| Bulk Jobs | Preview creates Draft job | Get one/list/report | Confirm/retry | Cancel action | Original report retry se change nahi hota |

### 19.1 Policy Type ke poore supported CRUD ka example

Create karna:

```http
POST /api/TenantPolicy/types
```

```json
{
  "moduleId": 101,
  "operationId": 1,
  "policyTypeCode": "LEAVE",
  "policyName": "Leave Policy",
  "description": "Leave entitlement and usage policies",
  "policyCategoryId": 1,
  "defaultCurrencyCode": "INR"
}
```

```json
{
  "isSucceeded": true,
  "message": "Policy type created successfully.",
  "data": {
    "id": 12,
    "code": "LEAVE",
    "name": "Leave Policy",
    "description": "Leave entitlement and usage policies",
    "categoryId": 1,
    "currencyCode": "INR",
    "isActive": true
  },
  "errors": []
}
```

List/read karna:

```http
GET /api/TenantPolicy/types?moduleId=101&operationId=4&isActive=true
```

Update karna:

```http
PUT /api/TenantPolicy/types/12
```

```json
{
  "moduleId": 101,
  "operationId": 2,
  "policyTypeCode": "LEAVE",
  "policyName": "Employee Leave Policy",
  "description": "Updated leave policy master",
  "policyCategoryId": 1,
  "defaultCurrencyCode": "INR",
  "isActive": true
}
```

Inactive/reactivate karna:

```http
PATCH /api/TenantPolicy/types/12/status
```

```json
{
  "moduleId": 101,
  "operationId": 9,
  "isActive": false
}
```

Policy Type inactive karne se woh `isActive=true` dropdown se hat jayega aur
create/update validation us inactive type ko accept nahi karegi. Existing Policy
ya Published Version automatically archive/delete nahi hoti, kyunki status
endpoint sirf `PolicyType.IsActive` badalta hai. Reactivate ke liye same endpoint
par `isActive:true` aur matching Active permission bhejein.

## 20. Five complete policy examples

### IDs ke baare mein zaroori baat

JSON field names aur API ke `message` English mein isliye hain kyunki ye exact
backend contract hai. UI label Hinglish/Hindi ho sakta hai, lekin request field
name ya action string translate na karein.

Neeche numeric IDs copy karke production UI mein hard-code na karein. `moduleId`,
`operationId`, `policyTypeId`, rule type IDs, geography IDs, department IDs aur
employee-type IDs current authenticated tenant ke lookups/menu/master APIs se
resolve honge. Pehle teen examples deployed read-back evidence par based hain;
last do contract-valid illustrative UI examples hain aur deployment acceptance
claim nahi hain.

### 20.1 Example 1 — Maharashtra Annual Leave Policy

Kaam: permanent employees ke liye annual entitlement, carry-forward aur
sandwich rule.

Request JSON:

```json
{
  "moduleId": 110,
  "operationId": 1,
  "policyTypeId": 6,
  "policyCode": "MH-ANNUAL-LEAVE-2027",
  "policyName": "India Maharashtra Annual Leave Policy",
  "summary": "Annual leave for permanent employees working in Maharashtra",
  "ownerDepartmentId": 4,
  "defaultCurrencyCode": "INR",
  "effectiveFrom": "2027-01-01",
  "effectiveTo": null,
  "changeSummary": "Initial 2027 version",
  "rules": [
    {
      "policyRuleTypeId": 2,
      "ruleName": "Annual leave entitlement",
      "ruleOrder": 1,
      "ruleConfiguration": "{\"annualEntitlementDays\":18,\"unit\":\"DAY\"}"
    },
    {
      "policyRuleTypeId": 4,
      "ruleName": "Annual leave carry forward",
      "ruleOrder": 2,
      "ruleConfiguration": "{\"carryForwardDays\":5,\"expiryMonths\":3}"
    },
    {
      "policyRuleTypeId": 5,
      "ruleName": "Maharashtra sandwich rule",
      "ruleOrder": 3,
      "ruleConfiguration": "{\"enabled\":true,\"includeWeeklyOff\":true,\"includePublicHoliday\":true}"
    }
  ],
  "applicability": [
    {
      "applicabilityMode": 1,
      "countryId": 1,
      "stateId": 22,
      "employeeTypeId": 7,
      "minimumServiceDays": 0,
      "priority": 500,
      "effectiveFrom": "2027-01-01",
      "effectiveTo": null
    }
  ]
}
```

Udaharan response JSON:

```json
{
  "isSucceeded": true,
  "message": "Policy draft created successfully.",
  "data": {
    "id": 42,
    "code": "MH-ANNUAL-LEAVE-2027",
    "name": "India Maharashtra Annual Leave Policy",
    "summary": "Annual leave for permanent employees working in Maharashtra",
    "policyTypeId": 6,
    "ownerDepartmentId": 4,
    "defaultCurrencyCode": "INR",
    "versionId": 73,
    "versionNumber": 1,
    "statusId": 1,
    "status": "Draft",
    "effectiveFrom": "2027-01-01",
    "effectiveTo": null,
    "changeSummary": "Initial 2027 version",
    "rules": [
      { "id": 81, "ruleTypeId": 2, "name": "Annual leave entitlement", "order": 1, "configuration": "{\"annualEntitlementDays\": 18, \"unit\": \"DAY\"}" },
      { "id": 82, "ruleTypeId": 4, "name": "Annual leave carry forward", "order": 2, "configuration": "{\"carryForwardDays\": 5, \"expiryMonths\": 3}" },
      { "id": 83, "ruleTypeId": 5, "name": "Maharashtra sandwich rule", "order": 3, "configuration": "{\"enabled\": true, \"includeWeeklyOff\": true, \"includePublicHoliday\": true}" }
    ],
    "applicability": [
      { "id": 91, "mode": 1, "priority": 500, "countryId": 1, "stateId": 22, "employeeTypeId": 7, "minimumServiceDays": 0, "effectiveFrom": "2027-01-01", "effectiveTo": null }
    ]
  },
  "errors": []
}
```

### 20.2 Example 2 — Hybrid Attendance Policy

Kaam: employee ko Web, Mobile ya assigned Device se attendance allow karna;
location validation aur office-day count JSON rule mein record karna.

Request:

```json
{
  "moduleId": 110,
  "operationId": 1,
  "policyTypeId": 6,
  "policyCode": "HYBRID-ATTENDANCE-2027",
  "policyName": "Hybrid Web Mobile Device Attendance Policy",
  "summary": "Hybrid attendance through web, mobile or assigned device",
  "ownerDepartmentId": 4,
  "defaultCurrencyCode": "INR",
  "effectiveFrom": "2027-01-01",
  "effectiveTo": null,
  "changeSummary": "Initial hybrid attendance version",
  "rules": [
    {
      "policyRuleTypeId": 7,
      "ruleName": "Hybrid attendance channels",
      "ruleOrder": 1,
      "ruleConfiguration": "{\"officeDaysPerWeek\":3,\"allowWeb\":true,\"allowMobile\":true,\"allowDevice\":true,\"locationValidationRequired\":true}"
    }
  ],
  "applicability": [
    {
      "applicabilityMode": 1,
      "countryId": 1,
      "employeeTypeId": 7,
      "priority": 500,
      "effectiveFrom": "2027-01-01",
      "effectiveTo": null
    }
  ]
}
```

Representative response:

```json
{
  "isSucceeded": true,
  "message": "Policy draft created successfully.",
  "data": {
    "id": 43,
    "code": "HYBRID-ATTENDANCE-2027",
    "name": "Hybrid Web Mobile Device Attendance Policy",
    "policyTypeId": 6,
    "versionId": 74,
    "versionNumber": 1,
    "statusId": 1,
    "status": "Draft",
    "effectiveFrom": "2027-01-01",
    "rules": [
      {
        "id": 84,
        "ruleTypeId": 7,
        "name": "Hybrid attendance channels",
        "order": 1,
        "configuration": "{\"allowWeb\": true, \"allowMobile\": true, \"allowDevice\": true, \"officeDaysPerWeek\": 3, \"locationValidationRequired\": true}"
      }
    ],
    "applicability": [
      { "id": 92, "mode": 1, "priority": 500, "countryId": 1, "employeeTypeId": 7, "effectiveFrom": "2027-01-01", "effectiveTo": null }
    ]
  },
  "errors": []
}
```

### 20.3 Example 3 — Employee Health Insurance Policy

Kaam: employee/dependant eligibility aur coverage limit define karna.

Request:

```json
{
  "moduleId": 110,
  "operationId": 1,
  "policyTypeId": 6,
  "policyCode": "HEALTH-INSURANCE-2027",
  "policyName": "Employee Health Insurance Policy",
  "summary": "Health insurance for permanent employees and eligible dependants",
  "ownerDepartmentId": 4,
  "defaultCurrencyCode": "INR",
  "effectiveFrom": "2027-01-01",
  "effectiveTo": null,
  "changeSummary": "Initial insurance version",
  "rules": [
    {
      "policyRuleTypeId": 1,
      "ruleName": "Health insurance eligibility",
      "ruleOrder": 1,
      "ruleConfiguration": "{\"employeeTypeId\":7,\"minimumServiceDays\":0,\"dependentCoverage\":true}"
    },
    {
      "policyRuleTypeId": 6,
      "ruleName": "Health insurance coverage limit",
      "ruleOrder": 2,
      "ruleConfiguration": "{\"coverageAmount\":500000,\"currency\":\"INR\",\"employeeContributionPercent\":0}"
    }
  ],
  "applicability": [
    {
      "applicabilityMode": 1,
      "countryId": 1,
      "employeeTypeId": 7,
      "minimumServiceDays": 0,
      "priority": 500,
      "effectiveFrom": "2027-01-01",
      "effectiveTo": null
    }
  ]
}
```

Representative response:

```json
{
  "isSucceeded": true,
  "message": "Policy draft created successfully.",
  "data": {
    "id": 44,
    "code": "HEALTH-INSURANCE-2027",
    "name": "Employee Health Insurance Policy",
    "policyTypeId": 6,
    "versionId": 75,
    "versionNumber": 1,
    "statusId": 1,
    "status": "Draft",
    "effectiveFrom": "2027-01-01",
    "rules": [
      { "id": 85, "ruleTypeId": 1, "name": "Health insurance eligibility", "order": 1, "configuration": "{\"employeeTypeId\": 7, \"minimumServiceDays\": 0, \"dependentCoverage\": true}" },
      { "id": 86, "ruleTypeId": 6, "name": "Health insurance coverage limit", "order": 2, "configuration": "{\"coverageAmount\": 500000, \"currency\": \"INR\", \"employeeContributionPercent\": 0}" }
    ],
    "applicability": [
      { "id": 93, "mode": 1, "priority": 500, "countryId": 1, "employeeTypeId": 7, "minimumServiceDays": 0, "effectiveFrom": "2027-01-01", "effectiveTo": null }
    ]
  },
  "errors": []
}
```

### 20.4 Example 4 — Women-specific Regional Leave Policy

Kaam: selected state/location aur gender ke liye special leave. Ye illustrative
example hai; actual gender/state/type IDs master APIs se resolve honge.

Request:

```json
{
  "moduleId": 110,
  "operationId": 1,
  "policyTypeId": 12,
  "policyCode": "KA-WOMEN-SPECIAL-LEAVE-2027",
  "policyName": "Karnataka Women Special Leave Policy",
  "summary": "Special leave for eligible women employees in Karnataka",
  "ownerDepartmentId": 4,
  "defaultCurrencyCode": "INR",
  "effectiveFrom": "2027-01-01",
  "effectiveTo": null,
  "changeSummary": "Initial version",
  "rules": [
    {
      "policyRuleTypeId": 2,
      "ruleName": "Special leave entitlement",
      "ruleOrder": 1,
      "ruleConfiguration": "{\"annualEntitlementDays\":6,\"unit\":\"DAY\"}"
    }
  ],
  "applicability": [
    {
      "applicabilityMode": 1,
      "countryId": 1,
      "stateId": 29,
      "genderId": 2,
      "employeeTypeId": 7,
      "priority": 300,
      "effectiveFrom": "2027-01-01",
      "effectiveTo": null
    }
  ]
}
```

Representative response:

```json
{
  "isSucceeded": true,
  "message": "Policy draft created successfully.",
  "data": {
    "id": 45,
    "code": "KA-WOMEN-SPECIAL-LEAVE-2027",
    "name": "Karnataka Women Special Leave Policy",
    "policyTypeId": 12,
    "versionId": 76,
    "versionNumber": 1,
    "statusId": 1,
    "status": "Draft",
    "effectiveFrom": "2027-01-01",
    "rules": [
      { "id": 87, "ruleTypeId": 2, "name": "Special leave entitlement", "order": 1, "configuration": "{\"annualEntitlementDays\": 6, \"unit\": \"DAY\"}" }
    ],
    "applicability": [
      { "id": 94, "mode": 1, "priority": 300, "countryId": 1, "stateId": 29, "employeeTypeId": 7, "genderId": 2, "effectiveFrom": "2027-01-01", "effectiveTo": null }
    ]
  },
  "errors": []
}
```

### 20.5 Example 5 — Travel and Reimbursement Policy

Kaam: eligible designation ke liye reimbursement limit, currency, receipt
threshold aur travel class store karna. Ye illustrative example hai; `ruleConfiguration`
ki business keys ko UI schema owner se approve karayein, kyunki API JSON object
shape validate karti hai lekin domain-specific key semantics execute nahi karti.

Request:

```json
{
  "moduleId": 110,
  "operationId": 1,
  "policyTypeId": 15,
  "policyCode": "IN-DOMESTIC-TRAVEL-2027",
  "policyName": "India Domestic Travel and Reimbursement Policy",
  "summary": "Domestic travel limits for eligible employees",
  "ownerDepartmentId": 9,
  "defaultCurrencyCode": "INR",
  "effectiveFrom": "2027-04-01",
  "effectiveTo": null,
  "changeSummary": "FY 2027 travel policy",
  "rules": [
    {
      "policyRuleTypeId": 1,
      "ruleName": "Travel eligibility",
      "ruleOrder": 1,
      "ruleConfiguration": "{\"minimumServiceDays\":90}"
    },
    {
      "policyRuleTypeId": 6,
      "ruleName": "Domestic travel reimbursement limit",
      "ruleOrder": 2,
      "ruleConfiguration": "{\"dailyLimit\":5000,\"currency\":\"INR\",\"receiptRequiredAbove\":1000,\"allowedClass\":\"ECONOMY\"}"
    }
  ],
  "applicability": [
    {
      "applicabilityMode": 1,
      "countryId": 1,
      "designationId": 18,
      "minimumServiceDays": 90,
      "priority": 400,
      "effectiveFrom": "2027-04-01",
      "effectiveTo": null
    }
  ]
}
```

Representative response:

```json
{
  "isSucceeded": true,
  "message": "Policy draft created successfully.",
  "data": {
    "id": 46,
    "code": "IN-DOMESTIC-TRAVEL-2027",
    "name": "India Domestic Travel and Reimbursement Policy",
    "policyTypeId": 15,
    "ownerDepartmentId": 9,
    "defaultCurrencyCode": "INR",
    "versionId": 77,
    "versionNumber": 1,
    "statusId": 1,
    "status": "Draft",
    "effectiveFrom": "2027-04-01",
    "rules": [
      { "id": 88, "ruleTypeId": 1, "name": "Travel eligibility", "order": 1, "configuration": "{\"minimumServiceDays\": 90}" },
      { "id": 89, "ruleTypeId": 6, "name": "Domestic travel reimbursement limit", "order": 2, "configuration": "{\"dailyLimit\": 5000, \"currency\": \"INR\", \"receiptRequiredAbove\": 1000, \"allowedClass\": \"ECONOMY\"}" }
    ],
    "applicability": [
      { "id": 95, "mode": 1, "priority": 400, "countryId": 1, "designationId": 18, "minimumServiceDays": 90, "effectiveFrom": "2027-04-01", "effectiveTo": null }
    ]
  },
  "errors": []
}
```

## 21. One policy ka complete lifecycle request/response sequence

Create response se `policyId=42`, `versionId=73` maan kar:

### 21.1 Submit

```http
POST /api/TenantPolicy/versions/73/transition
```

```json
{
  "moduleId": 102,
  "operationId": 20,
  "action": "SUBMIT",
  "comments": "Rules and applicability verified"
}
```

Response detail ka `status` `Under Review` hoga.

### 21.2 Approval progress

```http
GET /api/TenantPolicy/versions/73/approval-progress?moduleId=105&operationId=4
```

```json
{
  "isSucceeded": true,
  "message": "Policy approval progress retrieved successfully.",
  "data": [
    { "stageId": 801, "stageName": "HR Review", "stageOrder": 1, "minimumApprovals": 2, "approvalCount": 1, "isComplete": false }
  ],
  "errors": []
}
```

### 21.3 Approve

```json
{
  "moduleId": 105,
  "operationId": 21,
  "action": "APPROVE",
  "comments": "HR review completed"
}
```

Required approvers/stages ke according call repeat hogi. Final mandatory stage
complete hone par response `status: "Approved"` dega.

### 21.4 Publish

```json
{
  "moduleId": 105,
  "operationId": 28,
  "action": "PUBLISH",
  "comments": "Approved policy released"
}
```

Response `status: "Published"` aur ye version current ho jayega.

### 21.5 Assign

```json
{
  "moduleId": 103,
  "operationId": 11,
  "policyVersionId": 73,
  "employeeIds": [201, 202],
  "effectiveFrom": "2027-01-01",
  "effectiveTo": null,
  "isMandatory": true
}
```

```json
{
  "isSucceeded": true,
  "message": "Policy assignments processed successfully.",
  "data": { "inserted": 2, "existing": 0 },
  "errors": []
}
```

### 21.6 Resolve

```http
GET /api/TenantPolicy/resolve?moduleId=102&operationId=4&employeeId=201&effectiveDate=2027-01-15
```

```json
{
  "isSucceeded": true,
  "message": "Effective policies resolved successfully.",
  "data": [
    {
      "policyId": 42,
      "policyVersionId": 73,
      "policyCode": "MH-ANNUAL-LEAVE-2027",
      "policyName": "India Maharashtra Annual Leave Policy",
      "priority": 500,
      "resolutionSource": "MANUAL_ASSIGNMENT"
    }
  ],
  "errors": []
}
```

## 22. Inactive, Reject, Remove, Disable aur Archive ka exact effect

| User action | API/state change | Kya hoga | Kya nahi hoga | UI behavior |
| --- | --- | --- | --- | --- |
| Policy Type Inactive | `PATCH /types/{id}/status`, `isActive=false` | Active dropdown se type hatega; new create/update mein inactive type reject hoga | Existing policies/versions auto-delete ya auto-archive nahi | Inactive tab mein dikhayein; Reactivate action permission-based |
| Policy Under Review Reject | Transition `REJECT` | Version `Rejected`; current approval cycle end | Policy identity delete nahi; Published old version unaffected | Rejection comments show; owner ko Edit + Resubmit |
| Rejected version edit | Draft update API | Update ke waqt version Draft ban jata hai; rules/scopes replace hote hain | Old approval count reuse nahi | Save ke baad status refresh; Submit action show |
| Rejected version resubmit | Transition `SUBMIT` | Naya Under Review cycle; approvals fresh rejection boundary ke baad count | Previous rejection evidence delete nahi | Approval timeline mein old and new cycle distinguish |
| Assignment Remove/Deactivate | `DELETE /assignments/{id}` | `IsActive=false`, remover/time recorded; manual resolution source band | Policy/version aur history delete nahi | Removed row history mein show; acknowledge action active assignment ke bina block |
| Approval Stage Disable | `DELETE /approval-stages/{id}` | Stage future active-stage selection se hatega | Historical approval foreign keys/evidence delete nahi | Inactive stage filter mein show; destructive “Delete forever” text na use karein |
| Exception Reject | Decision with `approve=false` | Approval status Rejected | Exception record delete/deactivate nahi hota | Override ko effective policy ke roop mein show na karein; reason/history show |
| Document Delete | `DELETE /documents/{id}` | Editable version metadata soft-delete; object removal attempt | Published/Archived document delete allowed nahi | Confirm dialog; API success ke baad list refresh |
| Published Policy Archive | Transition `ARCHIVE` | Version `Archived`, `IsCurrent=false` | Historical assignments/audit/version delete nahi | Read-only Archived badge; new changes ke liye clone strategy/business decision |
| Bulk Cancel Draft/Queued | Cancel endpoint | Immediately Cancelled | Target rows process nahi honge | Terminal Cancelled state show |
| Bulk Cancel Running | Cancel endpoint | CancelRequested; worker next safe boundary par rukta hai | Already committed rows automatically rollback claim na karein | Poll continue; report se actual rows show |

### Reject ke baad exact request

```json
{
  "moduleId": 105,
  "operationId": 22,
  "action": "REJECT",
  "comments": "Applicability mein contractor exclusion missing hai"
}
```

Udaharan error jab UI galat transition bheje:

```json
{
  "isSucceeded": false,
  "message": "Action PUBLISH is invalid for the current policy status.",
  "data": null,
  "errors": [],
  "errorCode": "CONFLICT"
}
```

## 23. UI implementation checklist — wrong UI rokne ke liye

- [ ] Menu response se har leaf ka current `moduleId` aur operation IDs resolve
      kiye; numeric documentation examples hard-code nahi kiye.
- [ ] Legacy `/api/PolicyType`, Leave, Insurance aur AttendancePolicy routes use
      nahi kiye; sirf `/api/TenantPolicy` contract use kiya.
- [ ] Policy Type inactive aur Policy Version archive ko same action nahi maana.
- [ ] Draft/Rejected only editor enabled; Published/Archived read-only.
- [ ] Rules aur applicability update ko full replacement maana; omitted rows
      unintentionally delete na hon isliye complete editor state submit ki.
- [ ] `ruleConfiguration`, `overrideConfiguration`, `evidenceJson` JSON-object
      strings bheje; raw object/array nahi.
- [ ] Create response se real `policyId` aur `versionId` save kiya.
- [ ] Documents upload ko multipart FormData bheja; JSON nahi.
- [ ] S3 upload failure par saved-success UI nahi dikhaya.
- [ ] Approval progress server se refresh kiya; local approval count invent nahi.
- [ ] Assignment screen par sirf Published versions selectable rakhe.
- [ ] Resolve response ko assignment creation na maana.
- [ ] Acknowledge current logged-in employee ke context mein kiya.
- [ ] Delete wording sirf actual behavior ke according: assignment remove,
      approval-stage disable, document soft-delete; policy hard-delete endpoint nahi.
- [ ] Bulk Confirm ko completion na maana; terminal poll + report verify kiya.
- [ ] `401`, `403`, validation error aur `409 Conflict` ko alag UX messages diye.

## 24. Har child module ki dedicated dummy UI

Neeche ke wireframes UI layout samjhane ke liye hain. Ye frontend code nahi hain,
lekin inmein button, status aur API binding saaf dikhayi gayi hai.

### 24.1 Policy Type master

```text
+--------------------------------------------------------------------------------+
| Policy Type Master                                      [ + Add Policy Type ]  |
+--------------------------------------------------------------------------------+
| Search [____________]  Status [Active v]                          [ Search ]    |
+--------------------------------------------------------------------------------+
| Code       Name                    Category   Currency   Status    Actions       |
| LEAVE      Leave Policy            Leave      INR        Active    Edit Deactivate|
| ATTENDANCE Attendance Policy       Attendance -          Active    Edit Deactivate|
+--------------------------------------------------------------------------------+
| Add/Edit drawer                                                               |
| Code* [_______] Name* [________________] Category* [v] Currency [___]          |
| Description [____________________________________________________________]     |
|                                                   [Cancel] [Save]             |
+--------------------------------------------------------------------------------+
```

- Page load: `GET /lookups`, phir `GET /types`.
- Save new: `POST /types`; edit: `PUT /types/{id}`.
- Deactivate/Reactivate: confirmation ke baad `PATCH /types/{id}/status`.
- Inactive Type ko new-policy dropdown mein mat dikhayein. Existing policies ko
  inactive/archived maan lena galat hoga.

### 24.2 Approval Stage setup

```text
+--------------------------------------------------------------------------------+
| Approval Setup                    Category [All v]          [ + Add Stage ]     |
+--------------------------------------------------------------------------------+
| Order | Stage name       | Approver role | Min approvals | Mandatory | Action |
|   1   | HR Review        | HR Manager    |       1       | Yes       | Edit Disable|
|   2   | Finance Review   | Finance Head  |       2       | No        | Edit Disable|
+--------------------------------------------------------------------------------+
| Stage name* [____________] Order* [__] Role [v] Min approvals* [__]            |
| Mandatory [x]                                      [Cancel] [Save]            |
+--------------------------------------------------------------------------------+
```

- Load: `GET /approval-stages`.
- Add/Edit/Disable: corresponding POST/PUT/DELETE route.
- DELETE button ka visible label `Disable` rakhein, kyunki database row hard-delete
  nahi hoti; `isActive=false` hota hai.
- `policyCategoryId=null` ka matlab generic/all-category stage ho sakta hai; UI
  server response ko source of truth rakhe.

### 24.3 Version history, clone aur rejected correction

```text
+--------------------------------------------------------------------------------+
| Maharashtra Annual Leave                         Current published: Version 1  |
+--------------------------------------------------------------------------------+
| Version | Status       | Effective from | Change summary | Action              |
| 1       | Published    | 2027-01-01     | Initial issue  | View  Clone         |
| 2       | Rejected     | 2028-01-01     | 2028 revision  | Edit  Resubmit      |
+--------------------------------------------------------------------------------+
| Rejection comment: Carry-forward limit clarify karein.                         |
| [Open Draft Editor] [Submit again]                                              |
+--------------------------------------------------------------------------------+
```

- Published row directly editable nahi hai. `POST /{policyId}/versions/clone`
  karke new Draft banayein.
- Agar Draft pehle se hai to clone `409` de sakta hai; UI existing Draft khole.
- Rejected version editable hai. `PUT` se correction save karke `SUBMIT` karein.
- Backend mein dedicated “all version history” endpoint nahi hai. Isliye fake
  history API/UI mat banayein; available policy detail/audit aur returned IDs se
  hi screen banayein jab tak backend contract extend na ho.

### 24.4 Documents tab

```text
+--------------------------------------------------------------------------------+
| Documents — Version 2 (Draft)                             [ + Upload ]          |
+--------------------------------------------------------------------------------+
| Title              Type       Language  Employee visible  Size       Action    |
| Leave Handbook     Policy     en         Yes               220 KB     Delete    |
+--------------------------------------------------------------------------------+
| Upload: Title* [____________] Type* [v] Language [__] Visible [x]               |
| File* [ Choose file ]                                [Cancel] [Upload]         |
+--------------------------------------------------------------------------------+
```

- List: `GET /versions/{versionId}/documents`.
- Upload multipart `FormData`; JSON body nahi.
- Document update route nahi hai: editable Draft/Rejected mein delete + re-upload.
- Published/Archived mein Upload/Delete buttons disabled/hidden.
- Current deployed environment mein successful upload S3 credential issue se
  blocked report hua hai; API failure ko success mat dikhayein.

### 24.5 Resolve tester aur conflict display

```text
+--------------------------------------------------------------------------------+
| Policy Resolve Tester                                                          |
| Employee* [Search employee v] Effective date [2027-04-01] [Resolve]            |
+--------------------------------------------------------------------------------+
| Priority | Policy                     Source           Version                 |
| 500      | Maharashtra Annual Leave   APPLICABILITY    73                      |
| 300      | Company Leave Baseline     APPLICABILITY    41                      |
+--------------------------------------------------------------------------------+
| Note: This result calculates applicable Published policies; it assigns nothing.|
+--------------------------------------------------------------------------------+
```

- Call: `GET /resolve?employeeId=...&effectiveDate=...`.
- Empty result ka matlab assignment delete nahi; us date par resolver ko matching
  Published/current/effective policy nahi mili.
- Multiple results ko backend priority/order mein dikhayein; UI apna winner rule
  invent na kare.

### 24.6 Archive aur bulk-job confirmation

```text
+-------------------------------- Archive ---------------------------------------+
| Published Version 1 archive karne ke baad resolver/assignment selection mein   |
| current version ki tarah use nahi hogi. Existing evidence delete nahi hoga.    |
| Reason [_______________________________________________________________]        |
|                                                   [Cancel] [Archive]           |
+--------------------------------------------------------------------------------+

+-------------------------------- Bulk Jobs -------------------------------------+
| Job | Target       | Status      | Total | Success | Failed | Actions           |
| 91  | ASSIGNMENTS  | Processing  | 100   | 60      | 2      | View Cancel       |
| 84  | TYPES        | Failed      | 20    | 18      | 2      | Report Retry      |
+--------------------------------------------------------------------------------+
```

- Archive: transition action `ARCHIVE`, sirf Published se.
- Bulk Confirm ke response ko final completion na maanein. Job terminal state tak
  poll karein, phir report dekhein.
- Original job ka report retry se overwrite nahi hota; retry ko separate action/job
  ke roop mein dikhayein.

## 25. Child modules ka request/response playbook

Policy Type ka complete example section 19.1 mein hai aur Policy create/lifecycle
sections 20–21 mein hain. Neeche baaki har child module ka actual supported CRUD
contract diya hai. `moduleId`/`operationId` samples illustrative hain; authenticated
menu/permission pipeline se current IDs resolve karna mandatory hai.

### 25.1 Approval Stage — Create, Read, Update, Disable

Create:

```json
{
  "moduleId": 103,
  "operationId": 1,
  "policyCategoryId": 1,
  "stageName": "HR Review",
  "stageOrder": 1,
  "approverRoleId": 8,
  "minimumApprovals": 1,
  "isMandatory": true
}
```

```json
{
  "isSucceeded": true,
  "message": "Policy approval stage created successfully.",
  "data": {
    "id": 31,
    "policyCategoryId": 1,
    "stageName": "HR Review",
    "stageOrder": 1,
    "approverRoleId": 8,
    "minimumApprovals": 1,
    "isMandatory": true,
    "isActive": true
  },
  "errors": []
}
```

Read: `GET /approval-stages?policyCategoryId=1&isActive=true`; response `data` mein
upar jaise objects ki array aayegi. Update body same hai, plus `isActive`; path
`PUT /approval-stages/31`. Disable:

```http
DELETE /api/TenantPolicy/approval-stages/31?moduleId=103&operationId=3
```

```json
{
  "isSucceeded": true,
  "message": "Policy approval stage deleted successfully.",
  "data": true,
  "errors": []
}
```

Message mein “deleted” aa sakta hai, lekin persistence behavior disable/soft-delete
hai. UI historical approvals ko erase na kare.

### 25.2 Rules aur Applicability — embedded child CRUD

In dono ke separate POST/PUT/DELETE routes nahi hain. Draft/Rejected version ke
`PUT /{policyId}/versions/{versionId}` mein **poori desired list** bhejni hoti hai.

```json
{
  "moduleId": 102,
  "operationId": 2,
  "policyTypeId": 12,
  "policyCode": "MH-ANNUAL-LEAVE",
  "policyName": "Maharashtra Annual Leave",
  "summary": "Updated draft",
  "ownerDepartmentId": 4,
  "defaultCurrencyCode": "INR",
  "effectiveFrom": "2027-01-01",
  "effectiveTo": null,
  "changeSummary": "Entitlement and scope corrected",
  "rules": [
    {
      "policyRuleTypeId": 2,
      "ruleName": "Annual entitlement",
      "ruleOrder": 1,
      "ruleConfiguration": "{\"days\":18,\"unit\":\"DAY\"}"
    }
  ],
  "applicability": [
    {
      "applicabilityMode": 1,
      "countryId": 1,
      "stateId": 22,
      "priority": 500,
      "effectiveFrom": "2027-01-01",
      "effectiveTo": null
    }
  ]
}
```

Success mein full `PolicyDetailResponseDTO` aata hai. Kisi existing rule/scope ko
array se hata dene par woh desired replacement list ka part nahi rahega. UI sirf
edited row nahi, complete current editor state bheje.

### 25.3 Documents — Upload, Read, Delete

```http
POST /api/TenantPolicy/documents
Content-Type: multipart/form-data
```

```text
moduleId=102
operationId=1
policyVersionId=73
policyDocumentTypeId=1
documentTitle=Leave Handbook
languageCode=en
isEmployeeVisible=true
file=<leave-handbook.pdf>
```

```json
{
  "isSucceeded": true,
  "message": "Policy document uploaded successfully.",
  "data": {
    "id": 501,
    "policyVersionId": 73,
    "documentTypeId": 1,
    "title": "Leave Handbook",
    "originalFileName": "leave-handbook.pdf",
    "contentType": "application/pdf",
    "fileSizeBytes": 225280,
    "languageCode": "en",
    "isEmployeeVisible": true,
    "url": "<server-returned-url>"
  },
  "errors": []
}
```

Read: `GET /versions/73/documents?moduleId=102&operationId=4`. Delete:
`DELETE /documents/501?moduleId=102&operationId=3`; successful `data:true`.
Published/Archived document mutation reject ho sakti hai; list/read allowed rahega.

### 25.4 Assignment — Create, Read, Remove

Create request section 21.5 mein hai. Typical response:

```json
{
  "isSucceeded": true,
  "message": "Policy assigned successfully.",
  "data": { "inserted": 4, "existing": 1 },
  "errors": []
}
```

Iska matlab 5 selected employees mein 4 new assignments bane aur 1 pehle se
existing tha. Read response item:

```json
{
  "id": 901,
  "policyVersionId": 73,
  "employeeId": 10021,
  "assignmentSource": 1,
  "effectiveFrom": "2027-01-01",
  "effectiveTo": null,
  "isMandatory": true,
  "isActive": true
}
```

Remove: `DELETE /assignments/901?moduleId=104&operationId=3`. Success `data:true`;
row deactivate hoti hai. Direct update nahi: remove + correct reassign flow use karein.

### 25.5 Exception — Create, Read, Approve/Reject

```json
{
  "moduleId": 105,
  "operationId": 1,
  "policyVersionId": 73,
  "employeeId": 10021,
  "exceptionType": 1,
  "overrideConfiguration": "{\"days\":3,\"unit\":\"DAY\"}",
  "reason": "Approved special leave adjustment",
  "effectiveFrom": "2027-04-01",
  "effectiveTo": "2027-04-30"
}
```

```json
{
  "isSucceeded": true,
  "message": "Policy exception created successfully.",
  "data": {
    "id": 701,
    "policyVersionId": 73,
    "employeeId": 10021,
    "exceptionType": 1,
    "overrideConfiguration": "{\"days\":3,\"unit\":\"DAY\"}",
    "reason": "Approved special leave adjustment",
    "effectiveFrom": "2027-04-01",
    "effectiveTo": "2027-04-30",
    "approvalStatusId": 2,
    "isActive": true
  },
  "errors": []
}
```

Create ke baad status UnderReview hota hai. Read:
`GET /versions/73/exceptions?...`. Decision:

```json
{
  "moduleId": 105,
  "operationId": 5,
  "approve": false
}
```

`POST /exceptions/701/decision` response updated exception object deta hai.
Reject exception ko policy reject samajhna galat hai; sirf exception request reject
hoti hai. Exception delete/update endpoint available nahi hai.

### 25.6 Acknowledgement — Acknowledge aur Read

```json
{
  "moduleId": 106,
  "operationId": 1,
  "policyVersionId": 73,
  "evidenceJson": "{\"source\":\"EMPLOYEE_PORTAL\",\"accepted\":true}"
}
```

```json
{
  "isSucceeded": true,
  "message": "Policy acknowledged successfully.",
  "data": {
    "id": 801,
    "policyVersionId": 73,
    "employeeId": 10021,
    "status": 3,
    "assignedDateTime": "2027-04-01T09:00:00Z",
    "viewedDateTime": "2027-04-02T10:00:00Z",
    "acknowledgedDateTime": "2027-04-02T10:05:00Z"
  },
  "errors": []
}
```

Active assignment required hai aur employee current authenticated employee context
se aata hai; admin UI arbitrary `employeeId` body mein na bheje. Read admin/audit
screen: `GET /versions/73/acknowledgements?...`. Update/delete endpoint nahi.

### 25.7 Audit — Read-only evidence

```json
{
  "isSucceeded": true,
  "message": "Policy audit retrieved successfully.",
  "data": [
    {
      "id": 1001,
      "policyId": 42,
      "policyVersionId": 73,
      "entityName": "PolicyVersion",
      "entityId": 73,
      "actionName": "PUBLISH",
      "beforeData": "{\"statusId\":3}",
      "afterData": "{\"statusId\":4}",
      "changedById": 9001,
      "changedDateTime": "2027-03-01T11:30:00Z",
      "correlationId": "8a5d1c78-1e68-4b68-a198-57b784516d91"
    }
  ],
  "errors": []
}
```

Route: `GET /42/audit?...`. Audit par create/update/delete UI kabhi na banayein.

### 25.8 Bulk — complete screen sequence

1. `GET /bulk/{target}/template` se current template download.
2. `POST /bulk/{target}/preview` multipart file upload; returned job/row validation dikhayein.
3. User validation dekhe; `POST /bulk/{target}/confirm` kare.
4. `GET /bulk/{target}/jobs/{jobId}` poll kare.
5. Terminal state par `GET /bulk/{target}/jobs/{jobId}/report` download kare.
6. Failed job ho to `POST /retry`; processing ko stop karna ho to `POST /cancel`.
7. History screen ke liye `GET /bulk/{target}/jobs`.

Supported `{target}` values existing contract ke hisaab se Policy Types, Policy
Definitions aur Assignments hain. Target ko free text mat rakhein; documented
server route values/dropdown use karein. Exact multipart fields, status response
aur samples ke liye endpoint catalogue ke API numbers 28–37 source of truth hain.

## 26. Field dictionary — mandatory, optional aur UI control

### 26.1 Policy aur Version fields

| Field | Required/validation | UI control aur meaning |
| --- | --- | --- |
| `policyTypeId` | Required, `> 0` | Active Policy Type dropdown |
| `policyCode` | Required, max 50 | Stable business code; duplicate server error dikhayein |
| `policyName` | Required, max 200 | Display name |
| `summary` | Optional, max 1000 | Multiline summary |
| `ownerDepartmentId` | Optional | Department dropdown |
| `defaultCurrencyCode` | Optional, exactly 3 chars | Currency code such as INR; hard-code list na karein |
| `effectiveFrom` | Required date value | Version/scope start date |
| `effectiveTo` | Optional | Blank means open-ended; start se pehle na bhejein |
| `changeSummary` | Optional, max 1000 | Version mein kya badla |
| `rules` | List | Zero or more; each row validated below |
| `applicability` | List | Zero or more; complete replacement on update |

### 26.2 Rule aur Applicability fields

| Field | Required/validation | Meaning |
| --- | --- | --- |
| `policyRuleTypeId` | Required, `> 0` | `/lookups.ruleTypes` se |
| `ruleName` | Required, max 150 | Human-readable rule label |
| `ruleOrder` | Required, `>= 1` | Evaluation/display order |
| `ruleConfiguration` | Required JSON-object **string** | Schema rule type/domain par depend karta hai |
| `applicabilityMode` | Required range 1–2 | Include/Exclude mapping ko lookup/backend contract se label karein |
| geography/location/employee fields | Optional IDs | Matching dimensions; IDs master APIs se |
| `minimumServiceDays` | Optional, `>= 0` | Service threshold |
| `priority` | Integer, default 100 | Matching precedence input |
| scope dates | `effectiveFrom` + optional `effectiveTo` | Applicability validity |

`workArrangementType` aur `employmentStatus` numeric fields hain, lekin is document
ke evidence mein authoritative enum labels nahi mile. UI label/value invent na kare;
existing constants/lookup contract confirm hone ke baad hi dropdown bind kare.

### 26.3 Other child fields

| Module | Required | Optional/notes |
| --- | --- | --- |
| Assignment | `policyVersionId`, non-empty `employeeIds`, `effectiveFrom` | `effectiveTo`, `isMandatory` default true |
| Exception | version, employee, type, JSON-string override, reason, both dates | Reason max 1000; type labels backend enum se |
| Acknowledgement | `policyVersionId` | `evidenceJson` optional JSON-object string |
| Document | version, document type, title, file | language optional max 10; visible default true |
| Approval Stage | name, order, minimum approvals | category/role optional; mandatory default true |
| Transition | `action` | comments optional max 1000; version ID path se |
| Clone | source version and effective-from | change summary optional |

## 27. Button-to-permission aur status matrix

Numeric permission IDs hard-code nahi karne. Module code ke authenticated menu
operations se current IDs resolve karke request mein bhejne hain.

| UI action | Permission module | Operation intent | Status condition |
| --- | --- | --- | --- |
| Type list/create/edit/status | `TENANT_POLICY_TYPES` | View/Create/Update/Active-Inactive | Type state ke according |
| Policy list/detail/create/edit/clone/docs | `TENANT_POLICY_DEFINITIONS` | View/Create/Update/Delete where route uses it | Edit Draft/Rejected; docs immutable Published/Archived |
| Submit | `TENANT_POLICY_DEFINITIONS` | Submit | Draft or Rejected |
| Approve/Reject/Publish/Archive | `TENANT_POLICY_APPROVALS` | Matching lifecycle operation | Exact legal transition only |
| Stage CRUD/progress | `TENANT_POLICY_APPROVALS` | View/Create/Update/Delete | Setup/progress context |
| Assign/list/remove | `TENANT_POLICY_ASSIGNMENTS` | Create/View/Delete | Assign only Published |
| Exception create/list/decision | `TENANT_POLICY_EXCEPTIONS` | Create/View/Approve/Reject | Exception state driven |
| Acknowledge/list | `TENANT_POLICY_ACKNOWLEDGEMENTS` | Create/View | Active assignment required |
| Audit | `TENANT_POLICY_AUDIT` | View | Read-only |

Button visibility permission se aur button enabled state lifecycle status se decide
hogi. Sirf frontend hide security nahi hai; backend permission pipeline final authority
hai. `403` aane par user ko permission message dikhayein, silent failure nahi.

## 28. Error aur negative scenario UX

| Case | Expected UI behavior |
| --- | --- |
| `401 Unauthorized` | Session refresh/login flow; business validation na dikhayein |
| `403 Forbidden` | “Aapke paas is action ki permission nahi hai”; button mapping recheck |
| `400` validation | Server `errors` ko field/general error mein map karein |
| `404` | Record refresh karein; stale route/ID message |
| `409 Conflict` | Duplicate, illegal transition, existing Draft jaise conflict ko actionable text |
| `415` upload | Multipart/FormData correction; JSON upload retry na karein |
| S3/storage failure | Uploaded/saved badge na lagayein; retry option dikhayein |
| Published edit attempt | Editor read-only; “Clone new version” CTA |
| Assignment to non-Published | selection rok dein; server message preserve karein |
| Acknowledge without active assignment | acknowledgement button disable + assignment status explain |
| Resolve empty | “No effective published policy found”; assignment absent assume na karein |
| Bulk preview row errors | Confirm se pehle row/column error table |
| Bulk processing | polling with backoff; duplicate Confirm clicks disable |
| Bulk partial/failed | totals + downloadable report; success rows ko failed na dikhayein |

Representative error envelope:

```json
{
  "isSucceeded": false,
  "message": "The request could not be completed.",
  "data": null,
  "errors": ["Server-returned validation or business error"]
}
```

Exact message ko frontend decision key na banaye; HTTP status, `isSucceeded`, data
aur documented status fields use karein.

## 29. Operation se persistence par kya effect hota hai

| Operation | Main persistence effect | Kya nahi hota |
| --- | --- | --- |
| Create Type | `PolicyType` row | Policy/version auto-create nahi |
| Type inactive | `PolicyType.IsActive=false` | Existing policies archive/delete nahi |
| Create Policy | `Policy` + Version 1 Draft + rule/scope rows | Publish/assignment nahi |
| Update Draft/Rejected | Version fields; rule/scope desired lists replace | Published version in-place edit nahi |
| Clone | Same policy ke liye next Draft + copied children | Old Published row mutate nahi |
| Submit | Version UnderReview + approval workflow/history setup | Publish nahi |
| Approve | Approval history/count; complete hone par Approved | Assignment nahi |
| Reject | Version Rejected | Policy/type/document hard-delete nahi |
| Publish | Version Published/current; previous current unset | Employee assignment auto-create nahi |
| Archive | Published version Archived/current false | Historical audit/evidence delete nahi |
| Assign | Employee assignment rows; duplicates counted existing | Resolve rule create nahi |
| Remove assignment | Assignment inactive + removal metadata | Hard-delete/history loss nahi |
| Exception create | UnderReview active exception | Override approved/applicable assume nahi |
| Exception decision | Approval status Approved/Rejected | Base policy status change nahi |
| Acknowledge | Current employee acknowledgement status/timestamps | Other employee acknowledge nahi |
| Document delete | Soft-delete while mutable | Storage/audit history necessarily erased nahi |
| Stage delete | Approval stage inactive | Past approval history delete nahi |
| Bulk preview | Draft job + validation rows | Business rows final import nahi |
| Bulk confirm | Worker processing start/queue | Immediate completion guarantee nahi |

Relevant tables/entities repository behavior mein `PolicyType`, `Policy`,
`PolicyVersion`, `PolicyRule`, `PolicyApplicability`, `PolicyDocument`,
`PolicyApprovalStage`, `PolicyApprovalHistory`, `PolicyAssignment`,
`PolicyException`, `PolicyAcknowledgement`, `PolicyChangeAudit`, `BulkImportJob`
aur `BulkImportRow` hain. UI ko database par direct depend nahi karna; ye mapping
sirf operation ka effect samjhane ke liye hai.

## 30. Five examples ka coverage aur evidence boundary

Section 20 mein **5 alag complete policy create datasets** diye gaye hain. Har
dataset mein request aur representative response hai:

1. Maharashtra Annual Leave — deployed evidence based.
2. Hybrid Attendance — deployed evidence based.
3. Employee Health Insurance — deployed evidence based.
4. Women-specific Regional Leave — illustrative business example.
5. Travel and Reimbursement — illustrative business example.

Iska matlab “5 examples total” poora hai. Agar “har Policy Type ke andar 5 database
rows” chahiye, woh separate seed-data requirement hogi aur existing type master ki
final list/domain approval ke bina invent nahi ki ja sakti. `ruleConfiguration` ek
generic JSON-string contract hai; sample keys rule-engine schema ki formal guarantee
nahi hain jab tak domain schema/test evidence explicitly unhe validate na kare.

## 31. Current verification status

- All 37 routes deployed Swagger mein exposed hain.
- Authenticated business flow Policy Type CRUD, Draft create/read/update,
  Submit/Approve/Publish, assignment/resolve, exception, acknowledgement, audit,
  clone aur reject/resubmit tak pass report hua hai.
- Policy Type, Definition aur Assignment bulk imports ke live persistence evidence
  available hain.
- Document success flow configured S3 access-key rejection ki wajah se blocked hai.
- Terminal bulk job cancellation fix local hai; deployment pending report hua hai.
- Successful Retry flow genuine failed worker row milne tak pending hai.

Detailed contracts aur evidence:

- [Tenant Policy versioning, effective dates and permissions](TENANT_POLICY_VERSIONING_PERMISSIONS_HINGLISH.md)
- [Tenant Policy API handoff](TENANT_POLICY_API.md)
- [Tenant Policy endpoint catalogue](TENANT_POLICY_ENDPOINT_CATALOG.md)
- [Tenant Policy UI planning](TENANT_POLICY_UI_PLANNING.md)
- [Deployed business-flow evidence](../testing/policy/live-business-flow/2026-09-16.md)
- [Bulk-import evidence](../testing/policy/bulk-import/2026-09-16.md)
