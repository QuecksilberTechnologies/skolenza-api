namespace skolenza.application.Common.Enums;

public enum StudentDocumentPersonScope : short
{
    Student = 1,
    Guardian = 2,
    StudentAndGuardian = 3
}

public enum StudentDocumentReviewStatus : short
{
    AwaitingReview = 1,
    Approved = 2,
    Rejected = 3,
    ReturnedForCorrection = 4
}

public enum StudentFeeComponentChargeNature : short
{
    RecurringAnnual = 1,
    OneTime = 2
}

public enum StudentFeeInstallmentStatus : short
{
    Due = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5
}

public enum StudentFeePaymentMethod : short
{
    Offline = 1,
    Cashfree = 2
}

public enum StudentPaymentGatewayEnvironment : short
{
    Sandbox = 1,
    Production = 2
}

public enum StudentFeePaymentStatus : short
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Expired = 4,
    Refunded = 5
}

public enum StudentFeePaymentAttemptStatus : short
{
    Created = 1,
    Pending = 2,
    Succeeded = 3,
    Failed = 4,
    Expired = 5
}

public enum StudentFeeWebhookProcessingStatus : short
{
    Pending = 1,
    Processed = 2,
    Failed = 3,
    Ignored = 4
}

public enum StudentAdmissionStatus : short
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    Approved = 4,
    Rejected = 5,
    ReturnedForCorrection = 6,
    Cancelled = 7
}

public enum StudentLifecycleStatus : short
{
    Active = 1,
    Exited = 2
}

public enum StudentEnrollmentStatus : short
{
    Draft = 1,
    Active = 2,
    Completed = 3,
    Withdrawn = 4,
    Transferred = 5,
    Cancelled = 6
}

public enum StudentEducationIdentifierVerificationStatus : short
{
    Unverified = 1,
    Verified = 2,
    Rejected = 3
}

public enum StudentExitReason : short
{
    Transfer = 1,
    Graduation = 2,
    Withdrawal = 3,
    Other = 4
}

public enum StudentGuardianRelationshipType : short
{
    Father = 1,
    Mother = 2,
    Guardian = 3,
    Grandparent = 4,
    Other = 5
}

public enum StudentPortalDomainVerificationStatus : short
{
    Pending = 1,
    Verified = 2
}

public enum StudentPortalPersonaType : short
{
    Guardian = 1,
    Student = 2
}

public enum StudentPortalOneTimeTokenPurpose : short
{
    Invitation = 1,
    PasswordReset = 2
}
