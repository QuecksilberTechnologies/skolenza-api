namespace skolenza.domain.Entity;

#region Phase 4 - Timetable and substitute coverage

public sealed class TimetablePeriodMaster : InstitutionAcademicEntity
{
    public long AcademicSessionId { get; set; }
    public long? CampusId { get; set; }
    public short Sequence { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

public sealed class TimetableEntry
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
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
    public long? TimetableWorkflowMasterId { get; set; }
    public long? ParentTimetableEntryId { get; set; }
    public int RevisionNumber { get; set; } = 1;
    public string StatusCode { get; set; } = "DRAFT";
    public short CurrentApprovalSequence { get; set; }
    public DateTime? SubmittedDateTime { get; set; }
    public DateTime? ApprovedDateTime { get; set; }
    public DateTime? PublishedDateTime { get; set; }
    public DateTime? RejectedDateTime { get; set; }
    public string? ReviewRemark { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public sealed class TimetableWorkflowMaster : InstitutionAcademicEntity
{
    public long? CampusId { get; set; }
    public long? ClassSectionId { get; set; }
    public bool AllowTeacherDraftProposal { get; set; }
    public bool AutoPublishAfterFinalApproval { get; set; } = true;
    public bool IsDefault { get; set; }
}

public sealed class TimetableWorkflowStep
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long TimetableWorkflowMasterId { get; set; }
    public short Sequence { get; set; }
    public string ApproverTypeCode { get; set; } = string.Empty;
    public int? AccessRoleId { get; set; }
    public int? StaffPositionMasterId { get; set; }
    public long? StaffMemberId { get; set; }
    public bool IsActive { get; set; } = true;
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
}

public sealed class TimetableApprovalHistory
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long TimetableEntryId { get; set; }
    public short Sequence { get; set; }
    public long ActionByStaffMemberId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime ActionDateTime { get; set; }
}

public sealed class SubstituteWorkflowMaster : InstitutionAcademicEntity
{
    public bool RequiresSubstituteAcceptance { get; set; } = true;
    public bool AllowSelfNomination { get; set; }
    public bool AutoApproveWhenNoSteps { get; set; }
}

public sealed class SubstituteWorkflowStep
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long SubstituteWorkflowMasterId { get; set; }
    public short Sequence { get; set; }
    public string ApproverTypeCode { get; set; } = string.Empty;
    public int? AccessRoleId { get; set; }
    public int? StaffPositionMasterId { get; set; }
    public long? StaffMemberId { get; set; }
    public bool IsActive { get; set; } = true;
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
}

public sealed class StaffAbsenceImpact
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long LeaveRequestId { get; set; }
    public long AbsentStaffMemberId { get; set; }
    public long TimetableEntryId { get; set; }
    public DateOnly ImpactDate { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public sealed class SubstituteAssignment
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffAbsenceImpactId { get; set; }
    public long SubstituteWorkflowMasterId { get; set; }
    public long SubstituteStaffMemberId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public short CurrentApprovalSequence { get; set; }
    public DateTime? AcceptedDateTime { get; set; }
    public string? Remark { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public sealed class SubstituteApprovalHistory
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long SubstituteAssignmentId { get; set; }
    public short Sequence { get; set; }
    public long ActionByStaffMemberId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime ActionDateTime { get; set; }
}

#endregion

#region Phase 5 - Staff communication

public sealed class InstitutionCommunicationSetting
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public bool EnableInApp { get; set; } = true;
    public bool EnableEmail { get; set; }
    public bool EnableSms { get; set; }
    public bool EnablePush { get; set; }
    public int RetentionDays { get; set; } = 365;
    public bool IsActive { get; set; } = true;
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public sealed class StaffQueryCategoryMaster : InstitutionAcademicEntity;

public sealed class StaffConversation
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public string ConversationTypeCode { get; set; } = string.Empty;
    public long? StaffQueryCategoryMasterId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public long CreatedByStaffMemberId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedDateTime { get; set; }
}

public sealed class StaffConversationParticipant
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffConversationId { get; set; }
    public long StaffMemberId { get; set; }
    public string ParticipantRoleCode { get; set; } = string.Empty;
    public DateTime? LastReadDateTime { get; set; }
    public bool IsMuted { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StaffConversationMessage
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffConversationId { get; set; }
    public long SenderStaffMemberId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime SentDateTime { get; set; }
    public bool IsSoftDeleted { get; set; }
}

public sealed class StaffMessageAttachment
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffConversationMessageId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StorageObjectKey { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

public sealed class StaffBroadcastAudience
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffConversationId { get; set; }
    public string AudienceTypeCode { get; set; } = string.Empty;
    public long TargetId { get; set; }
}

#endregion
