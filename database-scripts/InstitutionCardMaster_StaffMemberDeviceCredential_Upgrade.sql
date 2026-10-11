-- Institution card inventory and server-owned staffMember-device credentials.
-- Apply before deploying the matching API version. The script preserves all
-- legacy enrollment data and deliberately does not copy card numbers forward.

ALTER TABLE "StaffMemberDeviceEnrollment" ADD COLUMN IF NOT EXISTS "CampusId" bigint;
UPDATE "StaffMemberDeviceEnrollment" e
SET "CampusId" = d."CampusId"
FROM "InstitutionDevice" d
WHERE e."InstitutionDeviceId" = d."Id" AND e."CampusId" IS NULL;
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM "StaffMemberDeviceEnrollment" WHERE "CampusId" IS NULL) THEN
        RAISE EXCEPTION 'StaffMemberDeviceEnrollment contains a device with no CampusId; correct it before migration.';
    END IF;
END $$;
ALTER TABLE "StaffMemberDeviceEnrollment" ALTER COLUMN "CampusId" SET NOT NULL;
ALTER TABLE "StaffMemberDeviceEnrollment" ADD COLUMN IF NOT EXISTS "InstitutionCardMasterId" bigint NULL;
ALTER TABLE "StaffMemberDeviceEnrollment" ADD COLUMN IF NOT EXISTS "FaceImageHash" varchar(128) NULL;
ALTER TABLE "StaffMemberDeviceEnrollment" ADD COLUMN IF NOT EXISTS "FaceDeploymentStatus" smallint NOT NULL DEFAULT 0;
ALTER TABLE "StaffMemberDeviceEnrollment" ADD COLUMN IF NOT EXISTS "CardDeploymentStatus" smallint NOT NULL DEFAULT 0;
ALTER TABLE "StaffMemberDeviceEnrollment" ADD COLUMN IF NOT EXISTS "PinDeploymentStatus" smallint NOT NULL DEFAULT 0;
ALTER TABLE "StaffMemberDeviceEnrollment" ADD COLUMN IF NOT EXISTS "FaceDeviceCommandId" bigint NULL;
ALTER TABLE "StaffMemberDeviceEnrollment" ADD COLUMN IF NOT EXISTS "CardDeviceCommandId" bigint NULL;
ALTER TABLE "StaffMemberDeviceEnrollment" ADD COLUMN IF NOT EXISTS "PinDeviceCommandId" bigint NULL;
ALTER TABLE "StaffMemberDeviceEnrollment" ADD COLUMN IF NOT EXISTS "UserActivationDeviceCommandId" bigint NULL;
ALTER TABLE "StaffMemberDeviceEnrollment" ADD COLUMN IF NOT EXISTS "AccessEffectiveFromDateTime" timestamp NULL;
ALTER TABLE "StaffMemberDeviceEnrollment" ADD COLUMN IF NOT EXISTS "AccessEffectiveToDateTime" timestamp NULL;

CREATE TABLE IF NOT EXISTS "InstitutionCardMaster" (
  "Id" bigserial PRIMARY KEY, "InstitutionId" bigint NOT NULL,
  "CardNumberEncrypted" varchar(1024) NOT NULL, "CardNumberLookupHash" varchar(128) NOT NULL,
  "CardReference" varchar(100), "CardStatus" smallint NOT NULL DEFAULT 1,
  "PurchaseCurrencyCode" varchar(3) NOT NULL DEFAULT 'INR', "UnitPurchasePriceExcludingTax" numeric(18,2) NOT NULL,
  "SupplierCountryId" integer, "SupplierStateId" integer, "PlaceOfSupplyCountryId" integer, "PlaceOfSupplyStateId" integer,
  "SupplierName" varchar(200), "SupplierTaxRegistrationNumber" varchar(100), "PurchaseInvoiceNumber" varchar(100), "PurchaseInvoiceDate" date,
  "TaxTreatment" smallint NOT NULL, "CgstRate" numeric(9,4) NOT NULL DEFAULT 0, "CgstAmount" numeric(18,2) NOT NULL DEFAULT 0,
  "SgstRate" numeric(9,4) NOT NULL DEFAULT 0, "SgstAmount" numeric(18,2) NOT NULL DEFAULT 0,
  "IgstRate" numeric(9,4) NOT NULL DEFAULT 0, "IgstAmount" numeric(18,2) NOT NULL DEFAULT 0,
  "ForeignTaxLabel" varchar(100), "ForeignTaxRate" numeric(9,4) NOT NULL DEFAULT 0, "ForeignTaxAmount" numeric(18,2) NOT NULL DEFAULT 0,
  "CustomsDutyAmount" numeric(18,2) NOT NULL DEFAULT 0, "FreightAmount" numeric(18,2) NOT NULL DEFAULT 0, "LandedCost" numeric(18,2) NOT NULL,
  "IsActive" boolean NOT NULL DEFAULT true, "IsSoftDeleted" boolean NOT NULL DEFAULT false,
  "AddedById" bigint NOT NULL, "AddedDateTime" timestamp NOT NULL, "UpdatedById" bigint, "UpdatedDateTime" timestamp, "SoftDeletedById" bigint, "SoftDeletedDateTime" timestamp,
  CONSTRAINT "FK_InstitutionCardMaster_Institution" FOREIGN KEY ("InstitutionId") REFERENCES "Institution"("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS "UX_InstitutionCardMaster_Institution_CardHash_Live" ON "InstitutionCardMaster" ("InstitutionId", "CardNumberLookupHash") WHERE NOT "IsSoftDeleted";
CREATE INDEX IF NOT EXISTS "IX_StaffMemberDeviceEnrollment_CampusId" ON "StaffMemberDeviceEnrollment" ("CampusId");
CREATE INDEX IF NOT EXISTS "IX_StaffMemberDeviceEnrollment_InstitutionCardMasterId" ON "StaffMemberDeviceEnrollment" ("InstitutionCardMasterId");
CREATE INDEX IF NOT EXISTS "IX_StaffMemberDeviceEnrollment_FaceDeviceCommandId" ON "StaffMemberDeviceEnrollment" ("FaceDeviceCommandId");
CREATE INDEX IF NOT EXISTS "IX_StaffMemberDeviceEnrollment_CardDeviceCommandId" ON "StaffMemberDeviceEnrollment" ("CardDeviceCommandId");
CREATE INDEX IF NOT EXISTS "IX_StaffMemberDeviceEnrollment_PinDeviceCommandId" ON "StaffMemberDeviceEnrollment" ("PinDeviceCommandId");
CREATE INDEX IF NOT EXISTS "IX_StaffMemberDeviceEnrollment_UserActivationDeviceCommandId" ON "StaffMemberDeviceEnrollment" ("UserActivationDeviceCommandId");
ALTER TABLE "StaffMemberDeviceEnrollment" ADD CONSTRAINT "FK_StaffMemberDeviceEnrollment_Campus" FOREIGN KEY ("CampusId") REFERENCES "Campus"("Id") ON DELETE RESTRICT;
ALTER TABLE "StaffMemberDeviceEnrollment" ADD CONSTRAINT "FK_StaffMemberDeviceEnrollment_InstitutionCardMaster" FOREIGN KEY ("InstitutionCardMasterId") REFERENCES "InstitutionCardMaster"("Id") ON DELETE RESTRICT;

CREATE TABLE IF NOT EXISTS "StaffMemberDeviceAccessWindow" (
  "Id" bigserial PRIMARY KEY, "StaffMemberDeviceEnrollmentId" bigint NOT NULL, "DayOfWeek" smallint NOT NULL,
  "StartLocalTime" time NOT NULL, "EndLocalTime" time NOT NULL, "IsActive" boolean NOT NULL DEFAULT true, "IsSoftDeleted" boolean NOT NULL DEFAULT false,
  "AddedById" bigint NOT NULL, "AddedDateTime" timestamp NOT NULL, "UpdatedById" bigint, "UpdatedDateTime" timestamp, "SoftDeletedById" bigint, "SoftDeletedDateTime" timestamp,
  CONSTRAINT "FK_StaffMemberDeviceAccessWindow_Enrollment" FOREIGN KEY ("StaffMemberDeviceEnrollmentId") REFERENCES "StaffMemberDeviceEnrollment"("Id") ON DELETE CASCADE,
  CONSTRAINT "CK_StaffMemberDeviceAccessWindow_TimeRange" CHECK ("StartLocalTime" < "EndLocalTime")
);
CREATE INDEX IF NOT EXISTS "IX_StaffMemberDeviceAccessWindow_Enrollment" ON "StaffMemberDeviceAccessWindow" ("StaffMemberDeviceEnrollmentId");

-- Host Institution RFID Management parent and its Card Inventory child module.
-- The parent is navigation-only; every CRUD mapping remains attached to the
-- Card Inventory child. The platform Super Admin role is id 1; additional Host
-- roles must be granted these mappings through the normal Host-role permission screen.
INSERT INTO skolenza."Module" ("InstitutionId","ModuleCode","ModuleName","DisplayName","URLPath","ParentModuleId","IsLeafNode","IsModuleDisplayInUI","IsCommonMenu","IsActive","ImageIconWeb","ImageIconMobile","ItemPriority","Remark","AddedById","AddedDateTime","ModuleScope")
SELECT NULL,'HOST_INSTITUTION_RFID_MANAGEMENT','Host-Institution-RFID-Management','Institution RFID Management',NULL,NULL,FALSE,TRUE,FALSE,TRUE,'bi bi-broadcast-pin','contactless',525,'Host management of Institution RFID cards and card inventory.',1,CURRENT_TIMESTAMP,2
WHERE NOT EXISTS (SELECT 1 FROM skolenza."Module" WHERE "ModuleCode"='HOST_INSTITUTION_RFID_MANAGEMENT');

UPDATE skolenza."Module" parent
SET "ModuleName"='Host-Institution-RFID-Management', "DisplayName"='Institution RFID Management', "URLPath"=NULL,
    "ParentModuleId"=NULL, "IsLeafNode"=FALSE, "IsModuleDisplayInUI"=TRUE, "IsCommonMenu"=FALSE,
    "IsActive"=TRUE, "ImageIconWeb"='bi bi-broadcast-pin', "ImageIconMobile"='contactless',
    "ItemPriority"=525, "Remark"='Host management of institution RFID cards and card inventory.',
    "UpdatedById"=1, "UpdatedDateTime"=CURRENT_TIMESTAMP, "ModuleScope"=2
WHERE parent."ModuleCode"='HOST_INSTITUTION_RFID_MANAGEMENT';

INSERT INTO skolenza."Module" ("InstitutionId","ModuleCode","ModuleName","DisplayName","URLPath","ParentModuleId","IsLeafNode","IsModuleDisplayInUI","IsCommonMenu","IsActive","ImageIconWeb","ImageIconMobile","ItemPriority","Remark","AddedById","AddedDateTime","ModuleScope")
SELECT NULL,'HOST_INSTITUTION_CARD_INVENTORY','Host-Institution-Card-Inventory','Card Inventory','/app/institution-card-inventory',parent."Id",TRUE,TRUE,FALSE,TRUE,'bi bi-credit-card','credit-card',530,'Host-only procurement and lifecycle inventory for Institution-issued physical cards.',1,CURRENT_TIMESTAMP,2
FROM skolenza."Module" parent
WHERE parent."ModuleCode"='HOST_INSTITUTION_RFID_MANAGEMENT'
  AND NOT EXISTS (SELECT 1 FROM skolenza."Module" WHERE "ModuleCode"='HOST_INSTITUTION_CARD_INVENTORY');

UPDATE skolenza."Module" child
SET "ParentModuleId"=parent."Id", "IsLeafNode"=TRUE, "IsModuleDisplayInUI"=TRUE, "IsActive"=TRUE,
    "UpdatedById"=1, "UpdatedDateTime"=CURRENT_TIMESTAMP, "ModuleScope"=2
FROM skolenza."Module" parent
WHERE child."ModuleCode"='HOST_INSTITUTION_CARD_INVENTORY'
  AND parent."ModuleCode"='HOST_INSTITUTION_RFID_MANAGEMENT';

INSERT INTO skolenza."ModuleOperationMapping" ("ModuleId","OperationId","PageURL","IconURL","IsCommonItem","IsOperational","Priority","Remark","IsActive","AddedById","AddedDateTime")
SELECT m."Id",o."Id",m."URLPath",m."ImageIconWeb",FALSE,TRUE,CASE lower(o."OperationName") WHEN 'view' THEN 10 WHEN 'create' THEN 20 WHEN 'update' THEN 30 WHEN 'delete' THEN 40 ELSE 99 END,'Card inventory permission.',TRUE,1,CURRENT_TIMESTAMP
FROM skolenza."Module" m CROSS JOIN skolenza."Operation" o
WHERE m."ModuleCode"='HOST_INSTITUTION_CARD_INVENTORY' AND lower(btrim(o."OperationName")) IN ('view','create','update','delete') AND o."IsActive"=TRUE
AND NOT EXISTS (SELECT 1 FROM skolenza."ModuleOperationMapping" x WHERE x."ModuleId"=m."Id" AND x."OperationId"=o."Id");

INSERT INTO skolenza."HostRoleModuleAndPermission" ("HostRoleId","ModuleId","OperationId","IsActive","IsSoftDeleted","AddedById","AddedDateTime")
SELECT 1,m."Id",o."Id",TRUE,FALSE,1,CURRENT_TIMESTAMP
FROM skolenza."Module" m CROSS JOIN skolenza."Operation" o
WHERE m."ModuleCode"='HOST_INSTITUTION_CARD_INVENTORY' AND lower(btrim(o."OperationName")) IN ('view','create','update','delete') AND o."IsActive"=TRUE
AND NOT EXISTS (SELECT 1 FROM skolenza."HostRoleModuleAndPermission" x WHERE x."HostRoleId"=1 AND x."ModuleId"=m."Id" AND x."OperationId"=o."Id" AND NOT x."IsSoftDeleted");
