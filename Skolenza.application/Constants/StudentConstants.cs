namespace skolenza.application.Constants;

public static class StudentConstants
{
    public const string AdmissionsModuleCode = "STUDENT_ADMISSIONS";
    public const string FeesModuleCode = "STUDENT_FEES";
    public const string BenefitSchemesModuleCode = "STUDENT_ADMISSIONS";
    public const string PortalDomainsModuleCode = "STUDENT_PORTAL_DOMAINS";
    public const string PortalDomainTxtRecordPrefix = "_skolenza-verification";
    public const string PortalDomainChallengePrefix = "skolenza-portal-domain-verification=";
    public const int PortalDomainChallengeExpiryHours = 24;
    public const string PortalAuthenticationPolicy = "StudentPortal";
    public const string PortalUserType = "StudentPortal";
    public const string PortalAccountIdClaim = "StudentPortalAccountId";
    public const int PortalRefreshTokenExpiryDays = 14;
    public const int PortalOneTimeTokenExpiryMinutes = 30;
    public const short PortalLoginFailureLimit = 5;
    public const int PortalLoginLockoutMinutes = 15;
    public const int PortalPasswordMinimumLength = 12;
    public const int PortalPasswordMaximumLength = 128;
    public const int PortalTokenHashLength = 64;
    public const string PortalInvitationTemplateCode = "STUDENT_PORTAL_INVITATION";
    public const string PortalPasswordResetTemplateCode = "STUDENT_PORTAL_PASSWORD_RESET";
    public const string PortalEmailProtectedPayloadKey = "_skolenzaProtectedPortalPayload";
    public const string PortalInvitationPath = "/auth/portal/accept-invitation";
    public const string PortalPasswordResetPath = "/auth/portal/reset-password";
    public const string PortalPasswordResetGenericMessage = "If an active portal account exists for this email, password reset instructions will be sent.";
    public const string PortalPasswordResetConcurrencyMessage = "The password reset conflicted with another request. Request a new reset link and try again.";
    public const int FeeTransactionMaximumAttempts = 2;
    public const string FeeTransactionConcurrencyMessage = "The fee transaction conflicted with another request. Please retry with the same idempotency key.";
    public const long PortalDocumentAbsoluteMaximumFileSizeBytes = 104_857_600;
    public const string AdmissionApplicationNumberPrefix = "APP";
    public const string StudentCodePrefix = "STD";
    public const string CashfreeGatewayCode = "CASHFREE";
    public const string CashfreeApiVersion = "2026-01-01";
    public const int EducationIdentifierMaximumLength = 80;
    public const int EducationIdentifierVisibleSuffixLength = 4;
    public const char EducationIdentifierMaskCharacter = '*';
}
