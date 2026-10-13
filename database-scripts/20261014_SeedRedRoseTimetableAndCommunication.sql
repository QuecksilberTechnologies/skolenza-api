BEGIN;

DO $$
BEGIN
    IF (SELECT COUNT(*) FROM skolenza."Institution" WHERE "InstitutionCode" = 'RRSMS' AND NOT COALESCE("IsSoftDeleted", false)) <> 1 THEN
        RAISE EXCEPTION 'Expected exactly one active RRSMS institution.';
    END IF;
END $$;

CREATE TEMP TABLE operations_seed_context ON COMMIT DROP AS
SELECT institution."Id" institution_id,
       (SELECT staff."Id" FROM skolenza."StaffMember" staff WHERE staff."InstitutionId" = institution."Id" AND staff."IsActive" AND NOT COALESCE(staff."IsSoftDeleted", false) ORDER BY staff."Id" LIMIT 1) actor_id,
       (SELECT session."Id" FROM skolenza."AcademicSessionMaster" session WHERE session."InstitutionId" = institution."Id" AND session."Code" = 'AY-2026-27') session_id
FROM skolenza."Institution" institution
WHERE institution."InstitutionCode" = 'RRSMS' AND NOT COALESCE(institution."IsSoftDeleted", false);

DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM operations_seed_context WHERE actor_id IS NULL OR session_id IS NULL) THEN
        RAISE EXCEPTION 'Run 20261013_SeedRedRoseSchoolAcademicCurriculum.sql before this seed.';
    END IF;
END $$;

INSERT INTO skolenza."TimetablePeriodMaster"
    ("InstitutionId", "AcademicSessionId", "Code", "Name", "Description", "Sequence", "StartTime", "EndTime", "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime")
SELECT context.institution_id, context.session_id, seed.code, seed.name, seed.description, seed.sequence, seed.start_time, seed.end_time, true, false, context.actor_id, CURRENT_TIMESTAMP
FROM operations_seed_context context
CROSS JOIN (VALUES
    ('P01','Period 1','Morning teaching period',1,TIME '08:00',TIME '08:45'),
    ('P02','Period 2','Morning teaching period',2,TIME '08:50',TIME '09:35'),
    ('P03','Period 3','Morning teaching period',3,TIME '09:40',TIME '10:25'),
    ('P04','Period 4','Pre-lunch teaching period',4,TIME '10:45',TIME '11:30'),
    ('P05','Period 5','Pre-lunch teaching period',5,TIME '11:35',TIME '12:20'),
    ('P06','Period 6','Afternoon teaching period',6,TIME '13:00',TIME '13:45'),
    ('P07','Period 7','Afternoon teaching period',7,TIME '13:50',TIME '14:35')
) seed(code,name,description,sequence,start_time,end_time)
ON CONFLICT ("InstitutionId", "Code") DO UPDATE SET "Name"=EXCLUDED."Name", "Description"=EXCLUDED."Description", "Sequence"=EXCLUDED."Sequence", "StartTime"=EXCLUDED."StartTime", "EndTime"=EXCLUDED."EndTime", "IsActive"=true, "IsSoftDeleted"=false;

INSERT INTO skolenza."StaffTeachingAssignment"
    ("InstitutionId", "StaffMemberId", "CourseOfferingId", "ClassSectionId", "AssignmentType", "ContributionPercent", "EffectiveFrom", "EffectiveTo", "IsPrimary", "IsActive", "AddedById", "AddedDateTime")
SELECT context.institution_id, context.actor_id, offering."Id", section."Id", 1, 100, DATE '2026-04-01', DATE '2027-03-31', true, true, context.actor_id, CURRENT_TIMESTAMP
FROM operations_seed_context context
JOIN skolenza."CourseOfferingMaster" offering ON offering."InstitutionId"=context.institution_id AND offering."Code"='GRADE-05-MATH'
JOIN skolenza."ClassSectionMaster" section ON section."InstitutionId"=context.institution_id AND section."Code"='GRADE-05-A'
ON CONFLICT ("InstitutionId", "StaffMemberId", "ClassSectionId", "CourseOfferingId", "EffectiveFrom") DO UPDATE SET "IsActive"=true, "EffectiveTo"=EXCLUDED."EffectiveTo", "IsPrimary"=true;

INSERT INTO skolenza."TimetableEntry"
    ("InstitutionId", "AcademicSessionId", "ClassSectionId", "CourseOfferingId", "TimetablePeriodMasterId", "StaffTeachingAssignmentId", "DayOfWeek", "RoomReference", "EffectiveFrom", "EffectiveTo", "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime")
SELECT context.institution_id, context.session_id, assignment."ClassSectionId", assignment."CourseOfferingId", period."Id", assignment."Id", day.day_no, 'Room 5-A', DATE '2026-04-01', DATE '2027-03-31', true, false, context.actor_id, CURRENT_TIMESTAMP
FROM operations_seed_context context
JOIN skolenza."StaffTeachingAssignment" assignment ON assignment."InstitutionId"=context.institution_id AND assignment."StaffMemberId"=context.actor_id
JOIN skolenza."TimetablePeriodMaster" period ON period."InstitutionId"=context.institution_id AND period."Code"='P01'
CROSS JOIN (VALUES (1),(2),(3),(4),(5)) day(day_no)
WHERE NOT EXISTS (SELECT 1 FROM skolenza."TimetableEntry" existing WHERE existing."InstitutionId"=context.institution_id AND existing."StaffTeachingAssignmentId"=assignment."Id" AND existing."DayOfWeek"=day.day_no AND existing."TimetablePeriodMasterId"=period."Id" AND existing."EffectiveFrom"=DATE '2026-04-01');

INSERT INTO skolenza."SubstituteWorkflowMaster"
    ("InstitutionId", "Code", "Name", "Description", "RequiresSubstituteAcceptance", "AllowSelfNomination", "AutoApproveWhenNoSteps", "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime")
SELECT institution_id, 'ACADEMIC-COVERAGE', 'Academic class coverage approval', 'Reporting manager approval followed by substitute acceptance.', true, false, false, true, false, actor_id, CURRENT_TIMESTAMP
FROM operations_seed_context
ON CONFLICT ("InstitutionId", "Code") DO UPDATE SET "Name"=EXCLUDED."Name", "Description"=EXCLUDED."Description", "RequiresSubstituteAcceptance"=true, "IsActive"=true, "IsSoftDeleted"=false;

INSERT INTO skolenza."SubstituteWorkflowStep"
    ("InstitutionId", "SubstituteWorkflowMasterId", "Sequence", "ApproverTypeCode", "IsActive", "AddedById", "AddedDateTime")
SELECT context.institution_id, workflow."Id", 1, 'MANAGER', true, context.actor_id, CURRENT_TIMESTAMP
FROM operations_seed_context context JOIN skolenza."SubstituteWorkflowMaster" workflow ON workflow."InstitutionId"=context.institution_id AND workflow."Code"='ACADEMIC-COVERAGE'
ON CONFLICT ("SubstituteWorkflowMasterId", "Sequence") DO UPDATE SET "ApproverTypeCode"='MANAGER', "IsActive"=true;

INSERT INTO skolenza."InstitutionCommunicationSetting"
    ("InstitutionId", "EnableInApp", "EnableEmail", "EnableSms", "EnablePush", "RetentionDays", "IsActive", "AddedById", "AddedDateTime")
SELECT institution_id, true, true, false, false, 730, true, actor_id, CURRENT_TIMESTAMP FROM operations_seed_context
ON CONFLICT ("InstitutionId") DO UPDATE SET "EnableInApp"=true, "EnableEmail"=true, "RetentionDays"=730, "IsActive"=true;

INSERT INTO skolenza."StaffQueryCategoryMaster"
    ("InstitutionId", "Code", "Name", "Description", "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime")
SELECT context.institution_id, seed.code, seed.name, seed.description, true, false, context.actor_id, CURRENT_TIMESTAMP
FROM operations_seed_context context CROSS JOIN (VALUES
    ('ACADEMIC','Academic and curriculum','Curriculum, class allocation and teaching support.'),
    ('ATTENDANCE','Attendance and leave','Attendance, leave balance and absence queries.'),
    ('HR','Employment and HR','Employment records, onboarding and staff documentation.'),
    ('IT','IT support','Account, device and classroom technology support.'),
    ('FACILITIES','Campus facilities','Room, transport, safety and campus facility support.'),
    ('PAYROLL','Payroll and benefits','Salary, deduction and benefit queries.')
) seed(code,name,description)
ON CONFLICT ("InstitutionId", "Code") DO UPDATE SET "Name"=EXCLUDED."Name", "Description"=EXCLUDED."Description", "IsActive"=true, "IsSoftDeleted"=false;

INSERT INTO skolenza."StaffConversation"
    ("InstitutionId", "ConversationTypeCode", "StaffQueryCategoryMasterId", "Subject", "StatusCode", "CreatedByStaffMemberId", "CreatedDateTime", "IsClosed")
SELECT context.institution_id, 'QUERY', category."Id", 'Grade 5 mathematics resource request', 'OPEN', context.actor_id, CURRENT_TIMESTAMP, false
FROM operations_seed_context context JOIN skolenza."StaffQueryCategoryMaster" category ON category."InstitutionId"=context.institution_id AND category."Code"='ACADEMIC'
WHERE NOT EXISTS (SELECT 1 FROM skolenza."StaffConversation" conversation WHERE conversation."InstitutionId"=context.institution_id AND conversation."Subject"='Grade 5 mathematics resource request');

INSERT INTO skolenza."StaffConversationParticipant"
    ("InstitutionId", "StaffConversationId", "StaffMemberId", "ParticipantRoleCode", "LastReadDateTime", "IsMuted", "IsActive")
SELECT context.institution_id, conversation."Id", context.actor_id, 'OWNER', CURRENT_TIMESTAMP, false, true
FROM operations_seed_context context JOIN skolenza."StaffConversation" conversation ON conversation."InstitutionId"=context.institution_id AND conversation."Subject"='Grade 5 mathematics resource request'
ON CONFLICT ("StaffConversationId", "StaffMemberId") DO UPDATE SET "IsActive"=true;

INSERT INTO skolenza."StaffConversationMessage"
    ("InstitutionId", "StaffConversationId", "SenderStaffMemberId", "Body", "SentDateTime", "IsSoftDeleted")
SELECT context.institution_id, conversation."Id", context.actor_id, 'Please arrange additional mathematics practice worksheets for Grade 5-A.', CURRENT_TIMESTAMP, false
FROM operations_seed_context context JOIN skolenza."StaffConversation" conversation ON conversation."InstitutionId"=context.institution_id AND conversation."Subject"='Grade 5 mathematics resource request'
WHERE NOT EXISTS (SELECT 1 FROM skolenza."StaffConversationMessage" message WHERE message."StaffConversationId"=conversation."Id" AND message."Body"='Please arrange additional mathematics practice worksheets for Grade 5-A.');

COMMIT;
