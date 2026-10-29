using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Student;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.StudentCmd;

#region Requests

public sealed record GetStudentBenefitSchemesQuery(StudentBenefitSchemeFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentBenefitSchemeResponseDTO>>>;

public sealed record CreateStudentBenefitSchemeCommand(StudentBenefitSchemeRequestDTO DTO)
    : IRequest<ApiResponse<StudentBenefitSchemeResponseDTO>>;

public sealed record UpdateStudentBenefitSchemeCommand(long Id, StudentBenefitSchemeRequestDTO DTO)
    : IRequest<ApiResponse<StudentBenefitSchemeResponseDTO>>;

public sealed record DeleteStudentBenefitSchemeCommand(long Id, StudentFeeMasterDeleteDTO DTO)
    : IRequest<ApiResponse<bool>>;

#endregion

#region Handlers

public sealed class GetStudentBenefitSchemesQueryHandler(
    IStudentBenefitSchemeRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStudentBenefitSchemesQuery, ApiResponse<List<StudentBenefitSchemeResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentBenefitSchemeResponseDTO>>> Handle(
        GetStudentBenefitSchemesQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.GetPageAsync(actor.InstitutionId, request.DTO, cancellationToken);
        return ApiResponse<List<StudentBenefitSchemeResponseDTO>>.SuccessPaginated(
            result.Data,
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            "Student benefit schemes retrieved successfully.");
    }
}

public sealed class CreateStudentBenefitSchemeCommandHandler(
    IStudentBenefitSchemeRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<CreateStudentBenefitSchemeCommand, ApiResponse<StudentBenefitSchemeResponseDTO>>
{
    public async Task<ApiResponse<StudentBenefitSchemeResponseDTO>> Handle(
        CreateStudentBenefitSchemeCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.SaveAsync(
            null, actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentBenefitSchemeResponseDTO>.Success(
            result, "Student benefit scheme created successfully.");
    }
}

public sealed class UpdateStudentBenefitSchemeCommandHandler(
    IStudentBenefitSchemeRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<UpdateStudentBenefitSchemeCommand, ApiResponse<StudentBenefitSchemeResponseDTO>>
{
    public async Task<ApiResponse<StudentBenefitSchemeResponseDTO>> Handle(
        UpdateStudentBenefitSchemeCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.SaveAsync(
            request.Id, actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentBenefitSchemeResponseDTO>.Success(
            result, "Student benefit scheme updated successfully.");
    }
}

public sealed class DeleteStudentBenefitSchemeCommandHandler(
    IStudentBenefitSchemeRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<DeleteStudentBenefitSchemeCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteStudentBenefitSchemeCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        await repository.DeleteAsync(request.Id, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<bool>.Success(true, "Student benefit scheme deactivated successfully.");
    }
}

#endregion
