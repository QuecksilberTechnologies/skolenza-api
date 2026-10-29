namespace skolenza.domain.Entity;

/// <summary>Student-owned master records use 64-bit identifiers and the institution master audit convention.</summary>
public abstract class StudentInstitutionMasterEntity
{
    public long Id { get; set; }
    public long? InstitutionId { get; set; }
    public long? TemplateSourceId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystemDefined { get; set; }
    public long? AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? DeletedDateTime { get; set; }
}

/// <summary>Institution-configured admission evidence required for a campus and academic session.</summary>
public sealed class StudentDocumentRequirementMaster : StudentInstitutionMasterEntity
{
    public long? CampusId { get; set; }
    public long? AcademicSessionId { get; set; }
    public short AppliesTo { get; set; }
    public bool IsRequired { get; set; }
    public string AllowedMimeTypesJson { get; set; } = "[]";
    public long MaxFileSizeBytes { get; set; }
    public bool RequiresExpiryDate { get; set; }
}

public sealed class StudentFeeFrequencyMaster : StudentInstitutionMasterEntity
{
    public short InstallmentsPerYear { get; set; }
}

public sealed class StudentFeeComponentMaster : StudentInstitutionMasterEntity
{
    public short ChargeNature { get; set; }
}

public sealed class StudentBenefitSchemeMaster : StudentInstitutionMasterEntity
{
    public int? CountryId { get; set; }
    public int? StateId { get; set; }
    public long? CampusId { get; set; }
    public long? AcademicSessionId { get; set; }
    public decimal MinimumDiscountPercent { get; set; }
    public decimal MaximumDiscountPercent { get; set; }
    public string? EligibilityDescription { get; set; }
    public long? EvidenceRequirementId { get; set; }
}

/// <summary>Permanent institution-owned learner identity, independent from academic-year placement.</summary>
public sealed class Student
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string? AdmissionNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public int GenderId { get; set; }
    public int CountryId { get; set; }
    public short LifecycleStatus { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Admission application and review lifecycle, before permanent enrollment is created.</summary>
public sealed class StudentAdmission
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public long AcademicSessionId { get; set; }
    public long CampusId { get; set; }
    public long AcademicGroupId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public int GenderId { get; set; }
    public int CountryId { get; set; }
    public short Status { get; set; }
    public string? ReviewReason { get; set; }
    public long? StudentId { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Append-only admission status transition and reviewer audit.</summary>
public sealed class StudentAdmissionStatusHistory
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentAdmissionId { get; set; }
    public short FromStatus { get; set; }
    public short ToStatus { get; set; }
    public string? Reason { get; set; }
    public long ChangedById { get; set; }
    public DateTime ChangedDateTime { get; set; }
}

/// <summary>Guardian relationship captured for an admission and materialized as a Student link on approval.</summary>
public sealed class StudentAdmissionGuardian
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentAdmissionId { get; set; }
    public long GuardianId { get; set; }
    public short RelationshipType { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsEmergencyContact { get; set; }
    public bool IsPickupAuthorized { get; set; }
    public bool HasPortalAccess { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Academic placement for one learner and one session.</summary>
public sealed class StudentEnrollment
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentId { get; set; }
    public long AcademicSessionId { get; set; }
    public long CampusId { get; set; }
    public long AcademicGroupId { get; set; }
    public long? AcademicProgrammeId { get; set; }
    public string? RollNumber { get; set; }
    public short Status { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Guardian person record shared across linked learners within an institution.</summary>
public sealed class Guardian
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Effective-dated student-to-guardian relationship and parent-portal scope.</summary>
public sealed class StudentGuardian
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentId { get; set; }
    public long GuardianId { get; set; }
    public short RelationshipType { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsEmergencyContact { get; set; }
    public bool IsPickupAuthorized { get; set; }
    public bool HasPortalAccess { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
}

/// <summary>Append-only event created when a learner leaves an institution.</summary>
public sealed class StudentExitHistory
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentId { get; set; }
    public long? StudentEnrollmentId { get; set; }
    public string? PreviousSchoolName { get; set; }
    public int? PreviousSchoolCountryId { get; set; }
    public string? DestinationSchoolName { get; set; }
    public int? DestinationSchoolCountryId { get; set; }
    public DateOnly EffectiveExitDate { get; set; }
    public short ExitReason { get; set; }
    public string? Notes { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
}

/// <summary>Institution catalogue of country and authority-issued learner identifiers.</summary>
public sealed class StudentEducationIdentifierMaster : StudentInstitutionMasterEntity
{
    public int CountryId { get; set; }
    public string IssuingAuthority { get; set; } = string.Empty;
}

/// <summary>Verified institution portal hostname used to resolve portal login tenant context.</summary>
public sealed class InstitutionPortalDomainMaster
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public string ChallengeValueHash { get; set; } = string.Empty;
    public DateTime ChallengeExpiresAt { get; set; }
    public short VerificationStatus { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? DeletedDateTime { get; set; }
}

/// <summary>Institution-scoped credential for a Guardian or optional Student portal persona.</summary>
public sealed class StudentPortalAccount
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public short FailedLoginCount { get; set; }
    public DateTime? LockoutEndDateTime { get; set; }
    public DateTime? LastLoginDateTime { get; set; }
    public string SecurityStamp { get; set; } = string.Empty;
    public DateTime AddedDateTime { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Explicit portal account-to-Guardian identity link; StudentGuardian scopes linked children.</summary>
public sealed class StudentPortalAccountGuardian
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentPortalAccountId { get; set; }
    public long GuardianId { get; set; }
    public DateTime LinkedDateTime { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Explicit optional portal account-to-Student persona link.</summary>
public sealed class StudentPortalAccountStudent
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentPortalAccountId { get; set; }
    public long StudentId { get; set; }
    public DateTime LinkedDateTime { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Opaque refresh token for a Student portal session, stored only as a SHA-256 hash.</summary>
public sealed class StudentPortalRefreshToken
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentPortalAccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedDateTime { get; set; }
    public DateTime ExpiresDateTime { get; set; }
    public DateTime? RevokedDateTime { get; set; }
    public string? ReplacedByTokenHash { get; set; }
}

/// <summary>Single-use invitation or password-reset token, stored only as a hash.</summary>
public sealed class StudentPortalOneTimeToken
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long? StudentPortalAccountId { get; set; }
    public long? GuardianId { get; set; }
    public long? StudentId { get; set; }
    public short Purpose { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedDateTime { get; set; }
    public DateTime ExpiresDateTime { get; set; }
    public DateTime? ConsumedDateTime { get; set; }
}

/// <summary>Encrypted external identifier issued to a student by a country or authority.</summary>
public sealed class StudentEducationIdentifier
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentId { get; set; }
    public long IdentifierMasterId { get; set; }
    public int CountryId { get; set; }
    public string EncryptedValue { get; set; } = string.Empty;
    public string MaskedValue { get; set; } = string.Empty;
    public short VerificationStatus { get; set; }
    public string? IssuerReference { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? VerifiedById { get; set; }
    public DateTime? VerifiedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Append-only review history for externally issued Student education identifiers.</summary>
public sealed class StudentEducationIdentifierVerificationHistory
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentEducationIdentifierId { get; set; }
    public short FromStatus { get; set; }
    public short ToStatus { get; set; }
    public string? Reason { get; set; }
    public long ChangedById { get; set; }
    public DateTime ChangedDateTime { get; set; }
}

/// <summary>A retained, versioned file supplied for an admission and reviewed by institution staff.</summary>
public sealed class StudentAdmissionDocument
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentAdmissionId { get; set; }
    public long RequirementId { get; set; }
    public long? GuardianId { get; set; }
    public short PersonScope { get; set; }
    public int VersionNumber { get; set; }
    public long? SupersedesDocumentId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StorageProvider { get; set; } = "S3";
    public string ObjectKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ChecksumSha256 { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public short Status { get; set; }
    public long SubmittedById { get; set; }
    public DateTime SubmittedDateTime { get; set; }
    public long? ReviewedById { get; set; }
    public DateTime? ReviewedDateTime { get; set; }
    public string? ReviewReason { get; set; }
}

/// <summary>Append-only audit history for an admission document review decision.</summary>
public sealed class StudentAdmissionDocumentReviewHistory
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentAdmissionDocumentId { get; set; }
    public short FromStatus { get; set; }
    public short ToStatus { get; set; }
    public string? Reason { get; set; }
    public long ChangedById { get; set; }
    public DateTime ChangedDateTime { get; set; }
}

/// <summary>Versioned document supplied by an authenticated Student or Guardian after enrollment.</summary>
public sealed class StudentRecordDocument
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentId { get; set; }
    public long RequirementId { get; set; }
    public long? GuardianId { get; set; }
    public long SubmittedByPortalAccountId { get; set; }
    public short PersonScope { get; set; }
    public int VersionNumber { get; set; }
    public long? SupersedesDocumentId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StorageProvider { get; set; } = "S3";
    public string ObjectKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ChecksumSha256 { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public short Status { get; set; }
    public DateTime SubmittedDateTime { get; set; }
    public long? ReviewedByStaffMemberId { get; set; }
    public DateTime? ReviewedDateTime { get; set; }
    public string? ReviewReason { get; set; }
}

/// <summary>Append-only staff review history for a post-admission Student document.</summary>
public sealed class StudentRecordDocumentReviewHistory
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentRecordDocumentId { get; set; }
    public short FromStatus { get; set; }
    public short ToStatus { get; set; }
    public string? Reason { get; set; }
    public long? ChangedByStaffMemberId { get; set; }
    public long? ChangedByPortalAccountId { get; set; }
    public DateTime ChangedDateTime { get; set; }
}

/// <summary>Host-managed ISO currency and official settlement currency for one country.</summary>
public sealed class CountryCurrencyMaster
{
    public long Id { get; set; }
    public int CountryId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public short DecimalPlaces { get; set; } = 2;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public long AddedByHostUserId { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedByHostUserId { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Campus/session/group tuition and admission charge configuration.</summary>
public sealed class StudentFeeStructure
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long CampusId { get; set; }
    public long AcademicSessionId { get; set; }
    public long AcademicGroupId { get; set; }
    public long CountryCurrencyMasterId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public short CurrencyDecimalPlaces { get; set; } = 2;
    public decimal LateFeePerInstallment { get; set; }
    public short GracePeriodDays { get; set; }
    public decimal? LateFeeCap { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Fee component amount and one-time charge allocation within a structure.</summary>
public sealed class StudentFeeStructureItem
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentFeeStructureId { get; set; }
    public long FeeComponentMasterId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public short ChargeNature { get; set; }
    public decimal Amount { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>An allowed payment frequency and its explicitly configured due dates.</summary>
public sealed class StudentFeeStructureFrequency
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentFeeStructureId { get; set; }
    public long FeeFrequencyMasterId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StudentFeeStructureInstallmentDueDate
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentFeeStructureFrequencyId { get; set; }
    public short InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
}

/// <summary>Immutable admission quote snapshot of fees, discount, currency and schedule.</summary>
public sealed class StudentAdmissionFeeQuote
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentAdmissionId { get; set; }
    public long StudentFeeStructureId { get; set; }
    public long StudentFeeStructureFrequencyId { get; set; }
    public long? StudentBenefitSchemeMasterId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public short CurrencyDecimalPlaces { get; set; } = 2;
    public decimal DiscountPercent { get; set; }
    public decimal GrossRecurringAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal OneTimeAmount { get; set; }
    public decimal NetAnnualAmount { get; set; }
    public short InstallmentsPerYear { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public long CreatedById { get; set; }
}

public sealed class StudentAdmissionFeeQuoteLine
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentAdmissionFeeQuoteId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public short ChargeNature { get; set; }
    public decimal AnnualAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
}

public sealed class StudentFeeInstallment
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentAdmissionFeeQuoteId { get; set; }
    public short InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal RecurringAmount { get; set; }
    public decimal OneTimeAmount { get; set; }
    public decimal LateFeeAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public short Status { get; set; }
}

public sealed class StudentFeePaymentAttempt
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentFeeInstallmentId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string GatewayCode { get; set; } = string.Empty;
    public string GatewayOrderId { get; set; } = string.Empty;
    public string? GatewayPaymentSessionId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public short Status { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime? ExpiresDateTime { get; set; }
}

public sealed class StudentPaymentGatewayConfiguration
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public string GatewayCode { get; set; } = string.Empty;
    public short Environment { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ProtectedClientSecret { get; set; } = string.Empty;
    public string ProtectedWebhookSecret { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public long CreatedById { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public sealed class StudentFeePaymentTransaction
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentFeeInstallmentId { get; set; }
    public long? StudentFeePaymentAttemptId { get; set; }
    public string? GatewayPaymentId { get; set; }
    public string? IdempotencyKey { get; set; }
    public short Method { get; set; }
    public short Status { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? BankReference { get; set; }
    public string? RecordedReference { get; set; }
    public long? RecordedById { get; set; }
    public DateTime? PaidDateTime { get; set; }
    public DateTime AddedDateTime { get; set; }
}

public sealed class StudentFeePaymentWebhookEvent
{
    public long Id { get; set; }
    public long StudentFeePaymentAttemptId { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadSha256 { get; set; } = string.Empty;
    public short ProcessingStatus { get; set; }
    public DateTime ReceivedDateTime { get; set; }
    public DateTime? ProcessedDateTime { get; set; }
}

public sealed class StudentFeeReceipt
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StudentFeePaymentTransactionId { get; set; }
    public long StudentAdmissionFeeQuoteId { get; set; }
    public long StudentFeeInstallmentId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime IssuedDateTime { get; set; }
}
