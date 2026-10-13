using skolenza.application.DTOS.Staff;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStaffAcademicStructureRepository
{
    Task<List<TeachingAssignmentTypeOptionDTO>> GetTeachingAssignmentTypesAsync(CancellationToken cancellationToken = default);
    Task<List<AcademicMasterResponseDTO>> GetMastersAsync(AcademicMasterType type, long institutionId, bool activeOnly, CancellationToken cancellationToken = default);
    Task<AcademicMasterResponseDTO> SaveMasterAsync(AcademicMasterType type, AcademicMasterRequestDTO request, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task DeleteMasterAsync(AcademicMasterType type, long id, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task<StaffAcademicProfileResponseDTO> GetStaffProfileAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken = default);
    Task<StaffTeachingAssignmentResponseDTO> SaveTeachingAssignmentAsync(StaffTeachingAssignmentRequestDTO request, long institutionId, long staffMemberId, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task<ClassTeacherAssignmentResponseDTO> SaveClassTeacherAssignmentAsync(ClassTeacherAssignmentRequestDTO request, long institutionId, long staffMemberId, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task DeleteTeachingAssignmentAsync(long id, long institutionId, CancellationToken cancellationToken = default);
    Task DeleteClassTeacherAssignmentAsync(long id, long institutionId, CancellationToken cancellationToken = default);
    Task<List<AcademicWorkflowStatusOptionDTO>> GetWorkflowStatusesAsync(CancellationToken cancellationToken = default);
    Task<List<CurriculumFrameworkResponseDTO>> GetCurriculumFrameworksAsync(long institutionId, CancellationToken cancellationToken = default);
    Task<CurriculumFrameworkResponseDTO> SaveCurriculumFrameworkAsync(CurriculumFrameworkRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default);
    Task<CurriculumFrameworkResponseDTO> ReviewCurriculumFrameworkAsync(WorkflowReviewRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default);
    Task<List<CurriculumUnitResponseDTO>> GetCurriculumUnitsAsync(long frameworkId, long institutionId, CancellationToken cancellationToken = default);
    Task<CurriculumUnitResponseDTO> SaveCurriculumUnitAsync(CurriculumUnitRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default);
    Task<CurriculumTopicResponseDTO> SaveCurriculumTopicAsync(CurriculumTopicRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default);
    Task<List<LessonPlanResponseDTO>> GetLessonPlansAsync(long institutionId, CancellationToken cancellationToken = default);
    Task<List<StaffTeachingAssignmentResponseDTO>> GetTeachingAssignmentsAsync(long institutionId, CancellationToken cancellationToken = default);
    Task<LessonPlanResponseDTO> SaveLessonPlanAsync(LessonPlanRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default);
    Task<LessonPlanResponseDTO> ReviewLessonPlanAsync(WorkflowReviewRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default);
    Task<List<LessonDeliveryResponseDTO>> GetLessonDeliveriesAsync(long institutionId, CancellationToken cancellationToken = default);
    Task<LessonDeliveryResponseDTO> SaveLessonDeliveryAsync(LessonDeliveryRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default);
    Task<LessonDeliveryResponseDTO> ReviewLessonDeliveryAsync(WorkflowReviewRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default);
    Task<List<SyllabusProgressResponseDTO>> GetSyllabusProgressAsync(long institutionId, long? courseOfferingId, CancellationToken cancellationToken = default);
}
