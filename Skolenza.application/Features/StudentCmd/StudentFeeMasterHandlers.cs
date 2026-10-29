using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Student;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.StudentCmd;

#region Requests

public sealed record GetStudentFeeFrequenciesQuery(StudentFeeFrequencyFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentFeeFrequencyResponseDTO>>>;

public sealed record CreateStudentFeeFrequencyCommand(StudentFeeFrequencyRequestDTO DTO)
    : IRequest<ApiResponse<StudentFeeFrequencyResponseDTO>>;

public sealed record UpdateStudentFeeFrequencyCommand(long Id, StudentFeeFrequencyRequestDTO DTO)
    : IRequest<ApiResponse<StudentFeeFrequencyResponseDTO>>;

public sealed record DeleteStudentFeeFrequencyCommand(long Id, StudentFeeMasterDeleteDTO DTO)
    : IRequest<ApiResponse<bool>>;

public sealed record GetStudentFeeComponentsQuery(StudentFeeComponentFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentFeeComponentResponseDTO>>>;

public sealed record CreateStudentFeeComponentCommand(StudentFeeComponentRequestDTO DTO)
    : IRequest<ApiResponse<StudentFeeComponentResponseDTO>>;

public sealed record UpdateStudentFeeComponentCommand(long Id, StudentFeeComponentRequestDTO DTO)
    : IRequest<ApiResponse<StudentFeeComponentResponseDTO>>;

public sealed record DeleteStudentFeeComponentCommand(long Id, StudentFeeMasterDeleteDTO DTO)
    : IRequest<ApiResponse<bool>>;

#endregion

#region Handlers

public sealed class GetStudentFeeFrequenciesQueryHandler(
    IStudentFeeMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStudentFeeFrequenciesQuery, ApiResponse<List<StudentFeeFrequencyResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentFeeFrequencyResponseDTO>>> Handle(
        GetStudentFeeFrequenciesQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var page = await repository.GetFrequenciesAsync(actor.InstitutionId, request.DTO, cancellationToken);
        return ApiResponse<List<StudentFeeFrequencyResponseDTO>>.SuccessPaginated(
            page.Data,
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            "Student fee frequencies retrieved successfully.");
    }
}

public sealed class CreateStudentFeeFrequencyCommandHandler(
    IStudentFeeMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<CreateStudentFeeFrequencyCommand, ApiResponse<StudentFeeFrequencyResponseDTO>>
{
    public async Task<ApiResponse<StudentFeeFrequencyResponseDTO>> Handle(
        CreateStudentFeeFrequencyCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.SaveFrequencyAsync(
            null, actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentFeeFrequencyResponseDTO>.Success(result, "Student fee frequency created successfully.");
    }
}

public sealed class UpdateStudentFeeFrequencyCommandHandler(
    IStudentFeeMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<UpdateStudentFeeFrequencyCommand, ApiResponse<StudentFeeFrequencyResponseDTO>>
{
    public async Task<ApiResponse<StudentFeeFrequencyResponseDTO>> Handle(
        UpdateStudentFeeFrequencyCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.SaveFrequencyAsync(
            request.Id, actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentFeeFrequencyResponseDTO>.Success(result, "Student fee frequency updated successfully.");
    }
}

public sealed class DeleteStudentFeeFrequencyCommandHandler(
    IStudentFeeMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<DeleteStudentFeeFrequencyCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteStudentFeeFrequencyCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        await repository.DeleteFrequencyAsync(request.Id, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<bool>.Success(true, "Student fee frequency deactivated successfully.");
    }
}

public sealed class GetStudentFeeComponentsQueryHandler(
    IStudentFeeMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStudentFeeComponentsQuery, ApiResponse<List<StudentFeeComponentResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentFeeComponentResponseDTO>>> Handle(
        GetStudentFeeComponentsQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var page = await repository.GetComponentsAsync(actor.InstitutionId, request.DTO, cancellationToken);
        return ApiResponse<List<StudentFeeComponentResponseDTO>>.SuccessPaginated(
            page.Data,
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            "Student fee components retrieved successfully.");
    }
}

public sealed class CreateStudentFeeComponentCommandHandler(
    IStudentFeeMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<CreateStudentFeeComponentCommand, ApiResponse<StudentFeeComponentResponseDTO>>
{
    public async Task<ApiResponse<StudentFeeComponentResponseDTO>> Handle(
        CreateStudentFeeComponentCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.SaveComponentAsync(
            null, actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentFeeComponentResponseDTO>.Success(result, "Student fee component created successfully.");
    }
}

public sealed class UpdateStudentFeeComponentCommandHandler(
    IStudentFeeMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<UpdateStudentFeeComponentCommand, ApiResponse<StudentFeeComponentResponseDTO>>
{
    public async Task<ApiResponse<StudentFeeComponentResponseDTO>> Handle(
        UpdateStudentFeeComponentCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.SaveComponentAsync(
            request.Id, actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentFeeComponentResponseDTO>.Success(result, "Student fee component updated successfully.");
    }
}

public sealed class DeleteStudentFeeComponentCommandHandler(
    IStudentFeeMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<DeleteStudentFeeComponentCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteStudentFeeComponentCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        await repository.DeleteComponentAsync(request.Id, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<bool>.Success(true, "Student fee component deactivated successfully.");
    }
}

#endregion
