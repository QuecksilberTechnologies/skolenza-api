-- Central host template used by StudentPortalAuthRepository. This seed inserts
-- only when the code does not exist, and never edits an administrator's template.
-- Sender and SMTP settings continue to come from the existing email configuration.

BEGIN;

DO $$
DECLARE
    seed_actor_id bigint;
BEGIN
    PERFORM pg_advisory_xact_lock(hashtext('skolenza.student_portal_invitation_template'));

    IF EXISTS (
        SELECT 1 FROM skolenza."EmailTemplate"
        WHERE upper("TemplateCode") = 'STUDENT_PORTAL_INVITATION'
    ) THEN
        RETURN;
    END IF;

    -- Reuse the audit actor from the existing host-managed active template seed.
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
        ('Guardian Portal Invitation', 'STUDENT_PORTAL_INVITATION',
         'Activate your guardian portal account',
         '<p>Dear {{GuardianName}},</p><p>You have been invited to access your school guardian portal.</p><p><a href="{{InvitationUrl}}">Activate your account</a></p><p>This invitation expires at {{ExpiresAtUtc}} (UTC). Please do not share the link.</p><p>If you were not expecting this invitation, contact your school.</p>',
         'System', 'en', true, seed_actor_id, CURRENT_TIMESTAMP);
END $$;

COMMIT;
