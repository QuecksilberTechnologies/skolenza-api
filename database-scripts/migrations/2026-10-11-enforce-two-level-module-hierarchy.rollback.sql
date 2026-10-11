BEGIN;

DROP TRIGGER IF EXISTS "TR_Module_TwoLevelHierarchy" ON skolenza."Module";
DROP FUNCTION IF EXISTS skolenza."ValidateModuleTwoLevelHierarchy"();

-- This rollback restores the former parent links for rows that still exist.
-- The three duplicate leaf identities merged by the forward migration are not
-- recreated because doing so would split live permission history arbitrarily.
-- Restore a pre-migration schema/data backup if those duplicate identities are
-- specifically required.
WITH links(child_code, parent_code) AS (
    VALUES
    ('COMMON_MY_PROFILE','COMMON_PROFILE'),
    ('COMMON_SIGN_OUT','COMMON_PROFILE'),
    ('COMMON_SETTINGS','COMMON_PROFILE'),
    ('COMMON_HELP_SUPPORT','COMMON_PROFILE'),
    ('COMMON_MESSAGE','COMMON_NOTIFICATIONS'),
    ('COMMON_NOTIFICATION','COMMON_NOTIFICATIONS'),
    ('HOST_BILLING_CONFIGURATION','HOST_SUBSCRIPTIONS'),
    ('HOST_BILLING_PLAN_PRICES','HOST_SUBSCRIPTIONS'),
    ('HOST_BILLING_TAX_RULES','HOST_SUBSCRIPTIONS'),
    ('HOST_BILLING_TRANSACTIONS','HOST_SUBSCRIPTIONS'),
    ('HOST_BILLING_REFUNDS','HOST_SUBSCRIPTIONS'),
    ('HOST_BILLING_RECONCILIATION','HOST_SUBSCRIPTIONS'),
    ('HOST_BILLING_AUDIT','HOST_SUBSCRIPTIONS'),
    ('INSTITUTION_EMAIL_TEMPLATE','INSTITUTION_EMAIL_CONFIG')
)
UPDATE skolenza."Module" child
SET "ParentModuleId" = parent."Id",
    "UpdatedById" = 1,
    "UpdatedDateTime" = CURRENT_TIMESTAMP
FROM links
JOIN skolenza."Module" parent ON upper(btrim(parent."ModuleCode")) = links.parent_code
WHERE upper(btrim(child."ModuleCode")) = links.child_code;

UPDATE skolenza."Module" module
SET "IsLeafNode" = NOT EXISTS
    (SELECT 1 FROM skolenza."Module" child WHERE child."ParentModuleId" = module."Id"),
    "UpdatedById" = 1,
    "UpdatedDateTime" = CURRENT_TIMESTAMP;

COMMIT;
