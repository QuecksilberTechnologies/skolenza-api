/*
    Institution clean-up script (PostgreSQL / pgAdmin compatible)

    Purpose
    -------
    Physically remove either one institution or every institution with their institution-scoped
    staffMember data, so fresh institution entries can be created. The script was built
    from the live skolenza schema's InstitutionId / StaffMemberId columns and foreign-key
    metadata.

    Safety
    ------
    1. To clean every institution, set ClearAllInstitutions = TRUE and leave InstitutionId NULL.
       To clean one institution only, set ClearAllInstitutions = FALSE and provide the
       numeric skolenza."Institution"."Id" value (never an encrypted ID).
    2. First run with ExecuteCleanup = FALSE. It produces per-table counts and
       commits no deletion.
    3. Verify the output, take a database backup, then change ExecuteCleanup to
       TRUE and run the entire file in one batch.
    4. An error before COMMIT causes the transaction to roll back. Do not run
       individual DELETE statements from this file separately.

    Notes
    -----
    - This removes database rows only. It does not remove physical files from
      object storage/local disk referenced by ticket or staffMember attachments.
    - Host records, HostRole, Operation, SubscriptionPlan and other host/shared
      configuration are deliberately preserved.
    - With ReplaceLocationReferenceData = FALSE (the safe default), India and
      China are upserted with one capital-level city per state/region while
      existing global country rules and other country data remain untouched.
    - Replacing the entire Country/State/City catalog requires
      ReplaceLocationReferenceData = TRUE. It is fail-safe and aborts if any
      remaining non-institution Country/State/City dependent data is detected.
*/

BEGIN;

CREATE TEMP TABLE institution_cleanup_config
(
    "InstitutionId" bigint NULL,
    "ClearAllInstitutions" boolean NOT NULL,
    "ExecuteCleanup" boolean NOT NULL,
    "ReplaceLocationReferenceData" boolean NOT NULL
) ON COMMIT DROP;

-- CHANGE ONLY THIS LINE.
-- All institutions: (NULL, TRUE, FALSE, FALSE). One institution: (54, FALSE, FALSE, FALSE).
-- Run once with ExecuteCleanup = FALSE. After reviewing the preview and taking
-- a backup, change only ExecuteCleanup to TRUE and run this whole file.
INSERT INTO institution_cleanup_config
    ("InstitutionId", "ClearAllInstitutions", "ExecuteCleanup", "ReplaceLocationReferenceData")
VALUES (NULL, TRUE, FALSE, FALSE);

-- Intentionally a compact reference catalog: every India state/UT and China
-- province/region/municipality receives one capital-level city. It is not a
-- full city directory; add more cities later through the location master API.
CREATE TEMP TABLE location_reference_seed
(
    "SortOrder" integer PRIMARY KEY,
    "CountryCode" character varying(10) NOT NULL,
    "StateName" character varying(100) NOT NULL,
    "CityName" character varying(100) NOT NULL
) ON COMMIT DROP;

INSERT INTO location_reference_seed ("SortOrder", "CountryCode", "StateName", "CityName")
VALUES
    (1,  'IN', 'Andhra Pradesh', 'Amaravati'),
    (2,  'IN', 'Arunachal Pradesh', 'Itanagar'),
    (3,  'IN', 'Assam', 'Dispur'),
    (4,  'IN', 'Bihar', 'Patna'),
    (5,  'IN', 'Chhattisgarh', 'Raipur'),
    (6,  'IN', 'Goa', 'Panaji'),
    (7,  'IN', 'Gujarat', 'Gandhinagar'),
    (8,  'IN', 'Haryana', 'Chandigarh'),
    (9,  'IN', 'Himachal Pradesh', 'Shimla'),
    (10, 'IN', 'Jharkhand', 'Ranchi'),
    (11, 'IN', 'Karnataka', 'Bengaluru'),
    (12, 'IN', 'Kerala', 'Thiruvananthapuram'),
    (13, 'IN', 'Madhya Pradesh', 'Bhopal'),
    (14, 'IN', 'Maharashtra', 'Mumbai'),
    (15, 'IN', 'Manipur', 'Imphal'),
    (16, 'IN', 'Meghalaya', 'Shillong'),
    (17, 'IN', 'Mizoram', 'Aizawl'),
    (18, 'IN', 'Nagaland', 'Kohima'),
    (19, 'IN', 'Odisha', 'Bhubaneswar'),
    (20, 'IN', 'Punjab', 'Chandigarh'),
    (21, 'IN', 'Rajasthan', 'Jaipur'),
    (22, 'IN', 'Sikkim', 'Gangtok'),
    (23, 'IN', 'Tamil Nadu', 'Chennai'),
    (24, 'IN', 'Telangana', 'Hyderabad'),
    (25, 'IN', 'Tripura', 'Agartala'),
    (26, 'IN', 'Uttar Pradesh', 'Lucknow'),
    (27, 'IN', 'Uttarakhand', 'Dehradun'),
    (28, 'IN', 'West Bengal', 'Kolkata'),
    (29, 'IN', 'Andaman and Nicobar Islands', 'Port Blair'),
    (30, 'IN', 'Chandigarh', 'Chandigarh'),
    (31, 'IN', 'Dadra and Nagar Haveli and Daman and Diu', 'Daman'),
    (32, 'IN', 'Delhi', 'New Delhi'),
    (33, 'IN', 'Jammu and Kashmir', 'Srinagar'),
    (34, 'IN', 'Ladakh', 'Leh'),
    (35, 'IN', 'Lakshadweep', 'Kavaratti'),
    (36, 'IN', 'Puducherry', 'Puducherry'),
    (37, 'CN', 'Anhui', 'Hefei'),
    (38, 'CN', 'Beijing', 'Beijing'),
    (39, 'CN', 'Chongqing', 'Chongqing'),
    (40, 'CN', 'Fujian', 'Fuzhou'),
    (41, 'CN', 'Gansu', 'Lanzhou'),
    (42, 'CN', 'Guangdong', 'Guangzhou'),
    (43, 'CN', 'Guangxi', 'Nanning'),
    (44, 'CN', 'Guizhou', 'Guiyang'),
    (45, 'CN', 'Hainan', 'Haikou'),
    (46, 'CN', 'Hebei', 'Shijiazhuang'),
    (47, 'CN', 'Heilongjiang', 'Harbin'),
    (48, 'CN', 'Henan', 'Zhengzhou'),
    (49, 'CN', 'Hubei', 'Wuhan'),
    (50, 'CN', 'Hunan', 'Changsha'),
    (51, 'CN', 'Inner Mongolia', 'Hohhot'),
    (52, 'CN', 'Jiangsu', 'Nanjing'),
    (53, 'CN', 'Jiangxi', 'Nanchang'),
    (54, 'CN', 'Jilin', 'Changchun'),
    (55, 'CN', 'Liaoning', 'Shenyang'),
    (56, 'CN', 'Macao', 'Macao'),
    (57, 'CN', 'Ningxia', 'Yinchuan'),
    (58, 'CN', 'Qinghai', 'Xining'),
    (59, 'CN', 'Shaanxi', 'Xi''an'),
    (60, 'CN', 'Shandong', 'Jinan'),
    (61, 'CN', 'Shanghai', 'Shanghai'),
    (62, 'CN', 'Shanxi', 'Taiyuan'),
    (63, 'CN', 'Sichuan', 'Chengdu'),
    (64, 'CN', 'Tianjin', 'Tianjin'),
    (65, 'CN', 'Tibet', 'Lhasa'),
    (66, 'CN', 'Xinjiang', 'Urumqi'),
    (67, 'CN', 'Yunnan', 'Kunming'),
    (68, 'CN', 'Zhejiang', 'Hangzhou'),
    (69, 'CN', 'Hong Kong', 'Hong Kong'),
    (70, 'CN', 'Taiwan', 'Taipei');

DO
$$
DECLARE
    v_institution_id bigint;
    v_clear_all_institutions boolean;
    v_execute_cleanup boolean;
    v_replace_location_reference_data boolean;
    v_count bigint;
    v_institution_count bigint;
    v_dependency record;
    v_sequence record;
    v_reference_table text;
    v_reference_sequence text;
BEGIN
    SELECT "InstitutionId", "ClearAllInstitutions", "ExecuteCleanup", "ReplaceLocationReferenceData"
    INTO v_institution_id, v_clear_all_institutions, v_execute_cleanup, v_replace_location_reference_data
    FROM pg_temp.institution_cleanup_config;

    IF NOT v_clear_all_institutions AND (v_institution_id IS NULL OR v_institution_id <= 0) THEN
        RAISE EXCEPTION 'For a single-institution cleanup, set a valid numeric Institution.Id in institution_cleanup_config.';
    END IF;

    CREATE TEMP TABLE institution_cleanup_scope ON COMMIT DROP AS
    SELECT "Id"
    FROM skolenza."Institution"
    WHERE v_clear_all_institutions OR "Id" = v_institution_id;

    IF NOT EXISTS (SELECT 1 FROM pg_temp.institution_cleanup_scope) THEN
        RAISE EXCEPTION 'No institution matched the cleanup configuration.';
    END IF;

    SELECT COUNT(*) INTO v_institution_count FROM pg_temp.institution_cleanup_scope;

    -- The full staffMember set is used for preview. It is narrowed to one institution
    -- for each delete iteration below.
    CREATE TEMP TABLE institution_cleanup_staffMember ("Id" bigint PRIMARY KEY) ON COMMIT DROP;
    INSERT INTO pg_temp.institution_cleanup_staffMember ("Id")
    SELECT "Id"
    FROM skolenza."StaffMember"
    WHERE "InstitutionId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_scope);

    CREATE TEMP TABLE institution_cleanup_preflight
    (
        "Scope" text NOT NULL,
        "TableName" text NOT NULL,
        "MatchColumn" text NOT NULL,
        "RecordCount" bigint NOT NULL
    ) ON COMMIT DROP;

    -- Inventory every table that explicitly stores InstitutionId or StaffMemberId,
    -- including legacy tables that do not have an FK constraint.
    FOR v_dependency IN
        SELECT c.relname AS table_name, a.attname AS column_name
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = c.oid
        WHERE n.nspname = 'skolenza'
          AND c.relkind IN ('r', 'p')
          AND a.attnum > 0
          AND NOT a.attisdropped
          AND a.attname IN ('InstitutionId', 'StaffMemberId')
        ORDER BY a.attname, c.relname
    LOOP
        IF v_dependency.column_name = 'InstitutionId' THEN
            EXECUTE format(
                'INSERT INTO pg_temp.institution_cleanup_preflight ("Scope", "TableName", "MatchColumn", "RecordCount")
                 SELECT ''Institution'', %L, %L, COUNT(*)
                 FROM skolenza.%I
                 WHERE %I IN (SELECT "Id" FROM pg_temp.institution_cleanup_scope)',
                v_dependency.table_name,
                v_dependency.column_name,
                v_dependency.table_name,
                v_dependency.column_name);
        ELSE
            EXECUTE format(
                'INSERT INTO pg_temp.institution_cleanup_preflight ("Scope", "TableName", "MatchColumn", "RecordCount")
                 SELECT ''StaffMember'', %L, %L, COUNT(*)
                 FROM skolenza.%I
                 WHERE %I IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)',
                v_dependency.table_name,
                v_dependency.column_name,
                v_dependency.table_name,
                v_dependency.column_name);
        END IF;
    END LOOP;

    -- These rows are reached through LoginCredential, Ticket, TicketThread,
    -- PayrollStaffMember, or UserRole rather than through a direct InstitutionId / StaffMemberId column.
    INSERT INTO pg_temp.institution_cleanup_preflight ("Scope", "TableName", "MatchColumn", "RecordCount")
    VALUES
    ('Indirect', 'RefreshToken', 'LoginCredentialId',
        (SELECT COUNT(*) FROM skolenza."RefreshToken" rt
         WHERE rt."LoginCredentialId" IN
         (
             SELECT lc."Id" FROM skolenza."LoginCredential" lc
             WHERE lc."InstitutionId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_scope)
                OR lc."StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
         ))),
    ('Indirect', 'TicketAttachment', 'TicketId',
        (SELECT COUNT(*) FROM skolenza."TicketAttachment" ta
         WHERE ta."TicketId" IN
         (
             SELECT t."Id" FROM skolenza."Ticket" t
             WHERE t."InstitutionId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_scope)
         ))),
    ('Indirect', 'ThreadMessage', 'ThreadId',
        (SELECT COUNT(*) FROM skolenza."ThreadMessage" tm
         WHERE tm."ThreadId" IN
         (
             SELECT tt."Id" FROM skolenza."TicketThread" tt
             WHERE tt."InstitutionId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_scope)
         ))),
    ('Indirect', 'PayrollStaffMemberDetail', 'PayrollStaffMemberId',
        (SELECT COUNT(*) FROM skolenza."PayrollStaffMemberDetail" ped
         WHERE ped."PayrollStaffMemberId" IN
         (
             SELECT pe."Id" FROM skolenza."PayrollStaffMember" pe
             WHERE pe."StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
                OR pe."PayrollRunId" IN
                (
                    SELECT pr."Id" FROM skolenza."PayrollRun" pr
                    WHERE pr."InstitutionId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_scope)
                )
         ))),
    ('Indirect', 'StaffMemberExperienceDocument', 'StaffMemberExperienceId',
        (SELECT COUNT(*) FROM skolenza."StaffMemberExperienceDocument" eed
         WHERE eed."StaffMemberExperienceId" IN
         (
             SELECT ee."Id" FROM skolenza."StaffMemberExperience" ee
             WHERE ee."StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
         ))),
    ('Indirect', 'InstitutionDeviceConfiguration', 'InstitutionDeviceId',
        (SELECT COUNT(*) FROM skolenza."InstitutionDeviceConfiguration" tdc
         WHERE tdc."InstitutionDeviceId" IN
         (
             SELECT td."Id" FROM skolenza."InstitutionDevice" td
             WHERE td."InstitutionId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_scope)
         ))),
    ('Reference', 'Country', 'Id', (SELECT COUNT(*) FROM skolenza."Country")),
    ('Reference', 'State', 'Id', (SELECT COUNT(*) FROM skolenza."State")),
    ('Reference', 'City', 'Id', (SELECT COUNT(*) FROM skolenza."City")),
    ('Reference', 'District', 'Id', (SELECT COUNT(*) FROM skolenza."District")),
    ('Reference', 'CountryIdentityRule', 'Id', (SELECT COUNT(*) FROM skolenza."CountryIdentityRule")),
    ('Reference', 'CountryStatutoryRule', 'Id', (SELECT COUNT(*) FROM skolenza."CountryStatutoryRule")),
    ('Reference', 'StatutoryType', 'Id', (SELECT COUNT(*) FROM skolenza."StatutoryType")),
    ('Reference', 'TaxSystemMaster', 'Id', (SELECT COUNT(*) FROM skolenza."TaxSystemMaster")),
    ('Reference', 'TaxRegimeMaster', 'Id', (SELECT COUNT(*) FROM skolenza."TaxRegimeMaster"));

    -- This remains empty in preview mode and is populated only after a
    -- successful actual cleanup followed by a safe sequence reset.
    CREATE TEMP TABLE institution_cleanup_sequence_reset
    (
        "TableName" text NOT NULL,
        "SequenceName" text NOT NULL
    ) ON COMMIT DROP;

    IF NOT v_execute_cleanup THEN
        RAISE NOTICE 'PREVIEW ONLY: no rows were deleted. Change ExecuteCleanup to TRUE only after reviewing institution_cleanup_preflight.';
        RETURN;
    END IF;

    -- Delete one original institution at a time. This keeps all staff-member-id filters
    -- correctly scoped while the original institution list remains fixed.
    FOR v_institution_id IN
        SELECT "Id"
        FROM pg_temp.institution_cleanup_scope
        ORDER BY "Id"
    LOOP
        TRUNCATE TABLE pg_temp.institution_cleanup_staffMember;
        INSERT INTO pg_temp.institution_cleanup_staffMember ("Id")
        SELECT "Id"
        FROM skolenza."StaffMember"
        WHERE "InstitutionId" = v_institution_id;

        -- Prevent two cleanup sessions from purging the same institution at the same time.
        PERFORM pg_advisory_xact_lock(hashtextextended('skolenza.institution.cleanup.' || v_institution_id::text, 0));

        BEGIN
    ---------------------------------------------------------------------------
    -- 1. Leaf/dependent data. Delete children before their Institution/StaffMember parents.
    ---------------------------------------------------------------------------

    DELETE FROM skolenza."ThreadMessage"
    WHERE "ThreadId" IN
          (SELECT "Id" FROM skolenza."TicketThread" WHERE "InstitutionId" = v_institution_id)
       OR "AddedById" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."TicketAttachment"
    WHERE "TicketId" IN
          (SELECT "Id" FROM skolenza."Ticket" WHERE "InstitutionId" = v_institution_id)
       OR "UploadedByUserId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."TicketHistory"
    WHERE "InstitutionId" = v_institution_id
       OR "TicketId" IN
          (SELECT "Id" FROM skolenza."Ticket" WHERE "InstitutionId" = v_institution_id)
       OR "DoneByUserId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."AssetAssignment"
    WHERE "InstitutionId" = v_institution_id
       OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
       OR "RequestId" IN
          (SELECT "Id" FROM skolenza."AssetRequest" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."AssetImage"
    WHERE "InstitutionId" = v_institution_id
       OR "AssetId" IN (SELECT "Id" FROM skolenza."Asset" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."StaffMemberExperienceDocument"
    WHERE "StaffMemberExperienceId" IN
          (SELECT "Id" FROM skolenza."StaffMemberExperience"
           WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember));

    DELETE FROM skolenza."StaffMemberWorkModeOverrideRequest"
    WHERE "InstitutionId" = v_institution_id
       OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
       OR "StaffMemberWorkArrangementId" IN
          (SELECT "Id" FROM skolenza."StaffMemberWorkArrangement"
           WHERE "InstitutionId" = v_institution_id
              OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember))
       OR "CampusId" IN
          (SELECT "Id" FROM skolenza."Campus" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."StaffMemberWorkPattern"
    WHERE "InstitutionId" = v_institution_id
       OR "StaffMemberWorkArrangementId" IN
          (SELECT "Id" FROM skolenza."StaffMemberWorkArrangement"
           WHERE "InstitutionId" = v_institution_id
              OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember))
       OR "CampusId" IN
          (SELECT "Id" FROM skolenza."Campus" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."InstitutionDeviceConfiguration"
    WHERE "InstitutionDeviceId" IN
          (SELECT "Id" FROM skolenza."InstitutionDevice" WHERE "InstitutionId" = v_institution_id);

    -- MQTT device-command replacement tables must be removed before InstitutionDevice.
    -- DeviceCredential remains encrypted at rest and is removed with its Institution device.
    DELETE FROM skolenza."DeviceCommandResponse"
    WHERE "InstitutionId" = v_institution_id
       OR "InstitutionDeviceId" IN
          (SELECT "Id" FROM skolenza."InstitutionDevice" WHERE "InstitutionId" = v_institution_id);
    DELETE FROM skolenza."DeviceMessageLog"
    WHERE "InstitutionId" = v_institution_id
       OR "InstitutionDeviceId" IN
          (SELECT "Id" FROM skolenza."InstitutionDevice" WHERE "InstitutionId" = v_institution_id);
    DELETE FROM skolenza."DeviceCommand" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."DeviceCredential"
    WHERE "InstitutionDeviceId" IN
          (SELECT "Id" FROM skolenza."InstitutionDevice" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."PayrollStaffMemberDetail"
    WHERE "PayrollStaffMemberId" IN
          (SELECT "Id" FROM skolenza."PayrollStaffMember"
           WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
              OR "PayrollRunId" IN
                 (SELECT "Id" FROM skolenza."PayrollRun" WHERE "InstitutionId" = v_institution_id));

    DELETE FROM skolenza."SalaryStructureDetail"
    WHERE "SalaryStructureId" IN
          (SELECT "Id" FROM skolenza."SalaryStructure" WHERE "InstitutionId" = v_institution_id)
       OR "ComponentId" IN
          (SELECT "Id" FROM skolenza."SalaryComponentMaster" WHERE "InstitutionId" = v_institution_id)
       OR "DependsOnComponentId" IN
          (SELECT "Id" FROM skolenza."SalaryComponentMaster" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."StaffMemberManagerMapping"
    WHERE "InstitutionId" = v_institution_id
       OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
       OR "ManagerId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
       OR "DepartmentId" IN (SELECT "Id" FROM skolenza."Department" WHERE "InstitutionId" = v_institution_id)
       OR "DesignationId" IN (SELECT "Id" FROM skolenza."Designation" WHERE "InstitutionId" = v_institution_id)
       OR "ReportingTypeId" IN (SELECT "Id" FROM skolenza."ReportingType" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."StaffMemberLocationAssignment"
    WHERE "InstitutionId" = v_institution_id
       OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
       OR "CampusId" IN
          (SELECT "Id" FROM skolenza."Campus" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."StaffMemberDeviceEnrollment"
    WHERE "InstitutionId" = v_institution_id
       OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMemberWorkArrangement"
    WHERE "InstitutionId" = v_institution_id
       OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
       OR "AttendancePolicyId" IN
          (SELECT "Id" FROM skolenza."AttendancePolicy" WHERE "InstitutionId" = v_institution_id)
       OR "PrimaryCampusId" IN
          (SELECT "Id" FROM skolenza."Campus" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."StaffMemberSalary"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
       OR "SalaryStructureId" IN
          (SELECT "Id" FROM skolenza."SalaryStructure" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."PayrollStaffMember"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
       OR "PayrollRunId" IN
          (SELECT "Id" FROM skolenza."PayrollRun" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."StaffMemberCategorySkill"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMemberContact"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMemberImage"
    WHERE "InstitutionId" = v_institution_id
       OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."UserAttendanceSetting"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."Attendance"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."AttendanceRequest"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMemberBankDetail"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMemberDailyAttendance"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMemberDependent"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMemberEducation"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMemberExperience"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMemberIdentity"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMemberPersonalDetail"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMemberStatutoryAccount"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMemberTaxProfile"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMemberWorkProfile"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."StaffMembersChangedTypeHistory"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."ForgotPasswordOTPDetail"
    WHERE "InstitutionId" = v_institution_id
       OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    DELETE FROM skolenza."LeaveRequest"
    WHERE "InstitutionId" = v_institution_id
       OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
       OR "LeaveTypeId" IN (SELECT "Id" FROM skolenza."LeaveType" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."RefreshToken"
    WHERE "LoginCredentialId" IN
          (SELECT "Id" FROM skolenza."LoginCredential"
           WHERE "InstitutionId" = v_institution_id
              OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember));

    DELETE FROM skolenza."UserRole"
    WHERE "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember)
       OR "RoleId" IN (SELECT "Id" FROM skolenza."Role" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."LoginCredential"
    WHERE "InstitutionId" = v_institution_id
       OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);

    -- Ticket has several NO ACTION StaffMember foreign keys, so it must be gone
    -- before deleting the staffMembers it can reference.
    DELETE FROM skolenza."Ticket" WHERE "InstitutionId" = v_institution_id;

    DELETE FROM skolenza."StaffMember" WHERE "InstitutionId" = v_institution_id;

    ---------------------------------------------------------------------------
    -- 2. Institution-owned organisation, leave, payroll, asset, ticket, role and module data.
    ---------------------------------------------------------------------------

    DELETE FROM skolenza."TicketThread" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."TicketType" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."TicketHeader" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."TicketClassification" WHERE "InstitutionId" = v_institution_id;

    DELETE FROM skolenza."AssetRequest"
    WHERE "InstitutionId" = v_institution_id
       OR "StaffMemberId" IN (SELECT "Id" FROM pg_temp.institution_cleanup_staffMember);
    DELETE FROM skolenza."Asset" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."AssetType" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."AssetStatus" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."AssetCategory" WHERE "InstitutionId" = v_institution_id;

    DELETE FROM skolenza."LeaveType" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."Holiday" WHERE "InstitutionId" = v_institution_id;

    DELETE FROM skolenza."PayrollRun" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."SalaryStructure" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."SalaryComponentMaster" WHERE "InstitutionId" = v_institution_id;

    DELETE FROM skolenza."AttendancePolicy" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."PolicyType" WHERE "InstitutionId" = v_institution_id;

    DELETE FROM skolenza."InstitutionDevice" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."Campus" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."Designation" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."Department" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."ReportingType" WHERE "InstitutionId" = v_institution_id;

    DELETE FROM skolenza."RoleModuleAndPermission"
    WHERE "RoleId" IN (SELECT "Id" FROM skolenza."Role" WHERE "InstitutionId" = v_institution_id)
       OR "ModuleId" IN (SELECT "Id" FROM skolenza."Module" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."HostRoleModuleAndPermission"
    WHERE "ModuleId" IN (SELECT "Id" FROM skolenza."Module" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."ModuleOperationMapping"
    WHERE "ModuleId" IN (SELECT "Id" FROM skolenza."Module" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."PlanModuleMapping"
    WHERE "ModuleId" IN (SELECT "Id" FROM skolenza."Module" WHERE "InstitutionId" = v_institution_id);

    DELETE FROM skolenza."InstitutionEnabledOperation" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."InstitutionEnabledModule" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."Role" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."Module" WHERE "InstitutionId" = v_institution_id;

    ---------------------------------------------------------------------------
    -- 3. Remaining direct InstitutionId rows, then the institution itself.
    ---------------------------------------------------------------------------
    DELETE FROM skolenza."ComplianceRule" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."StaffMemberCodePattern" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."RequestType" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."TaxRule" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."TaxSlab" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."InstitutionEncryptionKeys" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."InstitutionEmailConfig" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."InstitutionProfile" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."InstitutionSubscription" WHERE "InstitutionId" = v_institution_id;
    DELETE FROM skolenza."WorkflowStage" WHERE "InstitutionId" = v_institution_id;

    DELETE FROM skolenza."Institution" WHERE "Id" = v_institution_id;

    IF EXISTS (SELECT 1 FROM skolenza."Institution" WHERE "Id" = v_institution_id) THEN
        RAISE EXCEPTION 'Institution % could not be deleted. The transaction will roll back.', v_institution_id;
    END IF;

    RAISE NOTICE 'Institution % and its institution-scoped data have been deleted. Commit will finalize the cleanup.', v_institution_id;
        EXCEPTION
            WHEN OTHERS THEN
                RAISE EXCEPTION
                    'Institution cleanup failed for Institution Id % (SQLSTATE %): %',
                    v_institution_id,
                    SQLSTATE,
                    SQLERRM;
        END;
    END LOOP;

    ---------------------------------------------------------------------------
    -- 4. Rebuild the location-reference catalog only after every institution row
    --    has gone. The guard prevents a host/non-institution row from being removed
    --    merely because it uses CountryId, StateId or CityId.
    ---------------------------------------------------------------------------
    IF v_replace_location_reference_data THEN
        FOR v_dependency IN
            SELECT unnest(ARRAY[
                'Institution', 'StaffMember', 'Campus', 'ComplianceRule',
                'StaffMemberTaxProfile', 'SalaryComponentMaster',
                'TaxRule', 'TaxSlab'
            ]) AS table_name
        LOOP
            EXECUTE format('SELECT COUNT(*) FROM skolenza.%I', v_dependency.table_name)
            INTO v_count;

            IF v_count > 0 THEN
                RAISE EXCEPTION
                    'Location reference reset stopped: skolenza.% still has % row(s). No changes will be committed.',
                    v_dependency.table_name,
                    v_count;
            END IF;
        END LOOP;

        -- Reject new country/state/city FK tables that are not explicitly
        -- handled below. This keeps future schema changes fail-safe.
        FOR v_dependency IN
            SELECT DISTINCT source.relname AS table_name
            FROM pg_constraint fk
            JOIN pg_class target ON target.oid = fk.confrelid
            JOIN pg_namespace target_schema ON target_schema.oid = target.relnamespace
            JOIN pg_class source ON source.oid = fk.conrelid
            JOIN pg_namespace source_schema ON source_schema.oid = source.relnamespace
            WHERE fk.contype = 'f'
              AND target_schema.nspname = 'skolenza'
              AND source_schema.nspname = 'skolenza'
              AND target.relname IN ('Country', 'State', 'City')
              AND source.relname NOT IN
              (
                  'City', 'State', 'District', 'CountryIdentityRule',
                  'CountryStatutoryRule', 'StatutoryType', 'TaxSystemMaster',
                  'TaxRegimeMaster', 'Campus', 'ComplianceRule',
                  'StaffMember', 'StaffMemberTaxProfile',
                  'SalaryComponentMaster', 'TaxRule', 'TaxSlab'
              )
        LOOP
            EXECUTE format('SELECT COUNT(*) FROM skolenza.%I', v_dependency.table_name)
            INTO v_count;

            IF v_count > 0 THEN
                RAISE EXCEPTION
                    'Location reference reset stopped: unhandled Country/State/City dependency skolenza.% has % row(s).',
                    v_dependency.table_name,
                    v_count;
            END IF;
        END LOOP;

        DELETE FROM skolenza."TaxRegimeMaster";
        DELETE FROM skolenza."CountryStatutoryRule";
        DELETE FROM skolenza."CountryIdentityRule";
        DELETE FROM skolenza."District";
        DELETE FROM skolenza."City";
        DELETE FROM skolenza."TaxSystemMaster";
        DELETE FROM skolenza."StatutoryType";
        DELETE FROM skolenza."State";
        DELETE FROM skolenza."Country";

        FOREACH v_reference_table IN ARRAY ARRAY[
            'Country', 'State', 'City', 'District', 'CountryIdentityRule',
            'CountryStatutoryRule', 'StatutoryType', 'TaxSystemMaster',
            'TaxRegimeMaster'
        ]
        LOOP
            SELECT pg_get_serial_sequence(format('skolenza.%I', v_reference_table), 'Id')
            INTO v_reference_sequence;

            IF v_reference_sequence IS NOT NULL THEN
                EXECUTE format('ALTER SEQUENCE %s RESTART WITH 1', v_reference_sequence);
            END IF;
        END LOOP;

        INSERT INTO skolenza."Country" ("CountryName", "CountryCode", "STDCode", "IsActive")
        VALUES
            ('India', 'IN', '+91', TRUE),
            ('China', 'CN', '+86', TRUE);

        INSERT INTO skolenza."State" ("CountryId", "StateName", "IsActive")
        SELECT country."Id", seed."StateName", TRUE
        FROM pg_temp.location_reference_seed seed
        JOIN skolenza."Country" country ON country."CountryCode" = seed."CountryCode"
        ORDER BY seed."SortOrder";

        INSERT INTO skolenza."City" ("StateId", "CityName", "IsActive")
        SELECT state."Id", seed."CityName", TRUE
        FROM pg_temp.location_reference_seed seed
        JOIN skolenza."Country" country ON country."CountryCode" = seed."CountryCode"
        JOIN skolenza."State" state
          ON state."CountryId" = country."Id"
         AND state."StateName" = seed."StateName"
        ORDER BY seed."SortOrder";

        IF (SELECT COUNT(*) FROM skolenza."Country") <> 2
           OR (SELECT COUNT(*) FROM skolenza."State") <> (SELECT COUNT(*) FROM pg_temp.location_reference_seed)
           OR (SELECT COUNT(*) FROM skolenza."City") <> (SELECT COUNT(*) FROM pg_temp.location_reference_seed) THEN
            RAISE EXCEPTION 'Location reference seed validation failed. No changes will be committed.';
        END IF;
    ELSE
        -- Safe production path: do not remove reference data that can be used
        -- by host/global compliance configuration. Ensure the India/China
        -- catalog exists and is active, adding only missing rows.
        -- A prior manual reset can leave an identity sequence behind existing
        -- rows. Synchronize it before any seed insert to prevent PK collisions.
        FOREACH v_reference_table IN ARRAY ARRAY['Country', 'State', 'City']
        LOOP
            SELECT pg_get_serial_sequence(format('skolenza.%I', v_reference_table), 'Id')
            INTO v_reference_sequence;

            IF v_reference_sequence IS NOT NULL THEN
                EXECUTE format(
                    'SELECT setval(%L::regclass, COALESCE(MAX("Id"), 1), COUNT(*) > 0) FROM skolenza.%I',
                    v_reference_sequence,
                    v_reference_table);
            END IF;
        END LOOP;

        UPDATE skolenza."Country"
        SET "CountryName" = CASE "CountryCode"
                                WHEN 'IN' THEN 'India'
                                WHEN 'CN' THEN 'China'
                            END,
            "STDCode" = CASE "CountryCode"
                            WHEN 'IN' THEN '+91'
                            WHEN 'CN' THEN '+86'
                         END,
            "IsActive" = TRUE
        WHERE "CountryCode" IN ('IN', 'CN');

        INSERT INTO skolenza."Country" ("CountryName", "CountryCode", "STDCode", "IsActive")
        SELECT source."CountryName", source."CountryCode", source."STDCode", TRUE
        FROM
        (
            VALUES
                ('India'::character varying(100), 'IN'::character varying(10), '+91'::character varying(10)),
                ('China'::character varying(100), 'CN'::character varying(10), '+86'::character varying(10))
        ) AS source("CountryName", "CountryCode", "STDCode")
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM skolenza."Country" country
            WHERE country."CountryCode" = source."CountryCode"
        );

        UPDATE skolenza."State" state
        SET "IsActive" = TRUE
        FROM pg_temp.location_reference_seed seed
        JOIN skolenza."Country" country ON country."CountryCode" = seed."CountryCode"
        WHERE state."CountryId" = country."Id"
          AND state."StateName" = seed."StateName";

        INSERT INTO skolenza."State" ("CountryId", "StateName", "IsActive")
        SELECT country."Id", seed."StateName", TRUE
        FROM pg_temp.location_reference_seed seed
        JOIN skolenza."Country" country ON country."CountryCode" = seed."CountryCode"
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM skolenza."State" state
            WHERE state."CountryId" = country."Id"
              AND state."StateName" = seed."StateName"
        )
        ORDER BY seed."SortOrder";

        UPDATE skolenza."City" city
        SET "IsActive" = TRUE
        FROM pg_temp.location_reference_seed seed
        JOIN skolenza."Country" country ON country."CountryCode" = seed."CountryCode"
        JOIN skolenza."State" state
          ON state."CountryId" = country."Id"
         AND state."StateName" = seed."StateName"
        WHERE city."StateId" = state."Id"
          AND city."CityName" = seed."CityName";

        INSERT INTO skolenza."City" ("StateId", "CityName", "IsActive")
        SELECT state."Id", seed."CityName", TRUE
        FROM pg_temp.location_reference_seed seed
        JOIN skolenza."Country" country ON country."CountryCode" = seed."CountryCode"
        JOIN skolenza."State" state
          ON state."CountryId" = country."Id"
         AND state."StateName" = seed."StateName"
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM skolenza."City" city
            WHERE city."StateId" = state."Id"
              AND city."CityName" = seed."CityName"
        )
        ORDER BY seed."SortOrder";
    END IF;

    ---------------------------------------------------------------------------
    -- 5. Reset auto-generated IDs for empty institution-dependent tables.
    --    Shared/host tables that still contain records are skipped so their
    --    next IDs cannot collide with existing primary keys.
    ---------------------------------------------------------------------------
    FOR v_sequence IN
        WITH RECURSIVE institution_roots(table_oid) AS
        (
            SELECT 'skolenza."Institution"'::regclass::oid
            UNION
            SELECT c.oid
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_attribute a ON a.attrelid = c.oid
            WHERE n.nspname = 'skolenza'
              AND c.relkind IN ('r', 'p')
              AND a.attnum > 0
              AND NOT a.attisdropped
              AND a.attname IN ('InstitutionId', 'StaffMemberId')
        ),
        institution_dependencies(table_oid) AS
        (
            SELECT table_oid FROM institution_roots
            UNION
            SELECT fk.conrelid
            FROM pg_constraint fk
            JOIN institution_dependencies parent ON parent.table_oid = fk.confrelid
            JOIN pg_class child ON child.oid = fk.conrelid
            JOIN pg_namespace child_schema ON child_schema.oid = child.relnamespace
            WHERE fk.contype = 'f'
              AND child_schema.nspname = 'skolenza'
        )
        SELECT DISTINCT
            tbl.relname AS table_name,
            seq_ns.nspname AS sequence_schema,
            seq.relname AS sequence_name
        FROM pg_class seq
        JOIN pg_namespace seq_ns ON seq_ns.oid = seq.relnamespace
        JOIN pg_depend dep ON dep.objid = seq.oid AND dep.deptype IN ('a', 'i')
        JOIN pg_class tbl ON tbl.oid = dep.refobjid
        JOIN institution_dependencies td ON td.table_oid = tbl.oid
        WHERE seq.relkind = 'S'
          AND seq_ns.nspname = 'skolenza'
        ORDER BY tbl.relname, seq.relname
    LOOP
        EXECUTE format('LOCK TABLE skolenza.%I IN ACCESS EXCLUSIVE MODE', v_sequence.table_name);
        EXECUTE format('SELECT COUNT(*) FROM skolenza.%I', v_sequence.table_name)
        INTO v_count;

        IF v_count = 0 THEN
            EXECUTE format(
                'ALTER SEQUENCE %I.%I RESTART WITH 1',
                v_sequence.sequence_schema,
                v_sequence.sequence_name);

            INSERT INTO pg_temp.institution_cleanup_sequence_reset ("TableName", "SequenceName")
            VALUES (v_sequence.table_name, v_sequence.sequence_name);
        END IF;
    END LOOP;

    RAISE NOTICE '% institution(s) and their institution-scoped data have been deleted. Commit will finalize the cleanup.', v_institution_count;
END;
$$;

-- Always review this result. With ExecuteCleanup = FALSE, it is the only data output.
SELECT "Scope", "TableName", "MatchColumn", "RecordCount"
FROM pg_temp.institution_cleanup_preflight
WHERE "RecordCount" > 0
ORDER BY "Scope", "TableName", "MatchColumn";

-- Filled only when ExecuteCleanup = TRUE and the cleanup succeeds.
SELECT "TableName", "SequenceName"
FROM pg_temp.institution_cleanup_sequence_reset
ORDER BY "TableName", "SequenceName";

COMMIT;
