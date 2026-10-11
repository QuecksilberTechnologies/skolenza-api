BEGIN;

ALTER TABLE skolenza."AttendanceDeviceType"
    ADD COLUMN IF NOT EXISTS "DeviceTypeCode" character varying(30);

INSERT INTO skolenza."AttendanceDeviceType"
    ("DeviceTypeCode", "DeviceType", "Remark", "IsActive", "IsDeviceRegister", "AddedById", "AddedDateTime")
SELECT seed."Code", seed."Name", seed."Remark", TRUE, seed."RequiresRegistration", 1, CURRENT_TIMESTAMP
FROM (VALUES
    ('MOBILE', 'Mobile', 'Attendance marked from an authenticated mobile client.', FALSE),
    ('WEB', 'Web', 'Attendance marked from an authenticated web client.', FALSE),
    ('BIOMETRIC', 'Biometric Device', 'Attendance received from a registered physical attendance device.', TRUE),
    ('MANUAL', 'Manual Entry', 'Attendance entered by an authorized administrator.', FALSE)
) AS seed("Code", "Name", "Remark", "RequiresRegistration")
WHERE NOT EXISTS (
    SELECT 1 FROM skolenza."AttendanceDeviceType" existing
    WHERE upper(btrim(existing."DeviceTypeCode")) = seed."Code");

ALTER TABLE skolenza."AttendanceDeviceType"
    ALTER COLUMN "DeviceTypeCode" SET NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS "UX_AttendanceDeviceType_DeviceTypeCode"
    ON skolenza."AttendanceDeviceType" (upper(btrim("DeviceTypeCode")));

ALTER TABLE skolenza."StaffMemberAttendancePunch"
    ADD COLUMN IF NOT EXISTS "AttendanceDeviceTypeId" integer;

DO $channel_migration$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'skolenza'
          AND table_name = 'StaffMemberAttendancePunch'
          AND column_name = 'Channel') THEN
        EXECUTE $sql$
            UPDATE skolenza."StaffMemberAttendancePunch" punch
            SET "AttendanceDeviceTypeId" = device_type."Id"
            FROM skolenza."AttendanceDeviceType" device_type
            WHERE punch."AttendanceDeviceTypeId" IS NULL
              AND upper(btrim(device_type."DeviceTypeCode")) = CASE punch."Channel"
                  WHEN 1 THEN 'MOBILE'
                  WHEN 2 THEN 'WEB'
                  WHEN 3 THEN 'BIOMETRIC'
                  WHEN 4 THEN 'MANUAL'
              END
        $sql$;
    END IF;
END $channel_migration$;

DO $validation$
BEGIN
    IF EXISTS (SELECT 1 FROM skolenza."StaffMemberAttendancePunch" WHERE "AttendanceDeviceTypeId" IS NULL) THEN
        RAISE EXCEPTION 'StaffMemberAttendancePunch contains an unmapped attendance channel.';
    END IF;
END $validation$;

ALTER TABLE skolenza."StaffMemberAttendancePunch"
    ALTER COLUMN "AttendanceDeviceTypeId" SET NOT NULL;
ALTER TABLE skolenza."StaffMemberAttendancePunch"
    DROP CONSTRAINT IF EXISTS "CK_StaffMemberAttendancePunch_Channel";
ALTER TABLE skolenza."StaffMemberAttendancePunch"
    DROP COLUMN IF EXISTS "Channel";
ALTER TABLE skolenza."StaffMemberAttendancePunch"
    DROP CONSTRAINT IF EXISTS "FK_StaffMemberAttendancePunch_AttendanceDeviceType";
ALTER TABLE skolenza."StaffMemberAttendancePunch"
    ADD CONSTRAINT "FK_StaffMemberAttendancePunch_AttendanceDeviceType"
    FOREIGN KEY ("AttendanceDeviceTypeId") REFERENCES skolenza."AttendanceDeviceType" ("Id") ON DELETE RESTRICT;

COMMIT;
