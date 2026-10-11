BEGIN;

CREATE TEMP TABLE host_billing_module_seed
(
    "ModuleCode" varchar(100) PRIMARY KEY,
    "ModuleName" varchar(200) NOT NULL,
    "PageName" varchar(100) NOT NULL,
    "DisplayName" varchar(200) NOT NULL,
    "URLPath" varchar(500) NOT NULL,
    "IconWeb" varchar(100) NOT NULL,
    "IconMobile" varchar(100) NOT NULL,
    "Priority" integer NOT NULL,
    "Operations" text[] NOT NULL
) ON COMMIT DROP;

INSERT INTO host_billing_module_seed VALUES
('HOST_BILLING_CONFIGURATION','Host Billing Configuration','billing-configuration','Billing Configuration','/app/billing/configuration','bi bi-credit-card-2-front','credit-card',190,ARRAY['View','Update']),
('HOST_BILLING_PLAN_PRICES','Host Billing Plan Prices','billing-plan-prices','Plan Pricing','/app/billing/plan-prices','bi bi-tags','sell',200,ARRAY['View','Add','Update','Delete']),
('HOST_BILLING_TAX_RULES','Host Billing Tax Rules','billing-tax-rules','Tax Rules','/app/billing/tax-rules','bi bi-percent','percent',210,ARRAY['View','Add','Update','Delete']),
('HOST_BILLING_TRANSACTIONS','Host Billing Transactions','billing-transactions','Payment Transactions','/app/billing/transactions','bi bi-receipt','receipt-long',220,ARRAY['View']),
('HOST_BILLING_REFUNDS','Host Billing Refunds','billing-refunds','Refunds','/app/billing/refunds','bi bi-arrow-counterclockwise','currency-exchange',230,ARRAY['View','Add']),
('HOST_BILLING_RECONCILIATION','Host Billing Reconciliation','billing-reconciliation','Reconciliation','/app/billing/reconciliation','bi bi-arrow-repeat','sync',240,ARRAY['View','Update']),
('HOST_BILLING_AUDIT','Host Billing Audit','billing-audit','Billing Audit','/app/billing/audit','bi bi-journal-text','history',250,ARRAY['View']);

DO $$ BEGIN
    IF (SELECT COUNT(*) FROM skolenza."Module" WHERE "ModuleCode"='HOST_MANAGEMENT' AND "ModuleScope"=2 AND "IsActive"=TRUE) <> 1 THEN
        RAISE EXCEPTION 'Expected one active HOST_MANAGEMENT parent header.';
    END IF;
END $$;

INSERT INTO skolenza."Module"
("InstitutionId","ModuleCode","ModuleName","PageName","DisplayName","URLPath","ParentModuleId","IsLeafNode","IsModuleDisplayInUI","IsCommonMenu","ModuleScope","IsActive","ImageIconWeb","ImageIconMobile","ItemPriority","Remark","AddedById","AddedDateTime")
SELECT NULL,seed."ModuleCode",seed."ModuleName",seed."PageName",seed."DisplayName",seed."URLPath",parent."Id",TRUE,TRUE,FALSE,2,TRUE,seed."IconWeb",seed."IconMobile",seed."Priority",'HostAdmin billing administration.',1,CURRENT_TIMESTAMP
FROM host_billing_module_seed seed CROSS JOIN skolenza."Module" parent
WHERE parent."ModuleCode"='HOST_MANAGEMENT' AND parent."ModuleScope"=2
AND NOT EXISTS (SELECT 1 FROM skolenza."Module" existing WHERE existing."ModuleCode"=seed."ModuleCode");

UPDATE skolenza."Module" module SET
"ModuleName"=seed."ModuleName","DisplayName"=seed."DisplayName","URLPath"=seed."URLPath","ParentModuleId"=parent."Id","IsLeafNode"=TRUE,"IsModuleDisplayInUI"=TRUE,"IsActive"=TRUE,"ImageIconWeb"=seed."IconWeb","ImageIconMobile"=seed."IconMobile","ItemPriority"=seed."Priority","Remark"='HostAdmin billing administration.',"ModuleScope"=2,"UpdatedById"=1,"UpdatedDateTime"=CURRENT_TIMESTAMP
FROM host_billing_module_seed seed CROSS JOIN skolenza."Module" parent
WHERE module."ModuleCode"=seed."ModuleCode" AND parent."ModuleCode"='HOST_MANAGEMENT' AND parent."ModuleScope"=2;

INSERT INTO skolenza."ModuleOperationMapping"
("ModuleId","OperationId","PageURL","IconURL","IsCommonItem","IsOperational","Priority","Remark","IsActive","AddedById","AddedDateTime")
SELECT module."Id",operation."Id",seed."URLPath",COALESCE(operation."IconImage",seed."IconWeb"),FALSE,TRUE,
CASE operation."OperationName" WHEN 'View' THEN 10 WHEN 'Add' THEN 20 WHEN 'Update' THEN 30 ELSE 40 END,
operation."OperationName"||' '||seed."DisplayName"||'.',TRUE,1,CURRENT_TIMESTAMP
FROM host_billing_module_seed seed JOIN skolenza."Module" module ON module."ModuleCode"=seed."ModuleCode"
JOIN skolenza."Operation" operation ON operation."OperationName"=ANY(seed."Operations") AND operation."IsActive"=TRUE
WHERE NOT EXISTS (SELECT 1 FROM skolenza."ModuleOperationMapping" existing WHERE existing."ModuleId"=module."Id" AND existing."OperationId"=operation."Id");

UPDATE skolenza."ModuleOperationMapping" mapping SET "PageURL"=seed."URLPath","IsOperational"=TRUE,"IsActive"=TRUE,"UpdatedById"=1,"UpdatedDateTime"=CURRENT_TIMESTAMP
FROM host_billing_module_seed seed JOIN skolenza."Module" module ON module."ModuleCode"=seed."ModuleCode" JOIN skolenza."Operation" operation ON operation."OperationName"=ANY(seed."Operations")
WHERE mapping."ModuleId"=module."Id" AND mapping."OperationId"=operation."Id";

INSERT INTO skolenza."HostRoleModuleAndPermission"
("HostRoleId","ModuleId","OperationId","IsActive","IsSoftDeleted","AddedById","AddedDateTime")
SELECT role."Id",module."Id",operation."Id",TRUE,FALSE,1,CURRENT_TIMESTAMP
FROM skolenza."HostRole" role CROSS JOIN host_billing_module_seed seed JOIN skolenza."Module" module ON module."ModuleCode"=seed."ModuleCode" JOIN skolenza."Operation" operation ON operation."OperationName"=ANY(seed."Operations") AND operation."IsActive"=TRUE
WHERE role."Name"='Host-Super-Admin' AND role."IsActive"=TRUE AND role."IsSoftDeleted"=FALSE
AND NOT EXISTS (SELECT 1 FROM skolenza."HostRoleModuleAndPermission" existing WHERE existing."HostRoleId"=role."Id" AND existing."ModuleId"=module."Id" AND existing."OperationId"=operation."Id");

COMMIT;
