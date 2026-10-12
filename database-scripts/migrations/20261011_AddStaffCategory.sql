BEGIN;
DO $$
DECLARE catalogue_already_exists boolean := to_regclass('skolenza."StaffCategory"') IS NOT NULL;
BEGIN
 CREATE TABLE IF NOT EXISTS skolenza."StaffCategory" (
  "Id" smallint PRIMARY KEY, "Code" varchar(50) NOT NULL UNIQUE, "Name" varchar(100) NOT NULL,
  "Description" varchar(500), "DisplayOrder" smallint NOT NULL, "IsActive" boolean NOT NULL DEFAULT true,
  "IsSystemDefined" boolean NOT NULL DEFAULT true, "AddedDateTime" timestamptz NOT NULL DEFAULT now(), "UpdatedDateTime" timestamptz);
 IF to_regclass('skolenza."StaffMember"') IS NULL THEN RAISE EXCEPTION 'Required table skolenza.StaffMember does not exist.'; END IF;
 ALTER TABLE skolenza."StaffMember" ADD COLUMN IF NOT EXISTS "StaffCategoryId" smallint;
 IF NOT catalogue_already_exists THEN
  UPDATE skolenza."StaffMember" SET "StaffCategoryId" = CASE "StaffCategoryId"
   WHEN 1 THEN 2 WHEN 2 THEN 3 WHEN 3 THEN 5 WHEN 4 THEN 1 WHEN 5 THEN 4 ELSE 1 END;
 END IF;
END $$;
INSERT INTO skolenza."StaffCategory" ("Id","Code","Name","Description","DisplayOrder","IsActive","IsSystemDefined") VALUES
 (1,'ADMINISTRATIVE','Administrative','Institution leadership, administration and office management staff.',1,true,true),
 (2,'TEACHING','Teaching','Staff members assigned to teaching and learning responsibilities.',2,true,true),
 (3,'NON_TEACHING','Non-Teaching','Staff members serving non-teaching institutional functions.',3,true,true),
 (4,'SUPPORT','Support','Staff members providing operational and campus support services.',4,true,true),
 (5,'SPECIALIST','Specialist','Staff members providing specialist professional or technical expertise.',5,true,true)
ON CONFLICT ("Id") DO UPDATE SET "Code"=EXCLUDED."Code","Name"=EXCLUDED."Name","Description"=EXCLUDED."Description",
 "DisplayOrder"=EXCLUDED."DisplayOrder","IsActive"=EXCLUDED."IsActive","IsSystemDefined"=EXCLUDED."IsSystemDefined","UpdatedDateTime"=now();
ALTER TABLE skolenza."StaffMember" DROP CONSTRAINT IF EXISTS "CK_Employee_StaffCategoryId";
ALTER TABLE skolenza."StaffMember" DROP CONSTRAINT IF EXISTS "CK_StaffMember_StaffCategoryId";
ALTER TABLE skolenza."StaffMember" ADD CONSTRAINT "CK_StaffMember_StaffCategoryId" CHECK ("StaffCategoryId" IS NULL OR "StaffCategoryId" BETWEEN 1 AND 5);
ALTER TABLE skolenza."StaffMember" DROP CONSTRAINT IF EXISTS "FK_StaffMember_StaffCategory";
ALTER TABLE skolenza."StaffMember" ADD CONSTRAINT "FK_StaffMember_StaffCategory" FOREIGN KEY ("StaffCategoryId") REFERENCES skolenza."StaffCategory"("Id") ON DELETE RESTRICT;
CREATE INDEX IF NOT EXISTS "IX_StaffMember_StaffCategoryId" ON skolenza."StaffMember"("StaffCategoryId");
COMMENT ON COLUMN skolenza."StaffMember"."StaffCategoryId" IS 'Central Staff Category FK: 1 Administrative, 2 Teaching, 3 Non-Teaching, 4 Support, 5 Specialist.';
DO $$ BEGIN
 IF (SELECT count(*) FROM skolenza."StaffCategory" WHERE "IsActive") <> 5 THEN RAISE EXCEPTION 'StaffCategory seed verification failed.'; END IF;
 IF EXISTS (SELECT 1 FROM skolenza."StaffMember" m LEFT JOIN skolenza."StaffCategory" c ON c."Id"=m."StaffCategoryId" WHERE m."StaffCategoryId" IS NOT NULL AND c."Id" IS NULL) THEN RAISE EXCEPTION 'StaffMember contains an unmapped StaffCategoryId.'; END IF;
END $$;
COMMIT;
