BEGIN;
WITH institution AS (
    SELECT "Id" FROM skolenza."Institution" WHERE "InstitutionCode" = 'RRSMS' ORDER BY "Id" LIMIT 1
), actor AS (
    SELECT s."Id", s."InstitutionId" FROM skolenza."StaffMember" s JOIN institution i ON i."Id"=s."InstitutionId" WHERE s."IsActive" AND NOT s."IsSoftDeleted" ORDER BY s."Id" LIMIT 1
)
INSERT INTO skolenza."TimetableWorkflowMaster" ("InstitutionId","Code","Name","Description","AllowTeacherDraftProposal","AutoPublishAfterFinalApproval","IsDefault","IsActive","IsSoftDeleted","AddedById","AddedDateTime")
SELECT "InstitutionId",'STANDARD_TIMETABLE_APPROVAL','Standard Timetable Approval','Teachers may propose drafts; the configured academic approver reviews and final approval publishes automatically.',true,true,true,true,false,"Id",CURRENT_TIMESTAMP FROM actor
ON CONFLICT DO NOTHING;

WITH workflow AS (
    SELECT w."Id",w."InstitutionId",w."AddedById" FROM skolenza."TimetableWorkflowMaster" w WHERE w."Code"='STANDARD_TIMETABLE_APPROVAL'
), role_target AS (
    SELECT r."Id",r."InstitutionId" FROM skolenza."Role" r WHERE r."IsActive" AND COALESCE(r."IsSoftDeleted",false)=false AND (upper(r."RoleName") LIKE '%ADMIN%' OR upper(r."RoleName") LIKE '%ACADEMIC%') ORDER BY CASE WHEN upper(r."RoleName") LIKE '%ACADEMIC%' THEN 0 ELSE 1 END, r."Id"
)
INSERT INTO skolenza."TimetableWorkflowStep" ("InstitutionId","TimetableWorkflowMasterId","Sequence","ApproverTypeCode","AccessRoleId","IsActive","AddedById","AddedDateTime")
SELECT w."InstitutionId",w."Id",1,'ACCESS_ROLE',r."Id",true,w."AddedById",CURRENT_TIMESTAMP FROM workflow w JOIN LATERAL (SELECT * FROM role_target r WHERE r."InstitutionId"=w."InstitutionId" LIMIT 1) r ON true
ON CONFLICT ("TimetableWorkflowMasterId","Sequence") DO NOTHING;

UPDATE skolenza."TimetableEntry" e SET "TimetableWorkflowMasterId"=w."Id" FROM skolenza."TimetableWorkflowMaster" w WHERE e."InstitutionId"=w."InstitutionId" AND w."IsDefault" AND e."TimetableWorkflowMasterId" IS NULL;
COMMIT;
