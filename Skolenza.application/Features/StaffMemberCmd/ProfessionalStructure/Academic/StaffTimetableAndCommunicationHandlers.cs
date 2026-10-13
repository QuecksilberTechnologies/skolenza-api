using MediatR;
using skolenza.application.DTOS.Staff;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;

namespace skolenza.application.Features.StaffMemberCmd.ProfessionalStructure.Academic;

public sealed record GetStaffOperationsOptionsQuery(StaffOperationsPermissionRequestDTO DTO) : IRequest<ApiResponse<StaffOperationsOptionsResponseDTO>>;
public sealed record GetTimetablePeriodsQuery(StaffOperationsPermissionRequestDTO DTO) : IRequest<ApiResponse<List<TimetablePeriodResponseDTO>>>;
public sealed record SaveTimetablePeriodCommand(TimetablePeriodRequestDTO DTO) : IRequest<ApiResponse<TimetablePeriodResponseDTO>>;
public sealed record GetTimetableEntriesQuery(StaffOperationsPermissionRequestDTO DTO) : IRequest<ApiResponse<List<TimetableEntryResponseDTO>>>;
public sealed record SaveTimetableEntryCommand(TimetableEntryRequestDTO DTO) : IRequest<ApiResponse<TimetableEntryResponseDTO>>;
public sealed record GetSubstituteWorkflowsQuery(StaffOperationsPermissionRequestDTO DTO) : IRequest<ApiResponse<List<SubstituteWorkflowResponseDTO>>>;
public sealed record SaveSubstituteWorkflowCommand(SubstituteWorkflowRequestDTO DTO) : IRequest<ApiResponse<SubstituteWorkflowResponseDTO>>;
public sealed record GetStaffAbsenceImpactsQuery(StaffOperationsPermissionRequestDTO DTO) : IRequest<ApiResponse<List<StaffAbsenceImpactResponseDTO>>>;
public sealed record SynchronizeLeaveImpactsCommand(long LeaveRequestId, StaffOperationsPermissionRequestDTO DTO) : IRequest<ApiResponse<List<StaffAbsenceImpactResponseDTO>>>;
public sealed record AssignSubstituteCommand(SubstituteAssignmentRequestDTO DTO) : IRequest<ApiResponse<StaffAbsenceImpactResponseDTO>>;
public sealed record ActOnSubstituteCommand(long AssignmentId, SubstituteActionRequestDTO DTO) : IRequest<ApiResponse<StaffAbsenceImpactResponseDTO>>;
public sealed record GetCommunicationSettingQuery(StaffOperationsPermissionRequestDTO DTO) : IRequest<ApiResponse<CommunicationSettingDTO>>;
public sealed record SaveCommunicationSettingCommand(CommunicationSettingDTO DTO) : IRequest<ApiResponse<CommunicationSettingDTO>>;
public sealed record GetStaffQueryCategoriesQuery(StaffOperationsPermissionRequestDTO DTO, bool ActiveOnly) : IRequest<ApiResponse<List<StaffQueryCategoryResponseDTO>>>;
public sealed record SaveStaffQueryCategoryCommand(StaffQueryCategoryRequestDTO DTO) : IRequest<ApiResponse<StaffQueryCategoryResponseDTO>>;
public sealed record DeactivateStaffQueryCategoryCommand(long Id, StaffOperationsPermissionRequestDTO DTO) : IRequest<ApiResponse<bool>>;
public sealed record GetStaffConversationsQuery(StaffOperationsPermissionRequestDTO DTO) : IRequest<ApiResponse<List<StaffConversationResponseDTO>>>;
public sealed record GetStaffConversationQuery(long ConversationId, StaffOperationsPermissionRequestDTO DTO) : IRequest<ApiResponse<StaffConversationDetailResponseDTO>>;
public sealed record CreateStaffConversationCommand(StaffConversationRequestDTO DTO) : IRequest<ApiResponse<StaffConversationResponseDTO>>;
public sealed record AddStaffConversationMessageCommand(long ConversationId, StaffMessageRequestDTO DTO) : IRequest<ApiResponse<StaffConversationResponseDTO>>;
public sealed record MarkStaffConversationReadCommand(long ConversationId, StaffOperationsPermissionRequestDTO DTO) : IRequest<ApiResponse<StaffConversationDetailResponseDTO>>;
public sealed record CloseStaffConversationCommand(long ConversationId, StaffOperationsPermissionRequestDTO DTO) : IRequest<ApiResponse<StaffConversationDetailResponseDTO>>;

internal static class StaffOperationsContext
{
    internal static Task<(long InstitutionId, long StaffMemberId)> ResolveAsync(ICommonRequestService common) => CurriculumDeliveryContext.ResolveAsync(common);
}

public sealed class GetStaffOperationsOptionsQueryHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<GetStaffOperationsOptionsQuery, ApiResponse<StaffOperationsOptionsResponseDTO>>
{
    public async Task<ApiResponse<StaffOperationsOptionsResponseDTO>> Handle(
        GetStaffOperationsOptionsQuery request,
        CancellationToken token)
    {
        var actor = await StaffOperationsContext.ResolveAsync(common);
        var options = await unit.StaffAcademicStructureRepository.GetStaffOperationsOptionsAsync(
            actor.InstitutionId,
            token);
        return ApiResponse<StaffOperationsOptionsResponseDTO>.Success(
            options,
            "Staff operation options retrieved successfully.");
    }
}

public sealed class GetTimetablePeriodsQueryHandler(IUnitOfWork unit, ICommonRequestService common) : IRequestHandler<GetTimetablePeriodsQuery, ApiResponse<List<TimetablePeriodResponseDTO>>>
{
    public async Task<ApiResponse<List<TimetablePeriodResponseDTO>>> Handle(GetTimetablePeriodsQuery request, CancellationToken token) { var actor = await StaffOperationsContext.ResolveAsync(common); return ApiResponse<List<TimetablePeriodResponseDTO>>.Success(await unit.StaffAcademicStructureRepository.GetTimetablePeriodsAsync(actor.InstitutionId, token), "Timetable periods retrieved successfully."); }
}
public sealed class SaveTimetablePeriodCommandHandler(IUnitOfWork unit, ICommonRequestService common) : IRequestHandler<SaveTimetablePeriodCommand, ApiResponse<TimetablePeriodResponseDTO>>
{
    public async Task<ApiResponse<TimetablePeriodResponseDTO>> Handle(SaveTimetablePeriodCommand request, CancellationToken token) { var actor = await StaffOperationsContext.ResolveAsync(common); return ApiResponse<TimetablePeriodResponseDTO>.Success(await unit.StaffAcademicStructureRepository.SaveTimetablePeriodAsync(request.DTO, actor.InstitutionId, actor.StaffMemberId, token), "Timetable period saved successfully."); }
}
public sealed class GetTimetableEntriesQueryHandler(IUnitOfWork unit, ICommonRequestService common) : IRequestHandler<GetTimetableEntriesQuery, ApiResponse<List<TimetableEntryResponseDTO>>>
{
    public async Task<ApiResponse<List<TimetableEntryResponseDTO>>> Handle(GetTimetableEntriesQuery request, CancellationToken token) { var actor = await StaffOperationsContext.ResolveAsync(common); return ApiResponse<List<TimetableEntryResponseDTO>>.Success(await unit.StaffAcademicStructureRepository.GetTimetableEntriesAsync(actor.InstitutionId, token), "Timetable entries retrieved successfully."); }
}
public sealed class SaveTimetableEntryCommandHandler(IUnitOfWork unit, ICommonRequestService common) : IRequestHandler<SaveTimetableEntryCommand, ApiResponse<TimetableEntryResponseDTO>>
{
    public async Task<ApiResponse<TimetableEntryResponseDTO>> Handle(SaveTimetableEntryCommand request, CancellationToken token) { var actor = await StaffOperationsContext.ResolveAsync(common); return ApiResponse<TimetableEntryResponseDTO>.Success(await unit.StaffAcademicStructureRepository.SaveTimetableEntryAsync(request.DTO, actor.InstitutionId, actor.StaffMemberId, token), "Timetable entry saved successfully."); }
}
public sealed class GetSubstituteWorkflowsQueryHandler(IUnitOfWork unit, ICommonRequestService common) : IRequestHandler<GetSubstituteWorkflowsQuery, ApiResponse<List<SubstituteWorkflowResponseDTO>>>
{
    public async Task<ApiResponse<List<SubstituteWorkflowResponseDTO>>> Handle(GetSubstituteWorkflowsQuery request, CancellationToken token) { var actor = await StaffOperationsContext.ResolveAsync(common); return ApiResponse<List<SubstituteWorkflowResponseDTO>>.Success(await unit.StaffAcademicStructureRepository.GetSubstituteWorkflowsAsync(actor.InstitutionId, token), "Substitute workflows retrieved successfully."); }
}
public sealed class SaveSubstituteWorkflowCommandHandler(IUnitOfWork unit, ICommonRequestService common) : IRequestHandler<SaveSubstituteWorkflowCommand, ApiResponse<SubstituteWorkflowResponseDTO>>
{
    public async Task<ApiResponse<SubstituteWorkflowResponseDTO>> Handle(SaveSubstituteWorkflowCommand request, CancellationToken token) { var actor = await StaffOperationsContext.ResolveAsync(common); return ApiResponse<SubstituteWorkflowResponseDTO>.Success(await unit.StaffAcademicStructureRepository.SaveSubstituteWorkflowAsync(request.DTO, actor.InstitutionId, actor.StaffMemberId, token), "Substitute workflow saved successfully."); }
}

public sealed class GetStaffAbsenceImpactsQueryHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<GetStaffAbsenceImpactsQuery, ApiResponse<List<StaffAbsenceImpactResponseDTO>>>
{
    public async Task<ApiResponse<List<StaffAbsenceImpactResponseDTO>>> Handle(
        GetStaffAbsenceImpactsQuery request,
        CancellationToken token)
    {
        var actor = await StaffOperationsContext.ResolveAsync(common);
        var impacts = await unit.StaffAcademicStructureRepository.GetAbsenceImpactsAsync(
            actor.InstitutionId,
            token);
        return ApiResponse<List<StaffAbsenceImpactResponseDTO>>.Success(
            impacts,
            "Staff absence impacts retrieved successfully.");
    }
}

public sealed class SynchronizeLeaveImpactsCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<SynchronizeLeaveImpactsCommand, ApiResponse<List<StaffAbsenceImpactResponseDTO>>>
{
    public async Task<ApiResponse<List<StaffAbsenceImpactResponseDTO>>> Handle(
        SynchronizeLeaveImpactsCommand request,
        CancellationToken token)
    {
        var actor = await StaffOperationsContext.ResolveAsync(common);
        var impacts = await unit.StaffAcademicStructureRepository.SynchronizeLeaveImpactsAsync(
            request.LeaveRequestId,
            actor.InstitutionId,
            actor.StaffMemberId,
            token);
        return ApiResponse<List<StaffAbsenceImpactResponseDTO>>.Success(
            impacts,
            "Approved leave impacts synchronized successfully.");
    }
}

public sealed class AssignSubstituteCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<AssignSubstituteCommand, ApiResponse<StaffAbsenceImpactResponseDTO>>
{
    public async Task<ApiResponse<StaffAbsenceImpactResponseDTO>> Handle(
        AssignSubstituteCommand request,
        CancellationToken token)
    {
        var actor = await StaffOperationsContext.ResolveAsync(common);
        var impact = await unit.StaffAcademicStructureRepository.AssignSubstituteAsync(
            request.DTO,
            actor.InstitutionId,
            actor.StaffMemberId,
            token);
        return ApiResponse<StaffAbsenceImpactResponseDTO>.Success(
            impact,
            "Substitute assigned successfully.");
    }
}

public sealed class ActOnSubstituteCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<ActOnSubstituteCommand, ApiResponse<StaffAbsenceImpactResponseDTO>>
{
    public async Task<ApiResponse<StaffAbsenceImpactResponseDTO>> Handle(
        ActOnSubstituteCommand request,
        CancellationToken token)
    {
        var actor = await StaffOperationsContext.ResolveAsync(common);
        var impact = await unit.StaffAcademicStructureRepository.ActOnSubstituteAsync(
            request.AssignmentId,
            request.DTO,
            actor.InstitutionId,
            actor.StaffMemberId,
            token);
        return ApiResponse<StaffAbsenceImpactResponseDTO>.Success(
            impact,
            "Substitute workflow action completed successfully.");
    }
}
public sealed class GetCommunicationSettingQueryHandler(IUnitOfWork unit, ICommonRequestService common) : IRequestHandler<GetCommunicationSettingQuery, ApiResponse<CommunicationSettingDTO>>
{
    public async Task<ApiResponse<CommunicationSettingDTO>> Handle(GetCommunicationSettingQuery request, CancellationToken token) { var actor = await StaffOperationsContext.ResolveAsync(common); return ApiResponse<CommunicationSettingDTO>.Success(await unit.StaffAcademicStructureRepository.GetCommunicationSettingAsync(actor.InstitutionId, token), "Communication setting retrieved successfully."); }
}
public sealed class SaveCommunicationSettingCommandHandler(IUnitOfWork unit, ICommonRequestService common) : IRequestHandler<SaveCommunicationSettingCommand, ApiResponse<CommunicationSettingDTO>>
{
    public async Task<ApiResponse<CommunicationSettingDTO>> Handle(SaveCommunicationSettingCommand request, CancellationToken token) { var actor = await StaffOperationsContext.ResolveAsync(common); return ApiResponse<CommunicationSettingDTO>.Success(await unit.StaffAcademicStructureRepository.SaveCommunicationSettingAsync(request.DTO, actor.InstitutionId, actor.StaffMemberId, token), "Communication setting saved successfully."); }
}

public sealed class GetStaffQueryCategoriesQueryHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<GetStaffQueryCategoriesQuery, ApiResponse<List<StaffQueryCategoryResponseDTO>>>
{
    public async Task<ApiResponse<List<StaffQueryCategoryResponseDTO>>> Handle(
        GetStaffQueryCategoriesQuery request,
        CancellationToken token)
    {
        var actor = await StaffOperationsContext.ResolveAsync(common);
        var rows = await unit.StaffAcademicStructureRepository.GetQueryCategoriesAsync(
            actor.InstitutionId,
            request.ActiveOnly,
            token);
        return ApiResponse<List<StaffQueryCategoryResponseDTO>>.Success(
            rows,
            "Staff query categories retrieved successfully.");
    }
}

public sealed class SaveStaffQueryCategoryCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<SaveStaffQueryCategoryCommand, ApiResponse<StaffQueryCategoryResponseDTO>>
{
    public async Task<ApiResponse<StaffQueryCategoryResponseDTO>> Handle(
        SaveStaffQueryCategoryCommand request,
        CancellationToken token)
    {
        var actor = await StaffOperationsContext.ResolveAsync(common);
        var row = await unit.StaffAcademicStructureRepository.SaveQueryCategoryAsync(
            request.DTO,
            actor.InstitutionId,
            actor.StaffMemberId,
            token);
        return ApiResponse<StaffQueryCategoryResponseDTO>.Success(
            row,
            "Staff query category saved successfully.");
    }
}

public sealed class DeactivateStaffQueryCategoryCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<DeactivateStaffQueryCategoryCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeactivateStaffQueryCategoryCommand request,
        CancellationToken token)
    {
        var actor = await StaffOperationsContext.ResolveAsync(common);
        await unit.StaffAcademicStructureRepository.DeactivateQueryCategoryAsync(
            request.Id,
            actor.InstitutionId,
            actor.StaffMemberId,
            token);
        return ApiResponse<bool>.Success(true, "Staff query category deactivated successfully.");
    }
}
public sealed class GetStaffConversationsQueryHandler(IUnitOfWork unit, ICommonRequestService common) : IRequestHandler<GetStaffConversationsQuery, ApiResponse<List<StaffConversationResponseDTO>>>
{
    public async Task<ApiResponse<List<StaffConversationResponseDTO>>> Handle(GetStaffConversationsQuery request, CancellationToken token) { var actor = await StaffOperationsContext.ResolveAsync(common); return ApiResponse<List<StaffConversationResponseDTO>>.Success(await unit.StaffAcademicStructureRepository.GetConversationsAsync(actor.InstitutionId, actor.StaffMemberId, token), "Staff conversations retrieved successfully."); }
}
public sealed class GetStaffConversationQueryHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<GetStaffConversationQuery, ApiResponse<StaffConversationDetailResponseDTO>>
{
    public async Task<ApiResponse<StaffConversationDetailResponseDTO>> Handle(
        GetStaffConversationQuery request,
        CancellationToken token)
    {
        var actor = await StaffOperationsContext.ResolveAsync(common);
        var conversation = await unit.StaffAcademicStructureRepository.GetConversationAsync(
            request.ConversationId,
            actor.InstitutionId,
            actor.StaffMemberId,
            token);
        return ApiResponse<StaffConversationDetailResponseDTO>.Success(
            conversation,
            "Staff conversation retrieved successfully.");
    }
}
public sealed class CreateStaffConversationCommandHandler(IUnitOfWork unit, ICommonRequestService common) : IRequestHandler<CreateStaffConversationCommand, ApiResponse<StaffConversationResponseDTO>>
{
    public async Task<ApiResponse<StaffConversationResponseDTO>> Handle(CreateStaffConversationCommand request, CancellationToken token) { var actor = await StaffOperationsContext.ResolveAsync(common); return ApiResponse<StaffConversationResponseDTO>.Success(await unit.StaffAcademicStructureRepository.CreateConversationAsync(request.DTO, actor.InstitutionId, actor.StaffMemberId, token), "Staff conversation created successfully."); }
}
public sealed class AddStaffConversationMessageCommandHandler(IUnitOfWork unit, ICommonRequestService common) : IRequestHandler<AddStaffConversationMessageCommand, ApiResponse<StaffConversationResponseDTO>>
{
    public async Task<ApiResponse<StaffConversationResponseDTO>> Handle(AddStaffConversationMessageCommand request, CancellationToken token) { var actor = await StaffOperationsContext.ResolveAsync(common); return ApiResponse<StaffConversationResponseDTO>.Success(await unit.StaffAcademicStructureRepository.AddMessageAsync(request.ConversationId, request.DTO, actor.InstitutionId, actor.StaffMemberId, token), "Message sent successfully."); }
}
public sealed class MarkStaffConversationReadCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<MarkStaffConversationReadCommand, ApiResponse<StaffConversationDetailResponseDTO>>
{
    public async Task<ApiResponse<StaffConversationDetailResponseDTO>> Handle(
        MarkStaffConversationReadCommand request,
        CancellationToken token)
    {
        var actor = await StaffOperationsContext.ResolveAsync(common);
        var conversation = await unit.StaffAcademicStructureRepository.MarkConversationReadAsync(
            request.ConversationId,
            actor.InstitutionId,
            actor.StaffMemberId,
            token);
        return ApiResponse<StaffConversationDetailResponseDTO>.Success(
            conversation,
            "Conversation marked as read.");
    }
}

public sealed class CloseStaffConversationCommandHandler(
    IUnitOfWork unit,
    ICommonRequestService common)
    : IRequestHandler<CloseStaffConversationCommand, ApiResponse<StaffConversationDetailResponseDTO>>
{
    public async Task<ApiResponse<StaffConversationDetailResponseDTO>> Handle(
        CloseStaffConversationCommand request,
        CancellationToken token)
    {
        var actor = await StaffOperationsContext.ResolveAsync(common);
        var conversation = await unit.StaffAcademicStructureRepository.CloseConversationAsync(
            request.ConversationId,
            actor.InstitutionId,
            actor.StaffMemberId,
            token);
        return ApiResponse<StaffConversationDetailResponseDTO>.Success(
            conversation,
            "Conversation closed successfully.");
    }
}
