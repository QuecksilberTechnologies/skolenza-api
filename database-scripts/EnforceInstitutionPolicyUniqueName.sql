-- Prevent two active policy identities in one institution from using the same
-- trimmed, case-insensitive business name. Versions remain unique per PolicyId.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM skolenza."Policy"
        WHERE NOT "IsSoftDeleted"
        GROUP BY "InstitutionId", lower(btrim("PolicyName"))
        HAVING count(*) > 1
    ) THEN
        RAISE EXCEPTION
            'Duplicate institution policy names exist. Reconcile the duplicate policy identities before applying UX_Policy_Institution_NormalizedName.';
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Policy_Institution_NormalizedName"
    ON skolenza."Policy" ("InstitutionId", lower(btrim("PolicyName")))
    WHERE NOT "IsSoftDeleted";
