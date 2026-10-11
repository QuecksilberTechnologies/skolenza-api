# Tenant Policy Versioning, Effective Dates, Status aur Permissions — Hinglish Guide

Ye document UI developer, backend developer, tester aur business user ko ek hi
jagah par ye samjhane ke liye hai:

- Policy aur Policy Version mein kya difference hai;
- old aur new Published version ke saath exactly kya hota hai;
- `EffectiveFrom`, `EffectiveTo`, `IsCurrent` aur `IsActive` ka kya role hai;
- Draft, Submit, Approve, Reject, Publish aur Archive ka complete flow;
- HR Draft manage kare aur Admin approval/rejection kare to permission kaise lagegi;
- har operation mein kaunsi API aur kaunsi table use hoti hai;
- current backend behavior mein kaunse important gaps/decisions hain.

Base route:

```text
/api/TenantPolicy
```

Har protected request mein bearer token aur authenticated menu/permission flow se
resolve kiye gaye `moduleId` aur `operationId` required hain. Numeric IDs ko hard-code
na karein; is document ke numbers sirf examples hain.

---

## 1. Sabse simple mental model

```text
PolicyCategory: Leave
  └─ PolicyType: Annual Leave
      └─ Policy: MH Leave Policy
          ├─ Version 1
          │   ├─ Rules
          │   ├─ Applicability
          │   └─ Documents
          └─ Version 2
              ├─ Rules
              ├─ Applicability
              └─ Documents
```

### Policy kya hai?

Policy ek permanent business identity hai:

```text
PolicyId   = 100
PolicyCode = MH-LEAVE
PolicyName = MH Leave Policy
```

### Policy Version kya hai?

Policy Version batati hai ki kisi revision/period mein us Policy ke kaunse rules
aur applicability the:

```text
Version 1 = 18 days leave, 5 days carry-forward
Version 2 = 20 days leave, 8 days carry-forward
```

Ek line mein:

> Policy batati hai “ye kaunsi Policy hai”; Version batati hai “is revision mein
> Policy ke rules kya hain aur woh kab se effective hain.”

---

## 2. MH aur CG Policies ka Draft rule

MH Leave Policy aur CG Leave Policy alag Policies hain, kyunki unke `PolicyId`
alag hain:

```text
MH Leave Policy — PolicyId 100
└─ Version 2 — Draft

CG Leave Policy — PolicyId 200
└─ Version 1 — Draft
```

Dono ka Draft ek saath ho sakta hai.

Restriction per Policy lagti hai:

```text
MH Leave Policy — PolicyId 100
├─ Version 1 — Published
├─ Version 2 — Draft       ✅
└─ Version 3 — Draft       ❌ same Policy ka second Draft allowed nahi
```

Clone ke samay agar isi `PolicyId` ka koi Draft already hai to backend conflict
deta hai:

```text
A draft version already exists.
```

Current check specifically Draft status dekhta hai. Parallel revision business
flow approve na ho to UI ko ek revision complete hone se pehle doosra revision
start nahi karna chahiye.

---

## 3. Pehli baar Policy banane par tables mein kya jayega?

API:

```http
POST /api/TenantPolicy
```

Request example:

```json
{
  "moduleId": 102,
  "operationId": 1,
  "policyTypeId": 12,
  "policyCode": "MH-LEAVE",
  "policyName": "MH Leave Policy",
  "summary": "Maharashtra employees ki annual leave policy",
  "ownerDepartmentId": 4,
  "defaultCurrencyCode": "INR",
  "effectiveFrom": "2027-01-01",
  "effectiveTo": null,
  "changeSummary": "Initial policy version",
  "rules": [
    {
      "policyRuleTypeId": 2,
      "ruleName": "Annual Leave Entitlement",
      "ruleOrder": 1,
      "ruleConfiguration": "{\"days\":18,\"unit\":\"DAY\"}"
    },
    {
      "policyRuleTypeId": 4,
      "ruleName": "Carry Forward",
      "ruleOrder": 2,
      "ruleConfiguration": "{\"maximumDays\":5}"
    }
  ],
  "applicability": [
    {
      "applicabilityMode": 1,
      "countryId": 1,
      "stateId": 22,
      "employeeTypeId": 7,
      "priority": 100,
      "effectiveFrom": "2027-01-01",
      "effectiveTo": null
    }
  ]
}
```

Is ek API call se backend transaction mein ye records banata hai:

```text
Policy                   → main identity row
PolicyVersion            → Version 1, Draft
PolicyRule               → request ke rule rows
PolicyApplicability      → request ke applicability rows
PolicyChangeAudit        → CREATE evidence
```

### 3.1 `Policy` table

| Id | PolicyCode | PolicyName | PolicyTypeId | IsActive |
| ---: | --- | --- | ---: | ---: |
| 100 | MH-LEAVE | MH Leave Policy | 12 | true |

### 3.2 `PolicyVersion` table

| Id | PolicyId | VersionNumber | Status | EffectiveFrom | EffectiveTo | IsCurrent |
| ---: | ---: | ---: | --- | --- | --- | ---: |
| 501 | 100 | 1 | Draft | 2027-01-01 | null | false |

### 3.3 `PolicyRule` table

| Id | PolicyVersionId | RuleName | Configuration |
| ---: | ---: | --- | --- |
| 701 | 501 | Annual Leave Entitlement | `{"days":18}` |
| 702 | 501 | Carry Forward | `{"maximumDays":5}` |

### 3.4 `PolicyApplicability` table

| Id | PolicyVersionId | CountryId | StateId | EmployeeTypeId | Priority |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 801 | 501 | 1 | 22 | 7 | 100 |

Response mein UI ko teen values ka difference samajhna hai:

```json
{
  "id": 100,
  "versionId": 501,
  "versionNumber": 1,
  "status": "Draft"
}
```

| Field | Meaning |
| --- | --- |
| `id` / `policyId` | Permanent Policy identity |
| `versionId` | Specific version ki unique database ID |
| `versionNumber` | User-facing revision number: 1, 2, 3… |

---

## 4. Policy Version kitni ho sakti hain?

Fixed business limit jaise “maximum 5 versions” current code mein nahi hai.
Version number integer hai aur clone ke samay backend calculation karta hai:

```text
New VersionNumber = existing maximum VersionNumber + 1
```

Example:

```text
MH Leave Policy — PolicyId 100
├─ Version 1 — VersionId 501
├─ Version 2 — VersionId 502
├─ Version 3 — VersionId 503
└─ Version 4 — VersionId 504
```

Sabka `PolicyId=100` rahega. Har row ka `VersionId` alag hoga.

Draft ko baar-baar save/edit karne par version number nahi badhta:

```text
Version 1 Draft → edit → Version 1 Draft
```

New version tab banti hai jab Clone API call hoti hai.

---

## 5. Existing Published version se new version kaise banegi?

Maan lijiye:

```text
Version 1 — Published — IsCurrent=true — 18 days
```

2028 ke liye 20 days karne hain. Published Version 1 ko edit nahi karenge.
Clone API call karenge:

```http
POST /api/TenantPolicy/100/versions/clone
```

```json
{
  "moduleId": 102,
  "operationId": 1,
  "sourceVersionId": 501,
  "effectiveFrom": "2028-01-01",
  "changeSummary": "Annual entitlement changed from 18 to 20 days"
}
```

Clone ke baad:

```text
MH Leave Policy — PolicyId 100
├─ Version 1 — VersionId 501 — Published — IsCurrent=true
└─ Version 2 — VersionId 502 — Draft     — IsCurrent=false
```

Backend source version ke Rules aur Applicability ko new rows ke roop mein copy
karta hai. Version 1 ke child rows modify nahi hote.

Ab Version 2 ko Draft update API se change karein:

```http
PUT /api/TenantPolicy/100/versions/502
```

Draft update complete Rule aur Applicability lists replace karta hai. UI ko sirf
edited row nahi, complete desired editor state bhejni hai.

---

## 6. New version Publish hone par old Published version ka kya hoga?

Version 2 Publish hone se pehle:

| Version | Status | IsCurrent | Resolve mein use? |
| --- | --- | ---: | ---: |
| Version 1 | Published | true | Haan |
| Version 2 | Draft/UnderReview/Approved | false | Nahi |

Version 2 Publish hote hi backend:

```text
Version 2.Status    = Published
Version 2.IsCurrent = true
Version 1.IsCurrent = false
```

Result:

| Version | Status | IsCurrent |
| --- | --- | ---: |
| Version 1 | Published | false |
| Version 2 | Published | true |

Old Version 1:

- delete nahi hoti;
- automatically Archived nahi hoti;
- uska `EffectiveTo` automatically set nahi hota;
- audit/history ke liye database mein rehti hai;
- Resolve use ignore karta hai kyunki `IsCurrent=false` hai.

---

## 7. New version system mein kaise use hogi?

Resolve API:

```http
GET /api/TenantPolicy/resolve?employeeId=10021&effectiveDate=2028-02-01&moduleId=102&operationId=4
```

Resolver pehle sirf aisi Version leta hai jo:

```text
PolicyStatus = Published
AND IsCurrent = true
AND Version.IsActive = true
AND Policy.IsActive = true
AND EffectiveFrom <= requested date
AND (EffectiveTo is null OR EffectiveTo >= requested date)
```

Phir employee ki manual Assignment aur Applicability match hoti hai. Manual
assignment highest precedence rakhti hai. Applicability match mein specificity,
priority aur include/exclude behavior backend decide karta hai; UI apna resolution
algorithm na banaye.

Resolve sirf calculation hai. Ye assignment row create nahi karta.

---

## 8. Effective dates ko bilkul clear samjhein

### 8.1 `EffectiveTo = null`

```json
{
  "effectiveFrom": "2027-01-01",
  "effectiveTo": null
}
```

Meaning:

```text
01-01-2027 se start; koi fixed ending date nahi.
```

`null` ka matlab expired ya current date tak nahi. Ye open-ended period hai.

### 8.2 `EffectiveTo` present

```json
{
  "effectiveFrom": "2027-01-01",
  "effectiveTo": "2027-12-31"
}
```

Dono boundary dates inclusive hain:

| Date | Date-range valid? |
| --- | ---: |
| 2026-12-31 | Nahi |
| 2027-01-01 | Haan |
| 2027-12-31 | Haan |
| 2028-01-01 | Nahi |

### 8.3 Version, Applicability aur Assignment ki alag dates

Ek employee resolve hone ke liye relevant levels ki dates valid honi chahiye:

```text
PolicyVersion date window
        AND
PolicyApplicability date window

OR manual case mein

PolicyVersion date window
        AND
PolicyAssignment date window
```

Example:

```text
Version EffectiveFrom       = 2028-01-01
Applicability EffectiveFrom = 2028-04-01
```

January–March mein version date valid ho sakti hai, lekin applicability date valid
nahi hogi. April se matching employee resolve ho sakta hai.

---

## 9. Future-dated new version ka important current behavior

Maan lijiye:

```text
Version 1: EffectiveFrom=2027-01-01, EffectiveTo=null, IsCurrent=true
Version 2: EffectiveFrom=2028-01-01, Approved, IsCurrent=false
```

Agar Version 2 ko 15-12-2027 ko hi Publish kar diya:

```text
Version 1.IsCurrent = false
Version 2.IsCurrent = true
```

20-12-2027 ke Resolve mein:

```text
Version 1 reject → IsCurrent=false
Version 2 reject → EffectiveFrom future mein
Result           → koi matching current version nahi
```

Current backend scheduled activation nahi karta. Safe operational rule:

> Future version ko Approved rakhein aur required effective date par Publish karein,
> jab tak scheduled publishing/current-version logic separately implement na ho.

Historical date Resolve bhi only `IsCurrent=true` versions dekhta hai. Isliye new
version Publish hone ke baad old non-current version historical Resolve mein nahi
aayegi, chahe uski dates historically match karti hon. Ye current implementation
behavior hai, historical version engine samajhkar UI claim na kare.

---

## 10. Existing Assignment ka new version par kya effect hoga?

Assignment `PolicyId` ko nahi, exact `PolicyVersionId` ko point karti hai:

```text
Employee 10021 → VersionId 501
```

Version 2 Publish hone par backend:

- old Version 1 assignment automatically deactivate nahi karta;
- assignment Version 2 par migrate nahi karta;
- Version 2 ke acknowledgement records automatically old version se copy nahi karta.

Version 2 manually assign karni ho:

```http
POST /api/TenantPolicy/assignments
```

```json
{
  "moduleId": 104,
  "operationId": 1,
  "policyVersionId": 502,
  "employeeIds": [10021, 10022],
  "effectiveFrom": "2028-01-01",
  "effectiveTo": null,
  "isMandatory": true
}
```

Old assignment deactivate karni ho:

```http
DELETE /api/TenantPolicy/assignments/{assignmentId}?moduleId=104&operationId=3
```

Assignment removal hard-delete nahi; `PolicyAssignment.IsActive=false` aur removal
actor/time save hote hain.

---

## 11. Policy Status ka complete meaning

Status specific `PolicyVersion` ka hai, main Policy ka nahi:

```text
MH Leave Policy
├─ Version 1 — Published
└─ Version 2 — Draft
```

| Status ID | Status | Meaning | Edit? |
| ---: | --- | --- | ---: |
| 1 | Draft | HR/editor version bana raha hai | Haan |
| 2 | UnderReview | Approval ke liye submitted | Nahi |
| 3 | Approved | Mandatory approval complete; publish pending | Nahi |
| 4 | Published | Live/current ban sakti hai | Nahi |
| 6 | Archived | Retired version | Nahi |
| 7 | Rejected | Correction required | Haan; save par Draft banegi |

Valid transitions:

```text
Draft ──SUBMIT──> UnderReview
Rejected ──SUBMIT──> UnderReview
UnderReview ──APPROVE──> UnderReview ya Approved
UnderReview ──REJECT──> Rejected
Approved ──PUBLISH──> Published
Published ──ARCHIVE──> Archived
```

Invalid status/action combination `409 Conflict` dega.

---

## 12. HR Draft aur Admin Approval requirement

Requirement:

```text
HR          → Policy Draft create/edit/submit kare
Admin       → Approve ya Reject kare
Authorized  → Approved version Publish kare
```

Ismein do independent controls hain.

### 12.1 Endpoint permission

Ye decide karti hai user API call kar sakta hai ya nahi.

Suggested HR permissions under `TENANT_POLICY_DEFINITIONS`:

- View Policy;
- Create Policy;
- Update Draft;
- Clone Version;
- manage editable-version documents;
- Submit.

Suggested Admin permissions under `TENANT_POLICY_APPROVALS`:

- View Approval Progress;
- Approve;
- Reject;
- Publish;
- Archive;
- Approval Stage CRUD, agar Admin setup bhi manage kare.

Actual operation IDs authenticated menu/permission pipeline se milengi.

### 12.2 Approval Stage role

Ye decide karta hai current stage par kis role ka user approval/rejection de sakta
hai.

Admin-only approval stage:

```http
POST /api/TenantPolicy/approval-stages
```

```json
{
  "moduleId": 103,
  "operationId": 1,
  "policyCategoryId": 1,
  "stageName": "Admin Final Approval",
  "stageOrder": 1,
  "approverRoleId": 5,
  "minimumApprovals": 1,
  "isMandatory": true
}
```

Meaning:

| Field | Meaning |
| --- | --- |
| `policyCategoryId=1` | Leave category ke liye stage |
| `stageOrder=1` | First/current stage |
| `approverRoleId=5` | Admin role ID example |
| `minimumApprovals=1` | Ek valid Admin approval required |
| `isMandatory=true` | Iske bina version Approved nahi hogi |

Stage data `PolicyApprovalStage` table mein jata hai.

Permission aur stage role dono pass hone chahiye. Approval permission hone ke baad
bhi wrong role ho to backend `403` dega:

```text
The current approval stage requires a different approver role.
```

`approverRoleId=null` ho to koi bhi caller jiske paas endpoint ki Approve/Reject
permission hai, decision de sakta hai.

---

## 13. One-stage aur multi-stage approval

### Admin-only one-stage flow

```text
Stage 1: Admin Final Approval
Role: Admin
Minimum approvals: 1
```

```text
HR Draft → HR Submit → Admin Approve → Approved → Publish
```

### HR review + Admin final approval

| Order | Stage | Role | Minimum |
| ---: | --- | --- | ---: |
| 1 | HR Review | HR Manager | 1 |
| 2 | Admin Final Approval | Admin | 1 |

Flow:

```text
Draft
  ↓ SUBMIT
UnderReview
  ↓ HR Manager APPROVE
UnderReview
  ↓ Admin APPROVE
Approved
  ↓ PUBLISH
Published
```

Pehli stage complete hone par bhi status `UnderReview` reh sakta hai. Last mandatory
stage complete hone par status `Approved` hota hai.

---

## 14. Complete API process flow

### Step 0: Approval Stage configure karein

```http
GET    /api/TenantPolicy/approval-stages
POST   /api/TenantPolicy/approval-stages
PUT    /api/TenantPolicy/approval-stages/{id}
DELETE /api/TenantPolicy/approval-stages/{id}
```

DELETE logical disable hai; historical approval references delete nahi hote.

### Step 1: HR Policy create kare

```http
POST /api/TenantPolicy
```

Status:

```text
Version 1 = Draft
```

### Step 2: HR Draft edit kare

```http
PUT /api/TenantPolicy/{policyId}/versions/{versionId}
```

Sirf Draft ya Rejected version editable hai. Rejected version save hote hi backend
status Draft set karta hai.

### Step 3: HR Submit kare

```http
POST /api/TenantPolicy/versions/{versionId}/transition
```

```json
{
  "moduleId": 102,
  "operationId": 19,
  "action": "SUBMIT",
  "comments": "Draft completed; Admin approval required."
}
```

Result:

```text
Draft → UnderReview
```

Submit par `PolicyVersion` status aur `PolicyChangeAudit` update hote hain. Approval
History row actual decision ke samay banti hai, Submit ke samay nahi.

### Step 4: Admin progress dekhe

```http
GET /api/TenantPolicy/versions/{versionId}/approval-progress
```

Representative response:

```json
{
  "isSucceeded": true,
  "message": "Policy approval progress retrieved successfully.",
  "data": [
    {
      "stageId": 31,
      "stageName": "Admin Final Approval",
      "stageOrder": 1,
      "minimumApprovals": 1,
      "approvalCount": 0,
      "isComplete": false
    }
  ],
  "errors": []
}
```

### Step 5A: Admin Approve kare

```http
POST /api/TenantPolicy/versions/501/transition
```

```json
{
  "moduleId": 103,
  "operationId": 5,
  "action": "APPROVE",
  "comments": "Leave rules verified and approved."
}
```

Last mandatory stage complete hone par:

```text
UnderReview → Approved
```

### Step 5B: Admin Reject kare

```json
{
  "moduleId": 103,
  "operationId": 6,
  "action": "REJECT",
  "comments": "Carry-forward rule clear nahi hai."
}
```

Result:

```text
UnderReview → Rejected
```

### Step 6: Rejected ho to HR correction kare

```text
Rejected
  ↓ PUT Draft Update
Draft
  ↓ SUBMIT
UnderReview
```

Purani rejection history preserve rehti hai. New decision next sequence mein add
hota hai.

### Step 7: Approved version Publish karein

```json
{
  "moduleId": 103,
  "operationId": 7,
  "action": "PUBLISH",
  "comments": "Activate approved leave version."
}
```

Result:

```text
Approved → Published
New Version.IsCurrent = true
Previous Current Version.IsCurrent = false
```

### Step 8: Optional explicit Assignment

```http
POST /api/TenantPolicy/assignments
```

Assignment ke liye exact Version Published honi chahiye.

### Step 9: Resolve verify karein

```http
GET /api/TenantPolicy/resolve?employeeId=10021&effectiveDate=2028-01-01
```

Resolve read-only calculation hai.

### Step 10: Published version retire karni ho

```json
{
  "moduleId": 103,
  "operationId": 8,
  "action": "ARCHIVE",
  "comments": "Policy version retired."
}
```

Result:

```text
Published → Archived
IsCurrent = false
```

---

## 15. Approval/Reject decision mein table entries

### `PolicyApprovalStage`

Workflow configuration rakhti hai:

```text
Stage name, order, approver role, minimum approvals, mandatory/active flags
```

### `PolicyApprovalHistory`

Har actual Approve/Reject decision ka evidence:

| Field | Example |
| --- | --- |
| `PolicyVersionId` | 501 |
| `PolicyApprovalStageId` | 31 |
| `ActionType` | 1 Approve / 2 Reject |
| `ActionById` | Admin employee ID |
| `ActionDateTime` | UTC timestamp |
| `Comments` | Decision comment |
| `SequenceNumber` | 1, 2, 3… |

Same employee current stage par twice approve nahi kar sakta. `MinimumApprovals=2`
ho to do distinct eligible approvers required honge.

### `PolicyVersion`

Final state aur timestamps:

```text
PolicyStatusId
ApprovedById / ApprovedDateTime
PublishedById / PublishedDateTime
IsCurrent
```

### `PolicyChangeAudit`

CREATE, UPDATE_DRAFT, CLONE, SUBMIT, APPROVE, REJECT, PUBLISH aur ARCHIVE jaise
actions ka audit evidence rakhti hai.

---

## 16. Operation-to-table mapping

| Operation | API | Main persistence effect |
| --- | --- | --- |
| Create Policy | `POST /api/TenantPolicy` | Policy + V1 Draft + Rules + Applicability + Audit |
| Edit Draft/Rejected | `PUT /{policyId}/versions/{versionId}` | Version update; Rules/Scopes replace; Audit |
| Clone | `POST /{policyId}/versions/clone` | Next Draft version; copied children; Audit |
| Submit | `POST /versions/{versionId}/transition` | Version UnderReview; Audit |
| Approve | Same transition API | ApprovalHistory; possibly Version Approved; Audit |
| Reject | Same transition API | ApprovalHistory; Version Rejected; Audit |
| Publish | Same transition API | Version Published/current; previous current unset; Audit |
| Archive | Same transition API | Version Archived/current false; Audit |
| Approval Progress | `GET /versions/{versionId}/approval-progress` | Read-only calculation |
| Assign | `POST /assignments` | Assignment + missing acknowledgement rows + Audit |
| Remove Assignment | `DELETE /assignments/{id}` | Assignment inactive + removed metadata |
| Resolve | `GET /resolve` | Read-only; no assignment created |

---

## 17. Permission mapping

| UI action | Expected module code | Permission intent |
| --- | --- | --- |
| Policy list/detail | `TENANT_POLICY_DEFINITIONS` | View |
| Create Policy | `TENANT_POLICY_DEFINITIONS` | Add/Create |
| Edit Draft | `TENANT_POLICY_DEFINITIONS` | Update |
| Clone Version | `TENANT_POLICY_DEFINITIONS` | Add/Create |
| Submit | `TENANT_POLICY_DEFINITIONS` | Submit |
| Approve/Reject | `TENANT_POLICY_APPROVALS` | Matching operation |
| Publish/Archive | `TENANT_POLICY_APPROVALS` | Matching operation |
| Approval Stage CRUD | `TENANT_POLICY_APPROVALS` | View/Add/Update/Delete |
| Approval Progress | `TENANT_POLICY_APPROVALS` | View |
| Assign/Remove/List | `TENANT_POLICY_ASSIGNMENTS` | Assign/Remove/View |
| Audit | `TENANT_POLICY_AUDIT` | View |

UI button visibility permission se aur enabled/disabled state current Version status
se decide karein. Backend permission pipeline final authority hai.

---

## 18. Specific HR ko Draft assign karne ki limitation

Current model mein:

```text
Policy.OwnerDepartmentId
Policy.AddedById
Policy.UpdatedById
```

available hain, lekin dedicated fields/API nahi mile:

```text
DraftAssigneeEmployeeId
DraftAssigneeRoleId
AssignDraftToHr API
```

Isliye current authorization broadly permission-based hai:

```text
Jis authenticated HR ke paas correct module + operation permission hai,
woh Draft edit kar sakta hai.
```

`OwnerDepartmentId` ko specific editor security samajhna galat hoga; current Draft
update code us field se editor restrict nahi karta.

Agar business requirement ho:

```text
“Version 2 sirf HR employee Ravi edit kare”
```

to ye current API contract se covered nahi hai. Iske liye user-approved ownership/
work-item design ki zarurat hogi; UI apni taraf se assignment simulate na kare.

---

## 19. Approval setup ke important current behaviors

1. Mandatory active stages category-wise aur tenant-wide stages se runtime par read
   hote hain.
2. Submit ke samay approval-stage snapshot rows create nahi hoti.
3. Decision ke samay current active stage configuration use hoti hai.
4. Stage configuration UnderReview ke beech badalne ka in-flight workflow par effect
   ho sakta hai; business ko stage changes carefully control karne chahiye.
5. Koi mandatory stage configured nahi ho to authorized Approve action Version ko
   directly Approved kar sakta hai.
6. Approver role null ho to endpoint permission wala caller approve/reject kar sakta
   hai.
7. Reject kisi bhi current mandatory stage par Version ko Rejected kar deta hai.
8. Rejected correction save karne par Version Draft ban jati hai.
9. Publish ke waqt backend mandatory approval completion/Approved state enforce karta
   hai.

---

## 20. UI button matrix

| Version status | HR buttons | Admin/Approver buttons |
| --- | --- | --- |
| Draft | Edit, Save, Documents, Submit | View |
| UnderReview | View | Approve, Reject, View Progress |
| Rejected | Edit, Save, Resubmit | View decision history |
| Approved | View | Publish |
| Published | View, Clone | Archive, Assign (separate permission) |
| Archived | Read-only | Read-only |

Permission missing ho to button hide/disable ho. Status invalid ho to bhi button
disabled ho. Sirf frontend control security nahi; backend error handle karna zaroori
hai.

---

## 21. Dummy UI flow

```text
+-------------------------------------------------------------------------------+
| MH Leave Policy                                                PolicyId: 100  |
+-------------------------------------------------------------------------------+
| Version | Status       | Effective From | Current | Actions                   |
| 1       | Published    | 01-Jan-2027    | No      | View                      |
| 2       | Under Review | 01-Jan-2028    | No      | View Progress             |
+-------------------------------------------------------------------------------+
| Version 2 Approval Progress                                                 |
| Stage 1: Admin Final Approval     Required: 1     Received: 0     Pending     |
| Comment [______________________________________________________________]     |
|                                              [Reject] [Approve]              |
+-------------------------------------------------------------------------------+
```

Approve ke baad:

```text
Version 2 → Approved
[Publish] button authorized user ko dikhai dega
```

Publish ke baad:

```text
Version 1 → Published, Current=No
Version 2 → Published, Current=Yes
```

---

## 22. Error handling

| Error | UI meaning/action |
| --- | --- |
| `401` | Login/session problem |
| `403` | Module permission ya approval role mismatch |
| `409 A draft version already exists` | Existing Draft open karein |
| `409 invalid action` | Status refresh karein; wrong lifecycle button na dikhayein |
| Same approver duplicate | Progress refresh; duplicate approval block |
| Mandatory approval incomplete | Publish disabled; progress screen dikhayein |
| Non-Published assignment | Assignment action block karein |
| Resolve empty | No current/effective/applicable Published version; assignment delete assume na karein |

Frontend decision sirf response message string par na banaye. HTTP status,
`isSucceeded`, status fields aur documented contract use kare.

---

## 23. Current backend gaps aur business decisions

Ye implemented behavior ko samajhne ke liye critical list hai:

1. New Publish old Version ko delete/archive nahi karta; only `IsCurrent=false`.
2. Old Version ka `EffectiveTo` automatically close nahi hota.
3. Future-dated Version ko early Publish karne par Resolve gap ho sakta hai.
4. Historical-date Resolve old non-current Version nahi select karta.
5. Old Assignments new Version par auto-migrate/deactivate nahi hote.
6. New Published Version automatically employees ko assign nahi hoti.
7. Specific HR Draft-assignee ka table/API nahi hai.
8. Approval-stage snapshot Submit par persist nahi hota; decision runtime setup use
   karta hai.
9. Approve aur Publish alag operations hain.
10. `PolicyApprovalHistory` actual Approve/Reject par banti hai, Submit par nahi.

Inmein koi behavior change karna ho to separate business decision aur explicit
implementation request chahiye. UI ko undocumented behavior invent nahi karna.

---

## 24. Complete example timeline

```text
01-Jan-2027
HR creates MH Leave Policy
→ PolicyId 100
→ VersionId 501 / Version 1 / Draft

02-Jan-2027
HR edits rules
→ Version 1 remains Draft

03-Jan-2027
HR submits
→ Version 1 UnderReview

04-Jan-2027
Admin approves
→ Version 1 Approved

05-Jan-2027
Authorized user publishes
→ Version 1 Published, IsCurrent=true

01-Dec-2027
HR clones Version 1
→ VersionId 502 / Version 2 / Draft
→ Version 1 remains live/current

02-Dec-2027
HR changes 18 days to 20 days
→ Version 2 remains Draft

03-Dec-2027
HR submits
→ Version 2 UnderReview

04-Dec-2027
Admin rejects: carry-forward unclear
→ Version 2 Rejected

05-Dec-2027
HR corrects
→ Version 2 Draft

06-Dec-2027
HR resubmits
→ Version 2 UnderReview

07-Dec-2027
Admin approves
→ Version 2 Approved

01-Jan-2028
Authorized user publishes
→ Version 2 Published, IsCurrent=true
→ Version 1 Published, IsCurrent=false

After Publish
→ Applicability/Resolve Version 2 ko current maanta hai
→ Manual assignments ko separately review/migrate karna hoga
```

---

## 25. Saari 37 Policy APIs — purpose, permission aur use-time

Neeche complete active `TenantPolicyController` catalogue hai. Base route har row
ke aage `/api/TenantPolicy` hai.

### 25.1 Lookups aur Policy Type APIs

| # | Method aur route | Permission module | Kab/kis kaam ke liye |
| ---: | --- | --- | --- |
| 1 | `GET /lookups` | `TENANT_POLICY_TYPES` | Editor dropdowns ke categories, statuses, rule types aur document types load karna |
| 2 | `GET /types` | `TENANT_POLICY_TYPES` | Active/inactive Policy Type list dikhana |
| 3 | `POST /types` | `TENANT_POLICY_TYPES` | Annual Leave jaise Policy Type master banana |
| 4 | `PUT /types/{id}` | `TENANT_POLICY_TYPES` | Policy Type name/category/currency/details update karna |
| 5 | `PATCH /types/{id}/status` | `TENANT_POLICY_TYPES` | Type active/inactive karna; hard-delete nahi |

Recommended screen-load order:

```text
GET /lookups
  ↓
GET /types?isActive=true
  ↓
Policy Create/Edit form bind
```

Type inactive karne se existing Policies/Versions automatically archive nahi hoti.

### 25.2 Policy Definition aur Version APIs

| # | Method aur route | Permission module | Kab/kis kaam ke liye |
| ---: | --- | --- | --- |
| 6 | `GET /` | `TENANT_POLICY_DEFINITIONS` | Paged/search/filter Policy list |
| 7 | `GET /{id}` | `TENANT_POLICY_DEFINITIONS` | Policy, selected/current Version, Rules aur Applicability detail |
| 8 | `POST /` | `TENANT_POLICY_DEFINITIONS` | Policy + Version 1 Draft + children create |
| 9 | `PUT /{policyId}/versions/{versionId}` | `TENANT_POLICY_DEFINITIONS` | Draft/Rejected Version update; Rules/Applicability complete replace |
| 10 | `POST /{policyId}/versions/clone` | `TENANT_POLICY_DEFINITIONS` | Existing Version se next Draft copy banana |
| 11 | `POST /versions/{versionId}/transition` | Action-dependent | `SUBMIT`, `APPROVE`, `REJECT`, `PUBLISH`, `ARCHIVE` |
| 12 | `GET /resolve` | `TENANT_POLICY_DEFINITIONS` | Employee/date ke effective Published current Policies calculate karna |

Transition permission split:

```text
SUBMIT
→ TENANT_POLICY_DEFINITIONS

APPROVE / REJECT / PUBLISH / ARCHIVE
→ TENANT_POLICY_APPROVALS
```

### 25.3 Assignment APIs

| # | Method aur route | Permission module | Kab/kis kaam ke liye |
| ---: | --- | --- | --- |
| 13 | `POST /assignments` | `TENANT_POLICY_ASSIGNMENTS` | Published Version explicitly employees ko assign karna |
| 14 | `DELETE /assignments/{assignmentId}` | `TENANT_POLICY_ASSIGNMENTS` | Assignment deactivate karna; history preserve |
| 15 | `GET /versions/{versionId}/assignments` | `TENANT_POLICY_ASSIGNMENTS` | Active/removed assignments aur removal ID dekhna |

Exact order:

```text
Version Published
  ↓
POST /assignments
  ↓
GET /versions/{versionId}/assignments
  ↓ optional
DELETE /assignments/{assignmentId}
```

### 25.4 Exception APIs

| # | Method aur route | Permission module | Kab/kis kaam ke liye |
| ---: | --- | --- | --- |
| 16 | `POST /exceptions` | `TENANT_POLICY_EXCEPTIONS` | Employee-specific temporary override request create karna |
| 17 | `POST /exceptions/{exceptionId}/decision` | `TENANT_POLICY_EXCEPTIONS` | Exception approve/reject karna |
| 18 | `GET /versions/{versionId}/exceptions` | `TENANT_POLICY_EXCEPTIONS` | Exception request, dates, reason aur decision list karna |

Exception rejection Version rejection nahi hai. Ye sirf employee override request
ka decision hai.

### 25.5 Acknowledgement APIs

| # | Method aur route | Permission module | Kab/kis kaam ke liye |
| ---: | --- | --- | --- |
| 19 | `POST /acknowledgements` | `TENANT_POLICY_ACKNOWLEDGEMENTS` | Logged-in employee apni assigned Version acknowledge kare |
| 20 | `GET /versions/{versionId}/acknowledgements` | `TENANT_POLICY_ACKNOWLEDGEMENTS` | Assignment/acknowledgement evidence list dikhana |

Acknowledge call arbitrary employee ID accept nahi karti. Actor authenticated
employee hota hai aur active assignment required hai.

### 25.6 Document APIs

| # | Method aur route | Permission module | Kab/kis kaam ke liye |
| ---: | --- | --- | --- |
| 21 | `POST /documents` | `TENANT_POLICY_DEFINITIONS` | Editable Version par PDF/DOC/DOCX multipart upload |
| 22 | `GET /versions/{versionId}/documents` | `TENANT_POLICY_DEFINITIONS` | Active documents aur temporary URLs list karna |
| 23 | `DELETE /documents/{documentId}` | `TENANT_POLICY_DEFINITIONS` | Editable Version ka document soft-delete/storage removal |

Upload `multipart/form-data` hai; JSON request nahi. Published/Archived Version ke
documents immutable hain.

### 25.7 Audit API

| # | Method aur route | Permission module | Kab/kis kaam ke liye |
| ---: | --- | --- | --- |
| 24 | `GET /{policyId}/audit` | `TENANT_POLICY_AUDIT` | Newest-first immutable action/actor/version evidence dikhana |

Audit ke liye Policy Definitions View permission reuse nahi karna; dedicated Audit
View permission required hai.

### 25.8 Approval setup aur progress APIs

| # | Method aur route | Permission module | Kab/kis kaam ke liye |
| ---: | --- | --- | --- |
| 25 | `GET /approval-stages` | `TENANT_POLICY_APPROVALS` | Global/category-specific stages load karna |
| 26 | `POST /approval-stages` | `TENANT_POLICY_APPROVALS` | Ordered stage, role aur minimum approvals create karna |
| 27 | `PUT /approval-stages/{id}` | `TENANT_POLICY_APPROVALS` | Stage configuration update karna |
| 28 | `DELETE /approval-stages/{id}` | `TENANT_POLICY_APPROVALS` | Stage disable karna; history delete nahi |
| 29 | `GET /versions/{versionId}/approval-progress` | `TENANT_POLICY_APPROVALS` | Stage-wise required/received/completed progress |

Approval operation sequence:

```text
GET /approval-stages
  ↓ setup only
POST/PUT stage
  ↓ policy lifecycle
SUBMIT transition
  ↓ approver screen
GET /approval-progress
  ↓
APPROVE or REJECT transition
  ↓
GET /approval-progress refresh
```

### 25.9 Bulk Import APIs

Allowed `{target}` values:

```text
types        → Policy Types
definitions  → Policy + Version 1 Draft
assignments  → Published Version assignments
```

Permission module target ke according badalta hai:

```text
types        → TENANT_POLICY_TYPES
definitions  → TENANT_POLICY_DEFINITIONS
assignments  → TENANT_POLICY_ASSIGNMENTS
```

| # | Method aur route | Kab/kis kaam ke liye |
| ---: | --- | --- |
| 30 | `GET /bulk/{target}/template` | Current exact CSV headers/template download |
| 31 | `POST /bulk/{target}/preview` | CSV/XLSX validate karke Draft bulk job aur row errors banana |
| 32 | `POST /bulk/{target}/confirm` | Valid preview ko durable worker processing ke liye queue karna |
| 33 | `GET /bulk/{target}/jobs/{jobId}` | Ek job ka current progress/status poll karna |
| 34 | `GET /bulk/{target}/jobs` | Tenant/target job history list karna |
| 35 | `POST /bulk/{target}/retry` | Failed rows ke liye fresh retry job banana |
| 36 | `POST /bulk/{target}/cancel` | Draft/Queued cancel ya Running cancellation request |
| 37 | `GET /bulk/{target}/jobs/{jobId}/report` | Terminal job ka row-wise CSV result download |

Mandatory bulk order:

```text
GET template
  ↓
POST preview
  ↓ UI validation/duplicates review
POST confirm
  ↓
GET job repeatedly poll
  ↓ terminal status
GET report
  ↓ if retryable failures
POST retry
```

Confirm response ko completion na samjhein. Definition import Draft banata hai,
auto-approve/publish nahi. Assignment import sirf Published Version accept karta hai.

### 25.10 Complete screen-to-API map

| UI screen | APIs |
| --- | --- |
| Policy Type Master | 1–5 |
| Policy Dashboard/List | 1, 2, 6 |
| Policy Create Wizard | 1, 2, 8 |
| Draft Editor | 7, 9, 21–23 |
| Version Clone | 7, 10 |
| Approval Setup | 25–28 |
| Approval Inbox/Viewer | 6, 7, 11, 29 |
| Effective Policy Tester | 12 |
| Assignment Screen | 13–15 |
| Exception Screen | 16–18 |
| Employee Acknowledgement | 19 |
| Acknowledgement Evidence | 20 |
| Audit Timeline | 24 |
| Bulk Import Dialog/History | 30–37 |

---

## 26. Final short summary

```text
HR:
Create → Draft Edit → Submit

Admin:
Review → Approve ya Reject

Rejected:
HR Correct → Draft → Resubmit

Approved:
Authorized user Publish

Publish:
New Version IsCurrent=true
Old Version IsCurrent=false
Old Version delete nahi
Old assignment auto-migrate nahi
```

`EffectiveTo=null` ka final meaning:

> Fixed end date nahi hai, lekin use hone ke liye Version ka Published, Current,
> Active, date-valid aur employee ke liye applicable/assigned hona bhi zaroori hai.

Related detailed documents:

- [Tenant Policy complete API flow](TENANT_POLICY_API_FLOW_HINGLISH.md)
- [Tenant Policy table/data flow](TENANT_POLICY_TABLE_DATA_FLOW_HINGLISH.md)
- [Tenant Policy endpoint catalogue](TENANT_POLICY_ENDPOINT_CATALOG.md)
- [Tenant Policy API handoff](TENANT_POLICY_API.md)
