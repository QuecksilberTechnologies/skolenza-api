-- Insert the central Student portal reset template without replacing an existing
-- administrator-managed template. Mail configuration controls the sender.

BEGIN;

DO $$
DECLARE
    seed_actor_id bigint;
BEGIN
    PERFORM pg_advisory_xact_lock(hashtext('skolenza.student_portal_password_reset_template'));

    IF EXISTS (
        SELECT 1 FROM skolenza."EmailTemplate"
        WHERE upper("TemplateCode") = 'STUDENT_PORTAL_PASSWORD_RESET'
    ) THEN
        RETURN;
    END IF;

    SELECT "AddedById" INTO seed_actor_id
    FROM skolenza."EmailTemplate"
    WHERE "IsActive" AND "TemplateCode" = 'WELCOME_EMAIL'
    ORDER BY "AddedDateTime" DESC, "Id" DESC
    LIMIT 1;

    IF seed_actor_id IS NULL THEN
        RAISE EXCEPTION 'An active host WELCOME_EMAIL template is required to identify the existing seed actor.';
    END IF;

    INSERT INTO skolenza."EmailTemplate"
        ("TemplateName", "TemplateCode", "Subject", "Body", "Category", "LanguageCode",
         "IsActive", "AddedById", "AddedDateTime")
    VALUES
        ('Student Portal Password Reset', 'STUDENT_PORTAL_PASSWORD_RESET',
         'Reset your portal password',
         '<p>Hello,</p><p>A password reset was requested for your school portal account.</p><p><a href="{{PasswordResetUrl}}">Reset your password</a></p><p>This link expires at {{ExpiresAtUtc}} (UTC). Please do not share it.</p><p>If you did not request this, you can ignore this message.</p>',
         'System', 'en', true, seed_actor_id, CURRENT_TIMESTAMP);
END $$;

COMMIT;
