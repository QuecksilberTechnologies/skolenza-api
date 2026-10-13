using MediatR;
using skolenza.application.DTOS.Staff;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;

namespace skolenza.application.Features.StaffMemberCmd.ProfessionalStructure.Academic;

#region Requests

public sealed record GetAcademicWorkflowStatusesQuery(CurriculumDeliveryPermissionRequestDTO DTO) : IRequest<ApiResponse<List<AcademicWorkflowStatusOptionDTO>>>;
public sealed record GetCurriculumFrameworksQuery(CurriculumDeliveryPermissionRequestDTO DTO) : IRequest<ApiResponse<List<CurriculumFrameworkResponseDTO>>>;
public sealed record SaveCurriculumFrameworkCommand(CurriculumFrameworkRequestDTO DTO) : IRequest<ApiResponse<CurriculumFrameworkResponseDTO>>;
public sealed record ReviewCurriculumFrameworkCommand(WorkflowReviewRequestDTO DTO) : IRequest<ApiResponse<CurriculumFrameworkResponseDTO>>;
public sealed record GetCurriculumUnitsQuery(long FrameworkId, CurriculumDeliveryPermissionRequestDTO DTO) : IRequest<ApiResponse<List<CurriculumUnitResponseDTO>>>;
public sealed record SaveCurriculumUnitCommand(CurriculumUnitRequestDTO DTO) : IRequest<ApiResponse<CurriculumUnitResponseDTO>>;
public sealed record SaveCurriculumTopicCommand(CurriculumTopicRequestDTO DTO) : IRequest<ApiResponse<CurriculumTopicResponseDTO>>;
public sealed record GetLessonPlansQuery(CurriculumDeliveryPermissionRequestDTO DTO) : IRequest<ApiResponse<List<LessonPlanResponseDTO>>>;
public sealed record GetCurriculumTeachingAssignmentsQuery(CurriculumDeliveryPermissionRequestDTO DTO) : IRequest<ApiResponse<List<StaffTeachingAssignmentResponseDTO>>>;
public sealed record SaveLessonPlanCommand(LessonPlanRequestDTO DTO) : IRequest<ApiResponse<LessonPlanResponseDTO>>;
public sealed record ReviewLessonPlanCommand(WorkflowReviewRequestDTO DTO) : IRequest<ApiResponse<LessonPlanResponseDTO>>;
public sealed record GetLessonDeliveriesQuery(CurriculumDeliveryPermissionRequestDTO DTO) : IRequest<ApiResponse<List<LessonDeliveryResponseDTO>>>;
public sealed record SaveLessonDeliveryCommand(LessonDeliveryRequestDTO DTO) : IRequest<ApiResponse<LessonDeliveryResponseDTO>>;
public sealed record ReviewLessonDeliveryCommand(WorkflowReviewRequestDTO DTO) : IRequest<ApiResponse<LessonDeliveryResponseDTO>>;
public sealed record GetSyllabusProgressQuery(long? CourseOfferingId, CurriculumDeliveryPermissionRequestDTO DTO) : IRequest<ApiResponse<List<SyllabusProgressResponseDTO>>>;

#endregion

#region Handlers

internal static class CurriculumDeliveryContext
{
    internal static async Task<(long InstitutionId, long StaffMemberId)> ResolveAsync(ICommonRequestService service)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(service);
        return (actor.InstitutionId, actor.StaffMemberId);
    }
}

public sealed class GetAcademicWorkflowStatusesQueryHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<GetAcademicWorkflowStatusesQuery, ApiResponse<List<AcademicWorkflowStatusOptionDTO>>>
{
    public async Task<ApiResponse<List<AcademicWorkflowStatusOptionDTO>>> Handle(
        GetAcademicWorkflowStatusesQuery request,
        CancellationToken token)
    {
        await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.GetWorkflowStatusesAsync(token);
        return ApiResponse<List<AcademicWorkflowStatusOptionDTO>>.Success(data, "Academic workflow statuses retrieved successfully.");
    }
}
public sealed class GetCurriculumFrameworksQueryHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<GetCurriculumFrameworksQuery, ApiResponse<List<CurriculumFrameworkResponseDTO>>>
{
    public async Task<ApiResponse<List<CurriculumFrameworkResponseDTO>>> Handle(
        GetCurriculumFrameworksQuery request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.GetCurriculumFrameworksAsync(actor.InstitutionId, token);
        return ApiResponse<List<CurriculumFrameworkResponseDTO>>.Success(data, "Curriculum frameworks retrieved successfully.");
    }
}
public sealed class SaveCurriculumFrameworkCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<SaveCurriculumFrameworkCommand, ApiResponse<CurriculumFrameworkResponseDTO>>
{
    public async Task<ApiResponse<CurriculumFrameworkResponseDTO>> Handle(
        SaveCurriculumFrameworkCommand request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.SaveCurriculumFrameworkAsync(request.DTO, actor.InstitutionId, actor.StaffMemberId, token);
        return ApiResponse<CurriculumFrameworkResponseDTO>.Success(data, "Curriculum framework saved successfully.");
    }
}
public sealed class GetCurriculumUnitsQueryHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<GetCurriculumUnitsQuery, ApiResponse<List<CurriculumUnitResponseDTO>>>
{
    public async Task<ApiResponse<List<CurriculumUnitResponseDTO>>> Handle(
        GetCurriculumUnitsQuery request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.GetCurriculumUnitsAsync(request.FrameworkId, actor.InstitutionId, token);
        return ApiResponse<List<CurriculumUnitResponseDTO>>.Success(data, "Curriculum units retrieved successfully.");
    }
}
public sealed class ReviewCurriculumFrameworkCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<ReviewCurriculumFrameworkCommand, ApiResponse<CurriculumFrameworkResponseDTO>>
{
    public async Task<ApiResponse<CurriculumFrameworkResponseDTO>> Handle(
        ReviewCurriculumFrameworkCommand request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.ReviewCurriculumFrameworkAsync(
            request.DTO,
            actor.InstitutionId,
            actor.StaffMemberId,
            token);
        return ApiResponse<CurriculumFrameworkResponseDTO>.Success(
            data,
            "Curriculum framework status updated successfully.");
    }
}
public sealed class SaveCurriculumUnitCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<SaveCurriculumUnitCommand, ApiResponse<CurriculumUnitResponseDTO>>
{
    public async Task<ApiResponse<CurriculumUnitResponseDTO>> Handle(
        SaveCurriculumUnitCommand request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.SaveCurriculumUnitAsync(request.DTO, actor.InstitutionId, actor.StaffMemberId, token);
        return ApiResponse<CurriculumUnitResponseDTO>.Success(data, "Curriculum unit saved successfully.");
    }
}
public sealed class SaveCurriculumTopicCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<SaveCurriculumTopicCommand, ApiResponse<CurriculumTopicResponseDTO>>
{
    public async Task<ApiResponse<CurriculumTopicResponseDTO>> Handle(
        SaveCurriculumTopicCommand request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.SaveCurriculumTopicAsync(request.DTO, actor.InstitutionId, actor.StaffMemberId, token);
        return ApiResponse<CurriculumTopicResponseDTO>.Success(data, "Curriculum topic saved successfully.");
    }
}
public sealed class GetLessonPlansQueryHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<GetLessonPlansQuery, ApiResponse<List<LessonPlanResponseDTO>>>
{
    public async Task<ApiResponse<List<LessonPlanResponseDTO>>> Handle(
        GetLessonPlansQuery request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.GetLessonPlansAsync(actor.InstitutionId, token);
        return ApiResponse<List<LessonPlanResponseDTO>>.Success(data, "Lesson plans retrieved successfully.");
    }
}
public sealed class GetCurriculumTeachingAssignmentsQueryHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<GetCurriculumTeachingAssignmentsQuery, ApiResponse<List<StaffTeachingAssignmentResponseDTO>>>
{
    public async Task<ApiResponse<List<StaffTeachingAssignmentResponseDTO>>> Handle(
        GetCurriculumTeachingAssignmentsQuery request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.GetTeachingAssignmentsAsync(actor.InstitutionId, token);
        return ApiResponse<List<StaffTeachingAssignmentResponseDTO>>.Success(data, "Teaching assignments retrieved successfully.");
    }
}
public sealed class SaveLessonPlanCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<SaveLessonPlanCommand, ApiResponse<LessonPlanResponseDTO>>
{
    public async Task<ApiResponse<LessonPlanResponseDTO>> Handle(
        SaveLessonPlanCommand request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.SaveLessonPlanAsync(request.DTO, actor.InstitutionId, actor.StaffMemberId, token);
        return ApiResponse<LessonPlanResponseDTO>.Success(data, "Lesson plan saved successfully.");
    }
}
public sealed class ReviewLessonPlanCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<ReviewLessonPlanCommand, ApiResponse<LessonPlanResponseDTO>>
{
    public async Task<ApiResponse<LessonPlanResponseDTO>> Handle(
        ReviewLessonPlanCommand request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.ReviewLessonPlanAsync(request.DTO, actor.InstitutionId, actor.StaffMemberId, token);
        return ApiResponse<LessonPlanResponseDTO>.Success(data, "Lesson plan status updated successfully.");
    }
}
public sealed class GetLessonDeliveriesQueryHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<GetLessonDeliveriesQuery, ApiResponse<List<LessonDeliveryResponseDTO>>>
{
    public async Task<ApiResponse<List<LessonDeliveryResponseDTO>>> Handle(
        GetLessonDeliveriesQuery request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.GetLessonDeliveriesAsync(actor.InstitutionId, token);
        return ApiResponse<List<LessonDeliveryResponseDTO>>.Success(data, "Lesson deliveries retrieved successfully.");
    }
}
public sealed class SaveLessonDeliveryCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<SaveLessonDeliveryCommand, ApiResponse<LessonDeliveryResponseDTO>>
{
    public async Task<ApiResponse<LessonDeliveryResponseDTO>> Handle(
        SaveLessonDeliveryCommand request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.SaveLessonDeliveryAsync(request.DTO, actor.InstitutionId, actor.StaffMemberId, token);
        return ApiResponse<LessonDeliveryResponseDTO>.Success(data, "Lesson delivery saved successfully.");
    }
}
public sealed class ReviewLessonDeliveryCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<ReviewLessonDeliveryCommand, ApiResponse<LessonDeliveryResponseDTO>>
{
    public async Task<ApiResponse<LessonDeliveryResponseDTO>> Handle(
        ReviewLessonDeliveryCommand request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.ReviewLessonDeliveryAsync(request.DTO, actor.InstitutionId, actor.StaffMemberId, token);
        return ApiResponse<LessonDeliveryResponseDTO>.Success(data, "Lesson delivery status updated successfully.");
    }
}
public sealed class GetSyllabusProgressQueryHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<GetSyllabusProgressQuery, ApiResponse<List<SyllabusProgressResponseDTO>>>
{
    public async Task<ApiResponse<List<SyllabusProgressResponseDTO>>> Handle(
        GetSyllabusProgressQuery request,
        CancellationToken token)
    {
        var actor = await CurriculumDeliveryContext.ResolveAsync(common);
        var data = await unit.StaffAcademicStructureRepository.GetSyllabusProgressAsync(actor.InstitutionId, request.CourseOfferingId, token);
        return ApiResponse<List<SyllabusProgressResponseDTO>>.Success(data, "Syllabus progress retrieved successfully.");
    }
}
#endregion

