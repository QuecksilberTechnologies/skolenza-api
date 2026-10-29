using System.ComponentModel.DataAnnotations;
using skolenza.application.Common.Enums;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.Student;

public sealed class CountryCurrencyMasterFilterDTO : PermissionPagedRequestDTO
{
    public CountryCurrencyMasterFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public int? CountryId { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public sealed class CountryCurrencyMasterRequestDTO : PermissionRequestDTO
{
    [Range(1, int.MaxValue)]
    public int CountryId { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string CurrencyCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string CurrencyName { get; set; } = string.Empty;

    [Required, StringLength(12)]
    public string CurrencySymbol { get; set; } = string.Empty;
    [Range(0, 3)]
    public short DecimalPlaces { get; set; } = 2;

    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

public sealed class CountryCurrencyMasterResponseDTO
{
    public long Id { get; set; }
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public short DecimalPlaces { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

public sealed class StudentFeeStructureFilterDTO : PermissionPagedRequestDTO
{
    public StudentFeeStructureFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public long? CampusId { get; set; }
    public long? AcademicSessionId { get; set; }
    public long? AcademicGroupId { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public sealed class StudentFeeStructureRequestDTO : PermissionRequestDTO
{
    [Range(1, long.MaxValue)]
    public long CampusId { get; set; }

    [Range(1, long.MaxValue)]
    public long AcademicSessionId { get; set; }

    [Range(1, long.MaxValue)]
    public long AcademicGroupId { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal LateFeePerInstallment { get; set; }

    [Range(0, 90)]
    public short GracePeriodDays { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal? LateFeeCap { get; set; }

    [MinLength(1)]
    public List<StudentFeeStructureItemRequestDTO> Items { get; set; } = [];

    [MinLength(1)]
    public List<StudentFeeStructureFrequencyRequestDTO> Frequencies { get; set; } = [];

    public bool IsActive { get; set; } = true;
}

public sealed class StudentFeeStructureItemRequestDTO
{
    [Range(1, long.MaxValue)]
    public long FeeComponentMasterId { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal Amount { get; set; }
}

public sealed class StudentFeeStructureFrequencyRequestDTO
{
    [Range(1, long.MaxValue)]
    public long FeeFrequencyMasterId { get; set; }

    [MinLength(1)]
    public List<StudentFeeStructureDueDateRequestDTO> Installments { get; set; } = [];
}

public sealed class StudentFeeStructureDueDateRequestDTO
{
    [Range(1, 12)]
    public short InstallmentNumber { get; set; }

    public DateOnly DueDate { get; set; }
}

public sealed class StudentFeeStructureResponseDTO
{
    public long Id { get; set; }
    public long CampusId { get; set; }
    public string CampusName { get; set; } = string.Empty;
    public long AcademicSessionId { get; set; }
    public string AcademicSessionName { get; set; } = string.Empty;
    public long AcademicGroupId { get; set; }
    public string AcademicGroupName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public short CurrencyDecimalPlaces { get; set; }
    public decimal LateFeePerInstallment { get; set; }
    public short GracePeriodDays { get; set; }
    public decimal? LateFeeCap { get; set; }
    public bool IsActive { get; set; }
    public List<StudentFeeStructureItemResponseDTO> Items { get; set; } = [];
    public List<StudentFeeStructureFrequencyResponseDTO> Frequencies { get; set; } = [];
}

public sealed class StudentFeeStructureItemResponseDTO
{
    public long FeeComponentMasterId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public short ChargeNature { get; set; }
    public decimal Amount { get; set; }
}

public sealed class StudentFeeStructureFrequencyResponseDTO
{
    public long Id { get; set; }
    public long FeeFrequencyMasterId { get; set; }
    public string FrequencyCode { get; set; } = string.Empty;
    public string FrequencyName { get; set; } = string.Empty;
    public short InstallmentsPerYear { get; set; }
    public List<StudentFeeStructureDueDateResponseDTO> Installments { get; set; } = [];
}

public sealed class StudentFeeStructureDueDateResponseDTO
{
    public short InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
}

public sealed class StudentAdmissionFeeQuoteRequestDTO : PermissionRequestDTO
{
    [Range(1, long.MaxValue)]
    public long StudentFeeStructureId { get; set; }

    [Range(1, long.MaxValue)]
    public long StudentFeeStructureFrequencyId { get; set; }

    public long? StudentBenefitSchemeMasterId { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal DiscountPercent { get; set; }
}

public sealed class StudentAdmissionFeeQuoteResponseDTO
{
    public long Id { get; set; }
    public long StudentAdmissionId { get; set; }
    public long StudentFeeStructureId { get; set; }
    public long StudentFeeStructureFrequencyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public short CurrencyDecimalPlaces { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal GrossRecurringAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal OneTimeAmount { get; set; }
    public decimal NetAnnualAmount { get; set; }
    public short InstallmentsPerYear { get; set; }
    public List<StudentAdmissionFeeQuoteLineResponseDTO> Lines { get; set; } = [];
    public List<StudentFeeInstallmentResponseDTO> Installments { get; set; } = [];
}

public sealed class StudentAdmissionFeeQuoteLineResponseDTO
{
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public short ChargeNature { get; set; }
    public decimal AnnualAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
}

public sealed class StudentFeeInstallmentResponseDTO
{
    public long Id { get; set; }
    public long QuoteId { get; set; }
    public short InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal RecurringAmount { get; set; }
    public decimal OneTimeAmount { get; set; }
    public decimal LateFeeAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public short Status { get; set; }
}

public sealed class StudentOfflinePaymentRequestDTO : PermissionRequestDTO
{
    [Required, StringLength(100)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Amount { get; set; }

    [StringLength(150)]
    public string? RecordedReference { get; set; }

    public DateTimeOffset PaidAt { get; set; }
}

public sealed class StudentFeePaymentResponseDTO
{
    public long Id { get; set; }
    public long StudentFeeInstallmentId { get; set; }
    public short Method { get; set; }
    public short Status { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? GatewayOrderId { get; set; }
    public string? GatewayPaymentId { get; set; }
    public string? ReceiptNumber { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}

public sealed class StudentCashfreeCheckoutRequestDTO : PermissionRequestDTO
{
    [Required, StringLength(100)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class StudentCashfreeCheckoutResponseDTO
{
    public long PaymentAttemptId { get; set; }
    public string GatewayOrderId { get; set; } = string.Empty;
    public string PaymentSessionId { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class StudentCashfreePaymentStatusResponseDTO
{
    public long PaymentAttemptId { get; set; }
    public string GatewayOrderId { get; set; } = string.Empty;
    public short AttemptStatus { get; set; }
    public long? PaymentTransactionId { get; set; }
    public short? PaymentStatus { get; set; }
    public string? GatewayPaymentId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReceiptNumber { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}

public sealed class StudentCashfreeCheckoutDataDTO
{
    public long PaymentAttemptId { get; set; }
    public long InstitutionId { get; set; }
    public long StudentFeeInstallmentId { get; set; }
    public string GatewayOrderId { get; set; } = string.Empty;
    public string? PaymentSessionId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public short CurrencyDecimalPlaces { get; set; }
    public decimal Amount { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
}

public sealed class StudentCashfreePaymentDetailsDTO
{
    public string OrderId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? BankReference { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}

public sealed class StudentPaymentGatewayConfigurationRequestDTO : PermissionRequestDTO
{
    [EnumDataType(typeof(StudentPaymentGatewayEnvironment))]
    public StudentPaymentGatewayEnvironment Environment { get; set; }

    [Required, StringLength(250)]
    public string ClientId { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 1)]
    public string ClientSecret { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 1)]
    public string WebhookSecret { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    [Range(1, int.MaxValue)]
    public int Version { get; set; } = 1;
}

public sealed class StudentPaymentGatewayConfigurationFilterDTO : PermissionRequestDTO
{
}

public sealed class StudentPaymentGatewayConfigurationResponseDTO
{
    public string GatewayCode { get; set; } = string.Empty;
    public StudentPaymentGatewayEnvironment Environment { get; set; }
    public string ClientIdMasked { get; set; } = string.Empty;
    public bool HasClientSecret { get; set; }
    public bool HasWebhookSecret { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class StudentFeePaymentStatusRequestDTO : PermissionRequestDTO
{
}

public sealed class StudentFeeQuoteListRequestDTO : PermissionPagedRequestDTO
{
    public StudentFeeQuoteListRequestDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }
}

public sealed class StudentFeeReceiptListRequestDTO : PermissionPagedRequestDTO
{
    public StudentFeeReceiptListRequestDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public long? StudentAdmissionId { get; set; }
    public long? StudentFeeInstallmentId { get; set; }
}

public sealed class StudentFeeReceiptResponseDTO
{
    public long Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public long StudentAdmissionId { get; set; }
    public long StudentFeeInstallmentId { get; set; }
    public long PaymentTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; set; }
    public short PaymentMethod { get; set; }
    public string? RecordedReference { get; set; }
}

public sealed class StudentAdmissionFeeBalanceRequestDTO : PermissionRequestDTO
{
}

public sealed class StudentAdmissionFeeBalanceResponseDTO
{
    public long StudentAdmissionId { get; set; }
    public long? StudentId { get; set; }
    public long QuoteId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public short CurrencyDecimalPlaces { get; set; }
    public decimal NetAnnualAmount { get; set; }
    public decimal TotalLateFeeAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalOutstandingAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public List<StudentFeeInstallmentResponseDTO> Installments { get; set; } = [];
}
