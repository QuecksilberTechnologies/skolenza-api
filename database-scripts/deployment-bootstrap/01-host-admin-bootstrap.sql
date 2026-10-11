-- Skolenza Host login bootstrap extracted from the approved AxionPro canonical seed.
-- Scope: HostRole and the two canonical HostUser records only.
-- Subscription plans and Host permissions are intentionally excluded.
-- Source: AxionPro production-seed/04-access-and-host/001-modules-operations-two-host-admins.sql

BEGIN;

DO
$$
BEGIN
    IF EXISTS (SELECT 1 FROM skolenza."HostRole")
       OR EXISTS (SELECT 1 FROM skolenza."HostUser") THEN
        RAISE EXCEPTION 'Host bootstrap requires empty HostRole and HostUser tables.';
    END IF;
END;
$$;

INSERT INTO skolenza."HostRole"
(
    "Id", "Name", "Description", "IsActive", "AddedById", "AddedDateTime",
    "UpdatedById", "UpdatedDateTime", "DeletedById", "DeletedDateTime", "IsSoftDeleted"
)
VALUES
(
    1,
    'Host-Super-Admin',
    'Full Host-scope administration through persisted module-operation permissions.',
    TRUE,
    1,
    CURRENT_TIMESTAMP,
    1,
    CURRENT_TIMESTAMP,
    NULL,
    NULL,
    FALSE
);

-- Both canonical Host Admin accounts use the ASP.NET Core Identity PasswordHasher V3
-- hash below for the initial password 12344321. Change it after first login.
INSERT INTO skolenza."HostUser"
(
    "Id", "HostRoleId", "Name", "LoginId", "PasswordHash", "Email", "MobileNumber",
    "IsActive", "IsSoftDeleted", "AddedById", "AddedDateTime", "UpdatedById",
    "UpdatedDateTime", "DeletedById", "DeletedDateTime"
)
VALUES
(
    1,
    1,
    'Deepesh Gupta',
    'mca.deepesh@gmail.com',
    'AQAAAAIAAYagAAAAEEDpT6tXHxx4OhhP394Aqp4vlsVunbyd3qQGOnszn4oghxYFlkERmuDjy0ATNqawgw==',
    'mca.deepesh@gmail.com',
    '9111161399',
    TRUE,
    FALSE,
    1,
    CURRENT_TIMESTAMP,
    1,
    CURRENT_TIMESTAMP,
    NULL,
    NULL
),
(
    2,
    1,
    'Sujeet',
    'sujeet@skolenza.com',
    'AQAAAAIAAYagAAAAEEDpT6tXHxx4OhhP394Aqp4vlsVunbyd3qQGOnszn4oghxYFlkERmuDjy0ATNqawgw==',
    'sujeet@skolenza.com',
    NULL,
    TRUE,
    FALSE,
    1,
    CURRENT_TIMESTAMP,
    1,
    CURRENT_TIMESTAMP,
    NULL,
    NULL
);


SELECT setval(pg_get_serial_sequence('skolenza."HostRole"', 'Id'), (SELECT MAX("Id") FROM skolenza."HostRole"), TRUE);
SELECT setval(pg_get_serial_sequence('skolenza."HostUser"', 'Id'), (SELECT MAX("Id") FROM skolenza."HostUser"), TRUE);

COMMIT;

