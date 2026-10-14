using skolenza.application.DTOs.BaseDTO;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace skolenza.application.DTOS.Staff;

public class StaffOperationsPermissionRequestDTO : PermissionRequestDTO;

public sealed class StaffOperationsOptionDTO
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class StaffOperationsOptionsResponseDTO
{
    public List<StaffOperationsOptionDTO> StaffMembers { get; set; } = [];
    public List<StaffOperationsOptionDTO> Campuses { get; set; } = [];
    public List<StaffOperationsOptionDTO> Departments { get; set; } = [];
    public List<StaffOperationsOptionDTO> StaffCategories { get; set; } = [];
    public List<StaffOperationsOptionDTO> AccessRoles { get; set; } = [];
    public List<StaffOperationsOptionDTO> StaffPositions { get; set; } = [];
    public List<StaffOperationsOptionDTO> ApprovedLeaveRequests { get; set; } = [];
}

public sealed class TimetablePeriodRequestDTO : StaffOperationsPermissionRequestDTO
{
    public long? Id { get; set; }
    public long AcademicSessionId { get; set; }
    public long? CampusId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Sequence { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TimetablePeriodResponseDTO
{
    public long Id { get; set; }
    public long AcademicSessionId { get; set; }
    public string AcademicSessionName { get; set; } = string.Empty;
    public long? CampusId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Sequence { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; }
}

public class TimetableEntryRequestDTO : StaffOperationsPermissionRequestDTO
{
    public long? Id { get; set; }
    public long AcademicSessionId { get; set; }
    public long ClassSectionId { get; set; }
    public long CourseOfferingId { get; set; }
    public long TimetablePeriodMasterId { get; set; }
    public long StaffTeachingAssignmentId { get; set; }
    public short DayOfWeek { get; set; }
    public string? RoomReference { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TimetableEntryResponseDTO : TimetableEntryRequestDTO
{
    public new long Id { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string CourseOfferingName { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public int RevisionNumber { get; set; }
    public short CurrentApprovalSequence { get; set; }
    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanReview { get; set; }
    public bool CanPublish { get; set; }
    public string? ReviewRemark { get; set; }
}

public sealed class TimetableWorkflowStepDTO : SubstituteWorkflowStepDTO;

public class TimetableWorkflowRequestDTO : StaffOperationsPermissionRequestDTO
{
    public long? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? CampusId { get; set; }
    public long? ClassSectionId { get; set; }
    public bool AllowTeacherDraftProposal { get; set; }
    public bool AutoPublishAfterFinalApproval { get; set; } = true;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public List<TimetableWorkflowStepDTO> Steps { get; set; } = [];
}

public sealed class TimetableWorkflowResponseDTO : TimetableWorkflowRequestDTO
{
    public new long Id { get; set; }
}

public sealed class TimetableActionRequestDTO : StaffOperationsPermissionRequestDTO
{
    public string ActionCode { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

public sealed class TimetableApprovalHistoryResponseDTO
{
    public long Id { get; set; }
    public short Sequence { get; set; }
    public long ActionByStaffMemberId { get; set; }
    public string ActionByName { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime ActionDateTime { get; set; }
}

public class SubstituteWorkflowStepDTO
{
    public long? Id { get; set; }
    public short Sequence { get; set; }
    public string ApproverTypeCode { get; set; } = string.Empty;
    public int? AccessRoleId { get; set; }
    public int? StaffPositionMasterId { get; set; }
    public long? StaffMemberId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SubstituteWorkflowRequestDTO : StaffOperationsPermissionRequestDTO
{
    public long? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool RequiresSubstituteAcceptance { get; set; } = true;
    public bool AllowSelfNomination { get; set; }
    public bool AutoApproveWhenNoSteps { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SubstituteWorkflowStepDTO> Steps { get; set; } = [];
}

public sealed class SubstituteWorkflowResponseDTO : SubstituteWorkflowRequestDTO
{
    public new long Id { get; set; }
}

public sealed class StaffAbsenceImpactResponseDTO
{
    public long Id { get; set; }
    public long LeaveRequestId { get; set; }
    public long AbsentStaffMemberId { get; set; }
    public string AbsentStaffMemberName { get; set; } = string.Empty;
    public long TimetableEntryId { get; set; }
    public DateOnly ImpactDate { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string CourseOfferingName { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public long? SubstituteAssignmentId { get; set; }
    public long? SubstituteStaffMemberId { get; set; }
    public string? SubstituteStaffMemberName { get; set; }
    public string? SubstituteStatusCode { get; set; }
}

public sealed class SubstituteAssignmentRequestDTO : StaffOperationsPermissionRequestDTO
{
    public long StaffAbsenceImpactId { get; set; }
    public long SubstituteWorkflowMasterId { get; set; }
    public long SubstituteStaffMemberId { get; set; }
    public string? Remark { get; set; }
}

public sealed class SubstituteActionRequestDTO : StaffOperationsPermissionRequestDTO
{
    public string ActionCode { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

public sealed class CommunicationSettingDTO : StaffOperationsPermissionRequestDTO
{
    public bool EnableInApp { get; set; } = true;
    public bool EnableEmail { get; set; }
    public bool EnableSms { get; set; }
    public bool EnablePush { get; set; }
    public int RetentionDays { get; set; } = 365;
}

public class StaffQueryCategoryRequestDTO : StaffOperationsPermissionRequestDTO
{
    public long? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StaffQueryCategoryResponseDTO : StaffQueryCategoryRequestDTO
{
    public new long Id { get; set; }
}

public sealed class StaffAudienceDTO
{
    public string AudienceTypeCode { get; set; } = string.Empty;
    public long TargetId { get; set; }
}

public sealed class StaffConversationRequestDTO : StaffOperationsPermissionRequestDTO
{
    public string ConversationTypeCode { get; set; } = string.Empty;
    public long? StaffQueryCategoryMasterId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string InitialMessage { get; set; } = string.Empty;
    public List<long> ParticipantStaffMemberIds { get; set; } = [];
    public List<StaffAudienceDTO> Audiences { get; set; } = [];
}

public class StaffConversationResponseDTO
{
    public long Id { get; set; }
    public string ConversationTypeCode { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public long CreatedByStaffMemberId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public int ParticipantCount { get; set; }
    public int MessageCount { get; set; }
    public bool IsClosed { get; set; }
}

public sealed class StaffConversationMessageResponseDTO
{
    public long Id { get; set; }
    public long SenderStaffMemberId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime SentDateTime { get; set; }
    public bool CanAttach { get; set; }
    public List<StaffMessageAttachmentResponseDTO> Attachments { get; set; } = [];
}

public sealed class StaffMessageAttachmentUploadDTO : StaffOperationsPermissionRequestDTO
{
    public IFormFile File { get; set; } = null!;
}

public sealed class StaffMessageAttachmentResponseDTO
{
    public long Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    [JsonIgnore]
    public string StorageObjectKey { get; set; } = string.Empty;
}

public sealed class StaffConversationDetailResponseDTO : StaffConversationResponseDTO
{
    public string? QueryCategoryName { get; set; }
    public List<StaffConversationMessageResponseDTO> Messages { get; set; } = [];
}

public sealed class StaffMessageRequestDTO : StaffOperationsPermissionRequestDTO
{
    public string Body { get; set; } = string.Empty;
}
