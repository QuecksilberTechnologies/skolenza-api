BEGIN;

ALTER TABLE skolenza."StaffMemberBankDetail"
    ALTER COLUMN "AccountNumber" TYPE varchar(128),
    ALTER COLUMN "IFSCCode" TYPE varchar(128),
    ALTER COLUMN "UPIId" TYPE varchar(512);

COMMIT;
