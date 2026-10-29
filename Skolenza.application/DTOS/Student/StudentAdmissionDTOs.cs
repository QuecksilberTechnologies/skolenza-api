using skolenza.application.Common.Enums;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.Student;

public sealed class StudentAdmissionFilterDTO : PermissionPagedRequestDTO
{
    public StudentAdmissionFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public long? AcademicSessionId { get; set; }
    public long? CampusId { get; set; }
    public short? Status { get; set; }
}

public sealed class StudentAdmissionStatusHistoryFilterDTO : PermissionPagedRequestDTO
{
    public StudentAdmissionStatusHistoryFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }
}

public sealed class StudentAdmissionRequestDTO : PermissionRequestDTO
{
    public long AcademicSessionId { get; set; }
    public long CampusId { get; set; }
    public long AcademicGroupId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public int GenderId { get; set; }
    public int CountryId { get; set; }
}

public sealed class StudentAdmissionReviewDTO : PermissionRequestDTO
{
    public StudentAdmissionStatus Status { get; set; }
    public string? Reason { get; set; }
}

public sealed class StudentAdmissionResponseDTO
{
    public long Id { get; set; }
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
    public StudentAdmissionStatus Status { get; set; }
    public string? ReviewReason { get; set; }
    public long? StudentId { get; set; }
    public string? StudentCode { get; set; }
}

public sealed class StudentAdmissionStatusHistoryResponseDTO
{
    public long Id { get; set; }
    public long AdmissionId { get; set; }
    public StudentAdmissionStatus? FromStatus { get; set; }
    public StudentAdmissionStatus ToStatus { get; set; }
    public string? Reason { get; set; }
    public long ChangedById { get; set; }
    public DateTime ChangedDateTime { get; set; }
}

public sealed class StudentExitRequestDTO : PermissionRequestDTO
{
    public DateOnly EffectiveExitDate { get; set; }
    public StudentExitReason ExitReason { get; set; }
    public string? PreviousSchoolName { get; set; }
    public int? PreviousSchoolCountryId { get; set; }
    public string? DestinationSchoolName { get; set; }
    public int? DestinationSchoolCountryId { get; set; }
    public string? Notes { get; set; }
}

public sealed class StudentExitResponseDTO
{
    public long StudentId { get; set; }
    public long ExitHistoryId { get; set; }
    public DateOnly EffectiveExitDate { get; set; }
    public StudentExitReason ExitReason { get; set; }
    public string StudentCode { get; set; } = string.Empty;
}

public sealed class StudentRecordFilterDTO : PermissionPagedRequestDTO
{
    public StudentRecordFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public long? AcademicSessionId { get; set; }
    public long? CampusId { get; set; }
    public long? AcademicGroupId { get; set; }
    public bool IncludeExited { get; set; }
}

public sealed class StudentRecordResponseDTO
{
    public long Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string? AdmissionNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public short LifecycleStatus { get; set; }
    public long? CurrentEnrollmentId { get; set; }
    public long? AcademicSessionId { get; set; }
    public long? CampusId { get; set; }
    public long? AcademicGroupId { get; set; }
}

public sealed class StudentExitHistoryFilterDTO : PermissionPagedRequestDTO
{
    public StudentExitHistoryFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }
}

public sealed class StudentExitHistoryResponseDTO
{
    public long Id { get; set; }
    public long? StudentEnrollmentId { get; set; }
    public string? PreviousSchoolName { get; set; }
    public int? PreviousSchoolCountryId { get; set; }
    public string? DestinationSchoolName { get; set; }
    public int? DestinationSchoolCountryId { get; set; }
    public DateOnly EffectiveExitDate { get; set; }
    public StudentExitReason ExitReason { get; set; }
    public string? Notes { get; set; }
    public DateTime AddedDateTime { get; set; }
}

public sealed class StudentOptionsResponseDTO
{
    public IReadOnlyList<StudentOptionValueDTO> AdmissionStatuses { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> LifecycleStatuses { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> EnrollmentStatuses { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> DocumentPersonScopes { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> DocumentReviewStatuses { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> FeeChargeNatures { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> FeeInstallmentStatuses { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> FeePaymentMethods { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> FeePaymentStatuses { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> FeePaymentAttemptStatuses { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> FeeWebhookProcessingStatuses { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> EducationIdentifierVerificationStatuses { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> ExitReasons { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> GuardianRelationships { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> PaymentGatewayEnvironments { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> PortalDomainVerificationStatuses { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> PortalPersonaTypes { get; init; } = [];
    public IReadOnlyList<StudentOptionValueDTO> PortalOneTimeTokenPurposes { get; init; } = [];
}

public sealed class StudentOptionsRequestDTO : PermissionRequestDTO
{
}

public sealed class StudentOptionValueDTO
{
    public short Value { get; init; }
    public string Name { get; init; } = string.Empty;
}
