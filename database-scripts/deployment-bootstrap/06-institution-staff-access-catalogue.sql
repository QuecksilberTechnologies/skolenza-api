-- Skolenza Institution and Staff module catalogue.
-- Compatibility-first: persistence tables and API routes are not physically renamed by this script.
-- Idempotent on ModuleCode and OperationName. Numeric Module IDs are resolved dynamically.
BEGIN;

CREATE TEMP TABLE tmp_skolenza_operations ("Id" integer, "OperationName" text, "Remark" text, "OperationType" integer, "IconImage" text) ON COMMIT DROP;
INSERT INTO tmp_skolenza_operations VALUES
(1,'Add','Add a permitted record or queue a new request.',1,'plus-circle'),
(2,'Update','Update a permitted record or apply an approved change.',2,'pencil-square'),
(3,'Delete','Soft-delete or remove a permitted record.',3,'trash3'),
(4,'View','View permitted records and details.',4,'eye'),
(5,'Approve','Approve a pending request',3,'circle-check'),
(6,'Reject','Reject a pending request',3,'circle-x'),
(7,'Hold','Place a request or record on hold',3,'pause'),
(8,'Active','Activate a record',4,'toggle-right'),
(9,'Inactive','Deactivate an active record',4,'toggle-left'),
(10,'Apply','Submit or apply a request',4,'send'),
(11,'Assign','Assign a role, device, resource or responsibility',4,'user-check'),
(12,'Remove','Remove an assignment or association',4,'user-minus'),
(13,'Import','Upload, preview and queue a validated bulk import.',12,'file-up'),
(15,'Upload','Upload a document or file',4,'upload'),
(16,'Download','Download a document or file',4,'download'),
(17,'Verify','Verify submitted information',4,'badge-check'),
(18,'Allow Editing','Allow an staffMember to edit information',4,'file-pen-line'),
(19,'Resend Credentials','Resend login credentials to an staffMember',4,'mail-plus'),
(20,'Restore','Restore a deleted or inactive record',4,'rotate-ccw'),
(21,'Reset Password','Reset the login password of a selected institution staffMember',2,'key-round'),
(23,'Export','Export authorized module data for spreadsheet use.',11,'download'),
(29,'Submit','Submit a draft policy version for review.',19,'send'),
(30,'Review','Review a submitted policy version.',20,'search-check'),
(31,'Publish','Publish an approved policy version.',28,'broadcast'),
(32,'Archive','Archive a retired policy version.',29,'archive'),
(33,'Acknowledge','Record staffMember acknowledgement of a published policy.',30,'check2-circle'),
(34,'Review Compliance 20260919211630','Review a compliance record',411649,'bi bi-clipboard-check');

DO $$
DECLARE r record; existing_name text;
BEGIN
 FOR r IN SELECT * FROM tmp_skolenza_operations LOOP
  SELECT "OperationName" INTO existing_name FROM skolenza."Operation" WHERE "Id"=r."Id";
  IF existing_name IS NOT NULL AND lower(existing_name)<>lower(r."OperationName") THEN
   RAISE EXCEPTION 'Operation Id % is already used by %, expected %',r."Id",existing_name,r."OperationName";
  END IF;
  IF NOT EXISTS (SELECT 1 FROM skolenza."Operation" WHERE lower("OperationName")=lower(r."OperationName")) THEN
   INSERT INTO skolenza."Operation" ("Id","OperationName","Remark","OperationType","IsActive","AddedById","AddedDateTime","IconImage")
   VALUES (r."Id",r."OperationName",r."Remark",r."OperationType",TRUE,1,CURRENT_TIMESTAMP,r."IconImage");
  END IF;
 END LOOP;
 PERFORM setval('skolenza."Operation_Id_seq"',GREATEST((SELECT max("Id") FROM skolenza."Operation"),1),TRUE);
END $$;

CREATE TEMP TABLE tmp_skolenza_modules ("ModuleCode" text,"ModuleName" text,"DisplayName" text,"URLPath" text,"ParentCode" text,"IsLeafNode" boolean,"IsModuleDisplayInUI" boolean,"IsCommonMenu" boolean,"ModuleScope" smallint,"IsActive" boolean,"ImageIconWeb" text,"ImageIconMobile" text,"ItemPriority" integer,"Remark" text,"PageName" text) ON COMMIT DROP;
INSERT INTO tmp_skolenza_modules VALUES
('STAFF_MGMT','Staff Management','Staff Management',NULL,NULL,FALSE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of EMP_MGMT.','staff-management'),
('STAFF_DIRECTORY','Staff Directory','Staff Directory','/app/staff','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of STAFF_DIRECTORY.','staff-directory'),
('STAFF_OVERVIEW','Staff Overview','Staff Overview','/app/profile/basic-info','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of EMP_OVERVIEW.','staff-overview'),
('STAFF_BANK_DETAILS','Staff Bank Details','Bank Details','/app/profile/bank-info','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of EMP_BANK.','staff-bank-details'),
('STAFF_CONTACT_DETAILS','Staff Contact Details','Contact Details','/app/profile/contact-info','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of EMP_CONTACT.','staff-contact-details'),
('STAFF_EXPERIENCE','Staff Experience','Work Experience','/app/profile/experience-info','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of EMP_EXPERIENCE.','staff-experience'),
('STAFF_INSURANCE','Staff Insurance','Insurance','/app/profile/insurance-info','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of EMP_INSURANCE.','staff-insurance'),
('STAFF_IDENTITY','Staff Identity','Identity Details','/app/profile/identity-info','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of EMP_IDENTITY.','staff-identity'),
('STAFF_QUALIFICATIONS','Staff Qualifications','Qualifications','/app/profile/education-info','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of EMP_EDUCATION.','staff-qualifications'),
('STAFF_DEPENDANTS','Staff Dependants','Dependants','/app/profile/dependent-info','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of EMP_DEPENDENTS.','staff-dependants'),
('STAFF_DOCUMENTS','Staff Documents','Documents','/app/profile/files-info','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of EMP_FILES.','staff-documents'),
('STAFF_EMPLOYMENT_TYPES','Staff Employment Types','Employment Types','/app/staff-employment-types','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',1,'Skolenza migration of STAFF_MEMBER_TYPE.','staff-employment-types'),
('STAFF_DEVICE_ENROLMENTS','Staff Device Enrolments','Device Enrolments','/app/staff/device-enrolment','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-person-vcard','badge',50,'Skolenza migration of EMP_DEVICES.','staff-device-enrolments'),
('STAFF_CAMPUS_ASSIGNMENTS','Staff Campus Assignments','Campus Assignments','/app/staff/campus-assignments','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-buildings','business',60,'Skolenza migration of EMP_WORK_LOCATIONS.','staff-campus-assignments'),
('STAFF_WORK_ARRANGEMENTS','Staff Work Arrangements','Work Arrangements','/app/staff/work-arrangements','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-calendar-week','calendar-month',70,'Skolenza migration of EMP_WORK_ARRANGEMENT.','staff-work-arrangements'),
('STAFF_WORK_PATTERNS','Staff Work Patterns','Work Patterns','/app/staff/work-patterns','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-calendar3-range','date-range',80,'Skolenza migration of EMP_WORK_PATTERN.','staff-work-patterns'),
('STAFF_WORK_MODE_OVERRIDES','Staff Work Mode Overrides','Work Mode Overrides','/app/staff/work-mode-overrides','STAFF_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-calendar-event','event-repeat',90,'Skolenza migration of EMP_OVERRIDES.','staff-work-mode-overrides'),
('INSTITUTION_MGMT','Institution Management','Institution Management',NULL,NULL,FALSE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of INSTITUTION_MGMT.','institution-management'),
('INSTITUTION_PROFILE','Institution Profile','Institution Profile','/app/institution/profile','INSTITUTION_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of INSTITUTION_PROFILE.','institution-profile'),
('INSTITUTION_REPORTING_TYPES','Institution Reporting Types','Reporting Types','/app/reporting-types','INSTITUTION_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of INSTITUTION_REPORTING_TYPES.','institution-reporting-types'),
('INSTITUTION_STAFF_CODE','Staff Code Configuration','Staff Code Configuration','/app/staff-code-configuration','INSTITUTION_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of INSTITUTION_STAFF_MEMBER_CODE.','institution-staff-code'),
('INSTITUTION_SETTINGS','Institution Settings','Institution Settings','/app/institution/settings','INSTITUTION_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of INSTITUTION_SETTINGS.','institution-settings'),
('INSTITUTION_SUBSCRIPTION','Institution Subscription','Subscription','/app/institution/subscription','INSTITUTION_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of INSTITUTION_SUBSCRIPTION.','institution-subscription'),
('INSTITUTION_AUDIT_LOGS','Institution Audit Logs','Audit Logs','/app/institution/audit-logs','INSTITUTION_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-grid-3x3-gap','apps',0,'Skolenza migration of INSTITUTION_AUDIT_LOGS.','institution-audit-logs'),
('INSTITUTION_DASHBOARD','Institution Dashboard','Dashboard',NULL,NULL,FALSE,TRUE,FALSE,1,TRUE,'bi bi-speedometer2','dashboard',10,'Skolenza migration of INSTITUTION_DASHBOARD.','institution-dashboard'),
('INSTITUTION_DASHBOARD_OVERVIEW','Institution Dashboard Overview','Overview','/app/admin-dashboard','INSTITUTION_DASHBOARD',TRUE,TRUE,FALSE,1,TRUE,'bi bi-speedometer2','dashboard',10,'Skolenza migration of INSTITUTION_DASHBOARD_OVERVIEW.','institution-dashboard-overview'),
('INSTITUTION_EMAIL_CONFIG','Institution Email Configuration','Email Settings','/app/institution-email-config','INSTITUTION_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-envelope-at','mail',410,'Skolenza migration of INSTITUTION_EMAIL_CONFIG.','institution-email-config'),
('INSTITUTION_EMAIL_TEMPLATE','Institution Email Templates','Email Templates','/app/institution-email-templates','INSTITUTION_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-envelope-paper','mail',420,'Skolenza migration of INSTITUTION_EMAIL_TEMPLATE.','institution-email-template'),
('INSTITUTION_CAMPUSES','Campuses','Campuses','/app/campuses','INSTITUTION_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-geo-alt','location-on',500,'Skolenza migration of INSTITUTION_LOCATIONS.','institution-campuses'),
('INSTITUTION_DEVICE_SETUP','Institution Devices','Installed Devices','/app/institution-devices','INSTITUTION_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-hdd-network','devices',510,'Skolenza migration of INSTITUTION_DEVICE_SETUP.','institution-device-setup'),
('INSTITUTION_DEVICE_CONFIGURATION','Institution Device Configuration','Device Connectivity','/app/institution-device-configurations','INSTITUTION_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-sliders','settings',520,'Skolenza migration of INSTITUTION_DEVICE_CONFIGURATION.','institution-device-configuration'),
('INSTITUTION_DEPARTMENTS','Institution Departments','Departments','/app/departments','INSTITUTION_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-building','business',510,'Institution department option under the Institution Management header.','departments'),
('INSTITUTION_ACCESS','Institution Access','Roles & Permissions','/app/roles','INSTITUTION_MGMT',TRUE,TRUE,FALSE,1,TRUE,'bi bi-shield-lock','shield-account',530,'Institution access option under the Institution Management header.','access-roles'),
('INSTITUTION_POLICIES','Institution Policies','Policies',NULL,NULL,FALSE,TRUE,FALSE,1,TRUE,'bi bi-journal-check','document-text-outline',600,'Skolenza migration of INSTITUTION_POLICIES.','institution-policies'),
('INSTITUTION_POLICY_TYPES','Institution Policy Types','Policy Types','/app/institution-policies/types','INSTITUTION_POLICIES',TRUE,TRUE,FALSE,1,TRUE,'bi bi-tags','pricetags-outline',610,'Skolenza migration of INSTITUTION_POLICY_TYPES.','institution-policy-types'),
('INSTITUTION_POLICY_DEFINITIONS','Institution Policy Definitions','Policy Definitions','/app/institution-policies','INSTITUTION_POLICIES',TRUE,TRUE,FALSE,1,TRUE,'bi bi-file-earmark-text','document-outline',620,'Skolenza migration of INSTITUTION_POLICY_DEFINITIONS.','institution-policy-definitions'),
('INSTITUTION_POLICY_ASSIGNMENTS','Institution Policy Assignments','Policy Assignments','/app/institution-policies/assignments','INSTITUTION_POLICIES',TRUE,TRUE,FALSE,1,TRUE,'bi bi-person-check','person-add-outline',630,'Skolenza migration of INSTITUTION_POLICY_ASSIGNMENTS.','institution-policy-assignments'),
('INSTITUTION_POLICY_EXCEPTIONS','Institution Policy Exceptions','Policy Exceptions','/app/institution-policies/exceptions','INSTITUTION_POLICIES',TRUE,TRUE,FALSE,1,TRUE,'bi bi-sliders','options-outline',640,'Skolenza migration of INSTITUTION_POLICY_EXCEPTIONS.','institution-policy-exceptions'),
('INSTITUTION_POLICY_APPROVALS','Institution Policy Approvals','Policy Approvals','/app/institution-policies/approval-stages','INSTITUTION_POLICIES',TRUE,TRUE,FALSE,1,TRUE,'bi bi-check2-square','checkmark-done-outline',650,'Skolenza migration of INSTITUTION_POLICY_APPROVALS.','institution-policy-approvals'),
('INSTITUTION_POLICY_ACKNOWLEDGEMENTS','Institution Policy Acknowledgements','Policy Acknowledgements','/app/institution-policies/acknowledgements','INSTITUTION_POLICIES',TRUE,TRUE,FALSE,1,TRUE,'bi bi-person-check-fill','reader-outline',660,'Skolenza migration of INSTITUTION_POLICY_ACKNOWLEDGEMENTS.','institution-policy-acknowledgements'),
('INSTITUTION_POLICY_AUDIT','Institution Policy Audit','Policy Audit','/app/institution-policies/audit','INSTITUTION_POLICIES',TRUE,TRUE,FALSE,1,TRUE,'bi bi-clock-history','time-outline',670,'Skolenza migration of INSTITUTION_POLICY_AUDIT.','institution-policy-audit'),
('INSTITUTION_POLICY_HOLIDAY','Institution Holiday Calendar','Holiday Calendar','/app/holidays','INSTITUTION_POLICIES',TRUE,TRUE,FALSE,1,TRUE,'pi pi-calendar','pi pi-calendar',680,'Skolenza migration of INSTITUTION_POLICY_HOLIDAY.','institution-policy-holiday'),
('STAFF_CATEGORIES','Staff Categories','Staff Categories','/app/staff-categories','STAFF_MGMT',TRUE,FALSE,FALSE,1,TRUE,'bi bi-people','users',2,'Teaching and Non-Teaching classification. UI remains hidden until its API slice is complete.','staff-categories'),
('STAFF_FUNCTIONS','Staff Functions','Staff Functions','/app/staff-functions','STAFF_MGMT',TRUE,FALSE,FALSE,1,TRUE,'bi bi-diagram-3','network',3,'Technical, academic, administrative and support functions. UI remains hidden until its API slice is complete.','staff-functions');

DO $$
DECLARE r record; parent_id integer; existing_count integer; inserted_count integer;
BEGIN
 SELECT count(*) INTO existing_count FROM skolenza."Module" m JOIN tmp_skolenza_modules t ON lower(m."ModuleCode")=lower(t."ModuleCode");
 IF existing_count NOT IN (0,(SELECT count(*) FROM tmp_skolenza_modules)) THEN
  RAISE EXCEPTION 'Partial Institution/Staff catalogue detected (% existing of %). Restore/repair before rerunning.',existing_count,(SELECT count(*) FROM tmp_skolenza_modules);
 END IF;
 FOR r IN SELECT * FROM tmp_skolenza_modules WHERE "ParentCode" IS NULL ORDER BY "ItemPriority","ModuleCode" LOOP
  IF NOT EXISTS (SELECT 1 FROM skolenza."Module" WHERE lower("ModuleCode")=lower(r."ModuleCode")) THEN
   INSERT INTO skolenza."Module" ("InstitutionId","ModuleCode","ModuleName","DisplayName","URLPath","ParentModuleId","IsLeafNode","IsModuleDisplayInUI","IsCommonMenu","IsActive","ImageIconWeb","ImageIconMobile","ItemPriority","Remark","AddedById","AddedDateTime","ModuleScope","PageName")
   VALUES (NULL,r."ModuleCode",r."ModuleName",r."DisplayName",r."URLPath",NULL,r."IsLeafNode",r."IsModuleDisplayInUI",r."IsCommonMenu",r."IsActive",r."ImageIconWeb",r."ImageIconMobile",r."ItemPriority",r."Remark",1,CURRENT_TIMESTAMP,r."ModuleScope",r."PageName");
  END IF;
 END LOOP;
 LOOP
  inserted_count := 0;
  FOR r IN
   SELECT t.*
   FROM tmp_skolenza_modules t
   JOIN skolenza."Module" p ON lower(p."ModuleCode")=lower(t."ParentCode") AND p."IsActive"=TRUE
   WHERE t."ParentCode" IS NOT NULL
     AND NOT EXISTS (SELECT 1 FROM skolenza."Module" m WHERE lower(m."ModuleCode")=lower(t."ModuleCode"))
   ORDER BY t."ItemPriority",t."ModuleCode"
  LOOP
   SELECT "Id" INTO parent_id FROM skolenza."Module" WHERE lower("ModuleCode")=lower(r."ParentCode") AND "IsActive"=TRUE;
   INSERT INTO skolenza."Module" ("InstitutionId","ModuleCode","ModuleName","DisplayName","URLPath","ParentModuleId","IsLeafNode","IsModuleDisplayInUI","IsCommonMenu","IsActive","ImageIconWeb","ImageIconMobile","ItemPriority","Remark","AddedById","AddedDateTime","ModuleScope","PageName")
   VALUES (NULL,r."ModuleCode",r."ModuleName",r."DisplayName",r."URLPath",parent_id,r."IsLeafNode",r."IsModuleDisplayInUI",r."IsCommonMenu",r."IsActive",r."ImageIconWeb",r."ImageIconMobile",r."ItemPriority",r."Remark",1,CURRENT_TIMESTAMP,r."ModuleScope",r."PageName");
   inserted_count := inserted_count + 1;
  END LOOP;
  EXIT WHEN inserted_count = 0;
 END LOOP;
 IF EXISTS (
  SELECT 1 FROM tmp_skolenza_modules t
  WHERE NOT EXISTS (SELECT 1 FROM skolenza."Module" m WHERE lower(m."ModuleCode")=lower(t."ModuleCode"))
 ) THEN
  RAISE EXCEPTION 'One or more module parents could not be resolved.';
 END IF;
END $$;

CREATE TEMP TABLE tmp_skolenza_module_operations ("ModuleCode" text,"OperationName" text) ON COMMIT DROP;
INSERT INTO tmp_skolenza_module_operations VALUES
('INSTITUTION_DEPARTMENTS','Add'),
('INSTITUTION_DEPARTMENTS','Update'),
('INSTITUTION_DEPARTMENTS','Delete'),
('INSTITUTION_DEPARTMENTS','View'),
('INSTITUTION_DEPARTMENTS','Active'),
('INSTITUTION_DEPARTMENTS','Inactive'),
('INSTITUTION_DEPARTMENTS','Import'),
('INSTITUTION_DEPARTMENTS','Export'),
('STAFF_BANK_DETAILS','Add'),
('STAFF_BANK_DETAILS','Update'),
('STAFF_BANK_DETAILS','Delete'),
('STAFF_BANK_DETAILS','View'),
('STAFF_BANK_DETAILS','Verify'),
('STAFF_BANK_DETAILS','Allow Editing'),
('STAFF_CONTACT_DETAILS','Add'),
('STAFF_CONTACT_DETAILS','Update'),
('STAFF_CONTACT_DETAILS','Delete'),
('STAFF_CONTACT_DETAILS','View'),
('STAFF_CONTACT_DETAILS','Verify'),
('STAFF_CONTACT_DETAILS','Allow Editing'),
('STAFF_DEPENDANTS','Add'),
('STAFF_DEPENDANTS','Update'),
('STAFF_DEPENDANTS','Delete'),
('STAFF_DEPENDANTS','View'),
('STAFF_DEPENDANTS','Verify'),
('STAFF_DEPENDANTS','Allow Editing'),
('STAFF_DEVICE_ENROLMENTS','Add'),
('STAFF_DEVICE_ENROLMENTS','Update'),
('STAFF_DEVICE_ENROLMENTS','Delete'),
('STAFF_DEVICE_ENROLMENTS','View'),
('STAFF_DEVICE_ENROLMENTS','Assign'),
('STAFF_DEVICE_ENROLMENTS','Remove'),
('STAFF_QUALIFICATIONS','Add'),
('STAFF_QUALIFICATIONS','Update'),
('STAFF_QUALIFICATIONS','Delete'),
('STAFF_QUALIFICATIONS','View'),
('STAFF_QUALIFICATIONS','Verify'),
('STAFF_QUALIFICATIONS','Allow Editing'),
('STAFF_EXPERIENCE','Add'),
('STAFF_EXPERIENCE','Update'),
('STAFF_EXPERIENCE','Delete'),
('STAFF_EXPERIENCE','View'),
('STAFF_EXPERIENCE','Verify'),
('STAFF_EXPERIENCE','Allow Editing'),
('STAFF_DOCUMENTS','Delete'),
('STAFF_DOCUMENTS','View'),
('STAFF_DOCUMENTS','Upload'),
('STAFF_DOCUMENTS','Download'),
('STAFF_DOCUMENTS','Verify'),
('STAFF_DOCUMENTS','Allow Editing'),
('STAFF_IDENTITY','Add'),
('STAFF_IDENTITY','Update'),
('STAFF_IDENTITY','Delete'),
('STAFF_IDENTITY','View'),
('STAFF_IDENTITY','Verify'),
('STAFF_IDENTITY','Allow Editing'),
('STAFF_INSURANCE','Add'),
('STAFF_INSURANCE','Update'),
('STAFF_INSURANCE','Delete'),
('STAFF_INSURANCE','View'),
('STAFF_INSURANCE','Verify'),
('STAFF_INSURANCE','Allow Editing'),
('STAFF_DIRECTORY','Add'),
('STAFF_DIRECTORY','Update'),
('STAFF_DIRECTORY','Delete'),
('STAFF_DIRECTORY','View'),
('STAFF_DIRECTORY','Assign'),
('STAFF_DIRECTORY','Remove'),
('STAFF_DIRECTORY','Import'),
('STAFF_DIRECTORY','Resend Credentials'),
('STAFF_DIRECTORY','Reset Password'),
('STAFF_DIRECTORY','Export'),
('STAFF_WORK_MODE_OVERRIDES','Add'),
('STAFF_WORK_MODE_OVERRIDES','Update'),
('STAFF_WORK_MODE_OVERRIDES','Delete'),
('STAFF_WORK_MODE_OVERRIDES','View'),
('STAFF_WORK_MODE_OVERRIDES','Approve'),
('STAFF_WORK_MODE_OVERRIDES','Reject'),
('STAFF_OVERVIEW','Update'),
('STAFF_OVERVIEW','View'),
('STAFF_OVERVIEW','Verify'),
('STAFF_OVERVIEW','Allow Editing'),
('STAFF_WORK_ARRANGEMENTS','Add'),
('STAFF_WORK_ARRANGEMENTS','Update'),
('STAFF_WORK_ARRANGEMENTS','Delete'),
('STAFF_WORK_ARRANGEMENTS','View'),
('STAFF_WORK_ARRANGEMENTS','Allow Editing'),
('STAFF_CAMPUS_ASSIGNMENTS','Add'),
('STAFF_CAMPUS_ASSIGNMENTS','Update'),
('STAFF_CAMPUS_ASSIGNMENTS','Delete'),
('STAFF_CAMPUS_ASSIGNMENTS','View'),
('STAFF_CAMPUS_ASSIGNMENTS','Allow Editing'),
('STAFF_WORK_PATTERNS','Add'),
('STAFF_WORK_PATTERNS','Update'),
('STAFF_WORK_PATTERNS','Delete'),
('STAFF_WORK_PATTERNS','View'),
('STAFF_WORK_PATTERNS','Allow Editing'),
('STAFF_EMPLOYMENT_TYPES','Add'),
('STAFF_EMPLOYMENT_TYPES','Update'),
('STAFF_EMPLOYMENT_TYPES','Delete'),
('STAFF_EMPLOYMENT_TYPES','View'),
('STAFF_EMPLOYMENT_TYPES','Import'),
('STAFF_EMPLOYMENT_TYPES','Export'),
('INSTITUTION_ACCESS','Add'),
('INSTITUTION_ACCESS','Update'),
('INSTITUTION_ACCESS','Delete'),
('INSTITUTION_ACCESS','View'),
('INSTITUTION_ACCESS','Assign'),
('INSTITUTION_ACCESS','Remove'),
('INSTITUTION_ACCESS','Import'),
('INSTITUTION_ACCESS','Export'),
('INSTITUTION_AUDIT_LOGS','View'),
('INSTITUTION_AUDIT_LOGS','Export'),
('INSTITUTION_DASHBOARD_OVERVIEW','View'),
('INSTITUTION_DEVICE_CONFIGURATION','Add'),
('INSTITUTION_DEVICE_CONFIGURATION','Update'),
('INSTITUTION_DEVICE_CONFIGURATION','Delete'),
('INSTITUTION_DEVICE_CONFIGURATION','View'),
('INSTITUTION_DEVICE_SETUP','Add'),
('INSTITUTION_DEVICE_SETUP','Update'),
('INSTITUTION_DEVICE_SETUP','Delete'),
('INSTITUTION_DEVICE_SETUP','View'),
('INSTITUTION_EMAIL_CONFIG','Add'),
('INSTITUTION_EMAIL_CONFIG','Update'),
('INSTITUTION_EMAIL_CONFIG','Delete'),
('INSTITUTION_EMAIL_CONFIG','View'),
('INSTITUTION_EMAIL_TEMPLATE','Add'),
('INSTITUTION_EMAIL_TEMPLATE','Update'),
('INSTITUTION_EMAIL_TEMPLATE','Delete'),
('INSTITUTION_EMAIL_TEMPLATE','View'),
('INSTITUTION_STAFF_CODE','Add'),
('INSTITUTION_STAFF_CODE','Update'),
('INSTITUTION_STAFF_CODE','View'),
('INSTITUTION_CAMPUSES','Add'),
('INSTITUTION_CAMPUSES','Update'),
('INSTITUTION_CAMPUSES','Delete'),
('INSTITUTION_CAMPUSES','View'),
('INSTITUTION_CAMPUSES','Active'),
('INSTITUTION_CAMPUSES','Inactive'),
('INSTITUTION_POLICY_ACKNOWLEDGEMENTS','View'),
('INSTITUTION_POLICY_ACKNOWLEDGEMENTS','Export'),
('INSTITUTION_POLICY_ACKNOWLEDGEMENTS','Acknowledge'),
('INSTITUTION_POLICY_APPROVALS','View'),
('INSTITUTION_POLICY_APPROVALS','Approve'),
('INSTITUTION_POLICY_APPROVALS','Reject'),
('INSTITUTION_POLICY_APPROVALS','Review'),
('INSTITUTION_POLICY_APPROVALS','Publish'),
('INSTITUTION_POLICY_ASSIGNMENTS','View'),
('INSTITUTION_POLICY_ASSIGNMENTS','Assign'),
('INSTITUTION_POLICY_ASSIGNMENTS','Remove'),
('INSTITUTION_POLICY_ASSIGNMENTS','Import'),
('INSTITUTION_POLICY_ASSIGNMENTS','Export'),
('INSTITUTION_POLICY_AUDIT','View'),
('INSTITUTION_POLICY_AUDIT','Export'),
('INSTITUTION_POLICY_DEFINITIONS','Add'),
('INSTITUTION_POLICY_DEFINITIONS','Update'),
('INSTITUTION_POLICY_DEFINITIONS','Delete'),
('INSTITUTION_POLICY_DEFINITIONS','View'),
('INSTITUTION_POLICY_DEFINITIONS','Approve'),
('INSTITUTION_POLICY_DEFINITIONS','Reject'),
('INSTITUTION_POLICY_DEFINITIONS','Active'),
('INSTITUTION_POLICY_DEFINITIONS','Inactive'),
('INSTITUTION_POLICY_DEFINITIONS','Import'),
('INSTITUTION_POLICY_DEFINITIONS','Upload'),
('INSTITUTION_POLICY_DEFINITIONS','Download'),
('INSTITUTION_POLICY_DEFINITIONS','Export'),
('INSTITUTION_POLICY_DEFINITIONS','Submit'),
('INSTITUTION_POLICY_DEFINITIONS','Review'),
('INSTITUTION_POLICY_DEFINITIONS','Publish'),
('INSTITUTION_POLICY_DEFINITIONS','Archive'),
('INSTITUTION_POLICY_EXCEPTIONS','Add'),
('INSTITUTION_POLICY_EXCEPTIONS','Update'),
('INSTITUTION_POLICY_EXCEPTIONS','Delete'),
('INSTITUTION_POLICY_EXCEPTIONS','View'),
('INSTITUTION_POLICY_EXCEPTIONS','Approve'),
('INSTITUTION_POLICY_EXCEPTIONS','Reject'),
('INSTITUTION_POLICY_EXCEPTIONS','Active'),
('INSTITUTION_POLICY_EXCEPTIONS','Inactive'),
('INSTITUTION_POLICY_HOLIDAY','Add'),
('INSTITUTION_POLICY_HOLIDAY','Update'),
('INSTITUTION_POLICY_HOLIDAY','Delete'),
('INSTITUTION_POLICY_HOLIDAY','View'),
('INSTITUTION_POLICY_HOLIDAY','Import'),
('INSTITUTION_POLICY_HOLIDAY','Export'),
('INSTITUTION_POLICY_TYPES','Add'),
('INSTITUTION_POLICY_TYPES','Update'),
('INSTITUTION_POLICY_TYPES','Delete'),
('INSTITUTION_POLICY_TYPES','View'),
('INSTITUTION_POLICY_TYPES','Active'),
('INSTITUTION_POLICY_TYPES','Inactive'),
('INSTITUTION_POLICY_TYPES','Import'),
('INSTITUTION_POLICY_TYPES','Export'),
('INSTITUTION_PROFILE','Update'),
('INSTITUTION_PROFILE','View'),
('INSTITUTION_REPORTING_TYPES','Add'),
('INSTITUTION_REPORTING_TYPES','Update'),
('INSTITUTION_REPORTING_TYPES','Delete'),
('INSTITUTION_REPORTING_TYPES','View'),
('INSTITUTION_REPORTING_TYPES','Active'),
('INSTITUTION_REPORTING_TYPES','Inactive'),
('INSTITUTION_SETTINGS','Update'),
('INSTITUTION_SETTINGS','View'),
('INSTITUTION_SUBSCRIPTION','View'),
('STAFF_CATEGORIES','Add'),
('STAFF_CATEGORIES','Update'),
('STAFF_CATEGORIES','Delete'),
('STAFF_CATEGORIES','View'),
('STAFF_CATEGORIES','Active'),
('STAFF_CATEGORIES','Inactive'),
('STAFF_FUNCTIONS','Add'),
('STAFF_FUNCTIONS','Update'),
('STAFF_FUNCTIONS','Delete'),
('STAFF_FUNCTIONS','View'),
('STAFF_FUNCTIONS','Active'),
('STAFF_FUNCTIONS','Inactive');

INSERT INTO skolenza."ModuleOperationMapping" ("ModuleId","OperationId","IsActive","Remark","AddedById","AddedDateTime")
SELECT m."Id",o."Id",TRUE,'Skolenza Institution/Staff catalogue operation',1,CURRENT_TIMESTAMP
FROM tmp_skolenza_module_operations x
JOIN skolenza."Module" m ON lower(m."ModuleCode")=lower(x."ModuleCode")
JOIN skolenza."Operation" o ON lower(o."OperationName")=lower(x."OperationName")
WHERE NOT EXISTS (SELECT 1 FROM skolenza."ModuleOperationMapping" mm WHERE mm."ModuleId"=m."Id" AND mm."OperationId"=o."Id");

CREATE TEMP TABLE tmp_institution_types ("Code" text,"Name" text) ON COMMIT DROP;
INSERT INTO tmp_institution_types VALUES ('SCHOOL','School'),('COLLEGE','College'),('UNIVERSITY','University'),('COACHING_INSTITUTE','Coaching Institute'),('TRAINING_INSTITUTE','Training Institute'),('VOCATIONAL_INSTITUTE','Vocational Institute'),('PRESCHOOL','Preschool'),('OTHER','Other Educational Institution');

INSERT INTO skolenza."InstitutionType" ("InstitutionTypeName","Description","Remark","IsActive","AddedById","AddedDateTime","IsSoftDeted")
SELECT x."Name",x."Name" || ' institution type', 'InstitutionType code: ' || x."Code", TRUE,1,CURRENT_TIMESTAMP,FALSE
FROM tmp_institution_types x
WHERE NOT EXISTS (SELECT 1 FROM skolenza."InstitutionType" ti WHERE lower(btrim(ti."InstitutionTypeName"))=lower(x."Name") AND ti."IsSoftDeted" IS NOT TRUE);

DO $$
BEGIN
 IF (SELECT count(*) FROM skolenza."Module" m JOIN tmp_skolenza_modules t ON lower(m."ModuleCode")=lower(t."ModuleCode") WHERE m."IsActive"=TRUE) <> (SELECT count(*) FROM tmp_skolenza_modules) THEN RAISE EXCEPTION 'Module verification failed.'; END IF;
 IF EXISTS (SELECT 1 FROM tmp_skolenza_module_operations x LEFT JOIN skolenza."Module" m ON lower(m."ModuleCode")=lower(x."ModuleCode") LEFT JOIN skolenza."Operation" o ON lower(o."OperationName")=lower(x."OperationName") LEFT JOIN skolenza."ModuleOperationMapping" mm ON mm."ModuleId"=m."Id" AND mm."OperationId"=o."Id" WHERE mm."Id" IS NULL) THEN RAISE EXCEPTION 'Module operation verification failed.'; END IF;
 IF (SELECT count(*) FROM skolenza."InstitutionType" WHERE "IsActive"=TRUE AND "IsSoftDeted" IS NOT TRUE) < 8 THEN RAISE EXCEPTION 'InstitutionType seed verification failed.'; END IF;
END $$;

COMMIT;
