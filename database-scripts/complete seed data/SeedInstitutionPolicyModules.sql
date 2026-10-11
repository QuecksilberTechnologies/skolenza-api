/*
  Institution Policy module, operation and subscription-plan seed.
  PageName values are stable component keys and must not be renamed.
  Rerun-safe: modules and operations are resolved by normalized business keys.
*/
BEGIN;

ALTER TABLE skolenza."Module"
    ADD COLUMN IF NOT EXISTS "PageName" character varying(100);

UPDATE skolenza."Module"
SET "ModuleCode" = 'INSTITUTION_POLICY_HOLIDAY',
    "ModuleName" = 'Holiday',
    "DisplayName" = 'Holiday',
    "UpdatedById" = 1,
    "UpdatedDateTime" = CURRENT_TIMESTAMP
WHERE upper(btrim("ModuleCode")) = 'INSTITUTION_POLICY_HOLIDAY_CALENDAR';

CREATE TEMP TABLE policy_module_seed
(
    "ModuleCode" varchar(50) PRIMARY KEY,
    "ModuleName" varchar(100) NOT NULL,
    "DisplayName" varchar(100) NOT NULL,
    "URLPath" varchar(500),
    "PageName" varchar(100) NOT NULL,
    "ParentCode" varchar(50),
    "IsLeafNode" boolean NOT NULL,
    "ItemPriority" integer NOT NULL,
    "Remark" varchar(500),
    "ImageIconWeb" varchar(100),
    "ImageIconMobile" varchar(100)
) ON COMMIT DROP;

INSERT INTO policy_module_seed VALUES
('INSTITUTION_POLICIES','Institution-Policies','Policies',NULL,'institution-policies-root',NULL,false,600,'Institution policy configuration, applicability, assignment and governance.','bi bi-journal-check','document-text-outline'),
('INSTITUTION_POLICY_TYPES','Policy-Types','Policy Types','/app/policies/types','institution-policy-types','INSTITUTION_POLICIES',true,610,'Institution policy categories and policy-type catalogue.','bi bi-tags','pricetags-outline'),
('INSTITUTION_POLICY_DEFINITIONS','Policy-Definitions','Policy Definitions','/app/policies','institution-policy-definitions','INSTITUTION_POLICIES',true,620,'Versioned policy definitions, rules and documents.','bi bi-file-earmark-text','document-outline'),
('INSTITUTION_POLICY_ASSIGNMENTS','Policy-Assignments','Policy Assignments','/app/policies/assignments','institution-policy-assignments','INSTITUTION_POLICIES',true,630,'StaffMember policy assignments resolved from applicability rules.','bi bi-person-check','person-add-outline'),
('INSTITUTION_POLICY_EXCEPTIONS','Policy-Exceptions','Policy Exceptions','/app/policies/exceptions','institution-policy-exceptions','INSTITUTION_POLICIES',true,640,'Approved staffMember-specific policy overrides.','bi bi-sliders','options-outline'),
('INSTITUTION_POLICY_APPROVALS','Policy-Approvals','Policy Approvals','/app/policies/approvals','institution-policy-approvals','INSTITUTION_POLICIES',true,650,'Policy review, approval, rejection and publication queue.','bi bi-check2-square','checkmark-done-outline'),
('INSTITUTION_POLICY_ACKNOWLEDGEMENTS','Policy-Acknowledgements','Policy Acknowledgements','/app/policies/acknowledgements','institution-policy-acknowledgements','INSTITUTION_POLICIES',true,660,'StaffMember policy delivery, view and acknowledgement tracking.','bi bi-person-check-fill','reader-outline'),
('INSTITUTION_POLICY_AUDIT','Policy-Audit','Policy Audit','/app/policies/audit','institution-policy-audit','INSTITUTION_POLICIES',true,670,'Immutable policy change and lifecycle evidence.','bi bi-clock-history','time-outline'),
('INSTITUTION_POLICY_HOLIDAY','Holiday','Holiday','/app/holidays','institution-policy-holiday-calendar','INSTITUTION_POLICIES',true,680,'Location-based holidays.','bi bi-calendar-event','calendar-outline');

INSERT INTO skolenza."Module"
("InstitutionId","ModuleCode","ModuleName","DisplayName","URLPath","ParentModuleId","IsLeafNode","IsModuleDisplayInUI","IsCommonMenu","ModuleScope","IsActive","ImageIconWeb","ImageIconMobile","ItemPriority","Remark","AddedById","AddedDateTime","PageName")
SELECT NULL, seed."ModuleCode", seed."ModuleName", seed."DisplayName", seed."URLPath", NULL,
       seed."IsLeafNode", true, false, 1, true, seed."ImageIconWeb", seed."ImageIconMobile",
       seed."ItemPriority", seed."Remark", 1, CURRENT_TIMESTAMP, seed."PageName"
FROM policy_module_seed seed
WHERE NOT EXISTS (SELECT 1 FROM skolenza."Module" module WHERE upper(btrim(module."ModuleCode"))=upper(seed."ModuleCode"));

UPDATE skolenza."Module" module
SET "ModuleName"=seed."ModuleName", "DisplayName"=seed."DisplayName", "URLPath"=seed."URLPath",
    "IsLeafNode"=seed."IsLeafNode", "IsModuleDisplayInUI"=true, "IsCommonMenu"=false,
    "ModuleScope"=1, "IsActive"=true, "ImageIconWeb"=seed."ImageIconWeb",
    "ImageIconMobile"=seed."ImageIconMobile", "ItemPriority"=seed."ItemPriority",
    "Remark"=seed."Remark", "PageName"=COALESCE(module."PageName",seed."PageName"),
    "UpdatedById"=1, "UpdatedDateTime"=CURRENT_TIMESTAMP
FROM policy_module_seed seed
WHERE upper(btrim(module."ModuleCode"))=upper(seed."ModuleCode");

UPDATE skolenza."Module" child
SET "ParentModuleId"=parent."Id", "UpdatedById"=1, "UpdatedDateTime"=CURRENT_TIMESTAMP
FROM policy_module_seed seed
JOIN skolenza."Module" parent ON upper(btrim(parent."ModuleCode"))=upper(seed."ParentCode")
WHERE upper(btrim(child."ModuleCode"))=upper(seed."ModuleCode") AND seed."ParentCode" IS NOT NULL;

UPDATE skolenza."Module" parent SET "ParentModuleId"=NULL
WHERE upper(btrim(parent."ModuleCode"))='INSTITUTION_POLICIES';

CREATE TEMP TABLE policy_operation_seed
("OperationName" varchar(100) PRIMARY KEY, "OperationType" integer, "Remark" varchar(500), "IconImage" varchar(100)) ON COMMIT DROP;
INSERT INTO policy_operation_seed VALUES
('Submit',19,'Submit a draft policy version for review.','send'),
('Review',20,'Review a submitted policy version.','search-check'),
('Publish',28,'Publish an approved policy version.','broadcast'),
('Archive',29,'Archive a retired policy version.','archive'),
('Acknowledge',30,'Record staffMember acknowledgement of a published policy.','check2-circle');

INSERT INTO skolenza."Operation"
("OperationName","OperationType","Remark","IsActive","AddedById","AddedDateTime","IconImage")
SELECT seed."OperationName",seed."OperationType",seed."Remark",true,1,CURRENT_TIMESTAMP,seed."IconImage"
FROM policy_operation_seed seed
WHERE NOT EXISTS (SELECT 1 FROM skolenza."Operation" operation WHERE lower(btrim(operation."OperationName"))=lower(seed."OperationName"));

CREATE TEMP TABLE policy_module_operation_seed
("ModuleCode" varchar(50), "OperationName" varchar(100), "OperationType" integer, "Priority" integer,
 PRIMARY KEY ("ModuleCode", "OperationName")) ON COMMIT DROP;
INSERT INTO policy_module_operation_seed VALUES
('INSTITUTION_POLICY_TYPES','View',4,10),('INSTITUTION_POLICY_TYPES','Add',1,20),('INSTITUTION_POLICY_TYPES','Update',2,30),('INSTITUTION_POLICY_TYPES','Delete',3,40),('INSTITUTION_POLICY_TYPES','Active',4,50),('INSTITUTION_POLICY_TYPES','Inactive',4,60),('INSTITUTION_POLICY_TYPES','Import',12,70),('INSTITUTION_POLICY_TYPES','Export',11,80),
('INSTITUTION_POLICY_DEFINITIONS','View',4,10),('INSTITUTION_POLICY_DEFINITIONS','Add',1,20),('INSTITUTION_POLICY_DEFINITIONS','Update',2,30),('INSTITUTION_POLICY_DEFINITIONS','Delete',3,40),('INSTITUTION_POLICY_DEFINITIONS','Active',4,50),('INSTITUTION_POLICY_DEFINITIONS','Inactive',4,60),('INSTITUTION_POLICY_DEFINITIONS','Import',12,70),('INSTITUTION_POLICY_DEFINITIONS','Export',11,80),('INSTITUTION_POLICY_DEFINITIONS','Upload',14,90),('INSTITUTION_POLICY_DEFINITIONS','Download',13,100),('INSTITUTION_POLICY_DEFINITIONS','Submit',19,110),('INSTITUTION_POLICY_DEFINITIONS','Review',20,120),('INSTITUTION_POLICY_DEFINITIONS','Approve',5,130),('INSTITUTION_POLICY_DEFINITIONS','Reject',6,140),('INSTITUTION_POLICY_DEFINITIONS','Publish',28,150),('INSTITUTION_POLICY_DEFINITIONS','Archive',29,160),
('INSTITUTION_POLICY_ASSIGNMENTS','View',4,10),('INSTITUTION_POLICY_ASSIGNMENTS','Assign',7,20),('INSTITUTION_POLICY_ASSIGNMENTS','Remove',4,30),('INSTITUTION_POLICY_ASSIGNMENTS','Import',12,40),('INSTITUTION_POLICY_ASSIGNMENTS','Export',11,50),('INSTITUTION_POLICY_ASSIGNMENTS','Download',13,60),
('INSTITUTION_POLICY_EXCEPTIONS','View',4,10),('INSTITUTION_POLICY_EXCEPTIONS','Add',1,20),('INSTITUTION_POLICY_EXCEPTIONS','Update',2,30),('INSTITUTION_POLICY_EXCEPTIONS','Delete',3,40),('INSTITUTION_POLICY_EXCEPTIONS','Approve',5,50),('INSTITUTION_POLICY_EXCEPTIONS','Reject',6,60),('INSTITUTION_POLICY_EXCEPTIONS','Active',4,70),('INSTITUTION_POLICY_EXCEPTIONS','Inactive',4,80),
('INSTITUTION_POLICY_APPROVALS','View',4,10),('INSTITUTION_POLICY_APPROVALS','Review',20,20),('INSTITUTION_POLICY_APPROVALS','Approve',5,30),('INSTITUTION_POLICY_APPROVALS','Reject',6,40),('INSTITUTION_POLICY_APPROVALS','Publish',28,50),
('INSTITUTION_POLICY_ACKNOWLEDGEMENTS','View',4,10),('INSTITUTION_POLICY_ACKNOWLEDGEMENTS','Acknowledge',30,20),('INSTITUTION_POLICY_ACKNOWLEDGEMENTS','Export',11,30),
('INSTITUTION_POLICY_AUDIT','View',4,10),('INSTITUTION_POLICY_AUDIT','Export',11,20),
('INSTITUTION_POLICY_HOLIDAY','View',4,10),
('INSTITUTION_POLICY_HOLIDAY','Add',1,20),
('INSTITUTION_POLICY_HOLIDAY','Update',2,30),
('INSTITUTION_POLICY_HOLIDAY','Delete',3,40),
('INSTITUTION_POLICY_HOLIDAY','Import',12,50),
('INSTITUTION_POLICY_HOLIDAY','Export',11,60);

INSERT INTO skolenza."ModuleOperationMapping"
("ModuleId","OperationId","PageURL","IconURL","IsCommonItem","IsOperational","Priority","Remark","IsActive","AddedById","AddedDateTime")
SELECT module."Id", operation."Id", module."URLPath", operation."IconImage", false, true,
       seed."Priority", operation."OperationName" || ' permission for ' || module."DisplayName" || '.',
       true,1,CURRENT_TIMESTAMP
FROM policy_module_operation_seed seed
JOIN skolenza."Module" module ON upper(btrim(module."ModuleCode"))=upper(seed."ModuleCode")
JOIN LATERAL
(
    SELECT candidate.* FROM skolenza."Operation" candidate
    WHERE lower(btrim(candidate."OperationName"))=lower(seed."OperationName")
    ORDER BY CASE WHEN candidate."OperationType"=seed."OperationType" THEN 0 ELSE 1 END, candidate."Id"
    LIMIT 1
) operation ON true
WHERE NOT EXISTS
(
    SELECT 1 FROM skolenza."ModuleOperationMapping" existing
    WHERE existing."ModuleId"=module."Id" AND existing."OperationId"=operation."Id"
);

UPDATE skolenza."ModuleOperationMapping" mapping
SET "IsActive"=true,"IsOperational"=true,"Priority"=seed."Priority",
    "PageURL"=module."URLPath","UpdatedById"=1,"UpdatedDateTime"=CURRENT_TIMESTAMP
FROM policy_module_operation_seed seed
JOIN skolenza."Module" module ON upper(btrim(module."ModuleCode"))=upper(seed."ModuleCode")
JOIN LATERAL
(
    SELECT candidate."Id" FROM skolenza."Operation" candidate
    WHERE lower(btrim(candidate."OperationName"))=lower(seed."OperationName")
    ORDER BY CASE WHEN candidate."OperationType"=seed."OperationType" THEN 0 ELSE 1 END, candidate."Id" LIMIT 1
) operation ON true
WHERE mapping."ModuleId"=module."Id" AND mapping."OperationId"=operation."Id";

INSERT INTO skolenza."PlanModuleMapping"
("SubscriptionPlanId","ModuleId","IsActive","Remark","AddedById","AddedDateTime")
SELECT plan."Id",module."Id",true,'Institution policy module entitlement.',1,CURRENT_TIMESTAMP
FROM skolenza."SubscriptionPlan" plan
CROSS JOIN skolenza."Module" module
WHERE plan."IsActive"=true
  AND module."ModuleCode" IN (SELECT "ModuleCode" FROM policy_module_seed)
  AND NOT EXISTS (SELECT 1 FROM skolenza."PlanModuleMapping" existing WHERE existing."SubscriptionPlanId"=plan."Id" AND existing."ModuleId"=module."Id");

UPDATE skolenza."PlanModuleMapping" mapping SET "IsActive"=true,"UpdatedById"=1,"UpdatedDateTime"=CURRENT_TIMESTAMP
FROM skolenza."Module" module
WHERE mapping."ModuleId"=module."Id" AND module."ModuleCode" IN (SELECT "ModuleCode" FROM policy_module_seed);

COMMIT;

SELECT module."ModuleCode",module."DisplayName",parent."ModuleCode" AS "ParentCode",module."PageName",operation."OperationName"
FROM skolenza."Module" module
LEFT JOIN skolenza."Module" parent ON parent."Id"=module."ParentModuleId"
LEFT JOIN skolenza."ModuleOperationMapping" mapping ON mapping."ModuleId"=module."Id" AND mapping."IsActive"=true
LEFT JOIN skolenza."Operation" operation ON operation."Id"=mapping."OperationId"
WHERE module."ModuleCode" LIKE 'INSTITUTION_POLIC%'
ORDER BY module."ItemPriority",mapping."Priority";
