using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.StudentCmd;

#region Requests

public sealed record GetStudentDocumentRequirementsQuery(StudentDocumentRequirementFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentDocumentRequirementResponseDTO>>>;

public sealed record CreateStudentDocumentRequirementCommand(StudentDocumentRequirementRequestDTO DTO)
    : IRequest<ApiResponse<StudentDocumentRequirementResponseDTO>>;

public sealed record UpdateStudentDocumentRequirementCommand(long Id, StudentDocumentRequirementRequestDTO DTO)
    : IRequest<ApiResponse<StudentDocumentRequirementResponseDTO>>;

public sealed record DeleteStudentDocumentRequirementCommand(long Id, StudentDocumentRequirementDeleteDTO DTO)
    : IRequest<ApiResponse<bool>>;

#endregion

#region Handlers

public sealed class GetStudentDocumentRequirementsQueryHandler(
    IStudentDocumentRequirementRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStudentDocumentRequirementsQuery, ApiResponse<List<StudentDocumentRequirementResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentDocumentRequirementResponseDTO>>> Handle(
        GetStudentDocumentRequirementsQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.GetPageAsync(actor.InstitutionId, request.DTO, cancellationToken);
        return ApiResponse<List<StudentDocumentRequirementResponseDTO>>.SuccessPaginated(
            result.Data,
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            "Student document requirements retrieved successfully.");
    }
}

public sealed class CreateStudentDocumentRequirementCommandHandler(
    IStudentDocumentRequirementRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<CreateStudentDocumentRequirementCommand, ApiResponse<StudentDocumentRequirementResponseDTO>>
{
    public async Task<ApiResponse<StudentDocumentRequirementResponseDTO>> Handle(
        CreateStudentDocumentRequirementCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.CreateAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentDocumentRequirementResponseDTO>.Success(result, "Student document requirement created successfully.");
    }
}

public sealed class UpdateStudentDocumentRequirementCommandHandler(
    IStudentDocumentRequirementRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<UpdateStudentDocumentRequirementCommand, ApiResponse<StudentDocumentRequirementResponseDTO>>
{
    public async Task<ApiResponse<StudentDocumentRequirementResponseDTO>> Handle(
        UpdateStudentDocumentRequirementCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.UpdateAsync(request.Id, actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentDocumentRequirementResponseDTO>.Success(result, "Student document requirement updated successfully.");
    }
}

public sealed class DeleteStudentDocumentRequirementCommandHandler(
    IStudentDocumentRequirementRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<DeleteStudentDocumentRequirementCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteStudentDocumentRequirementCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        await repository.DeleteAsync(request.Id, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<bool>.Success(true, "Student document requirement deactivated successfully.");
    }
}

#endregion

internal static class StudentRequestContext
{
    internal static async Task<(long InstitutionId, long StaffMemberId)> ResolveActorAsync(
        ICommonRequestService commonRequestService)
    {
        var validation = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success || validation.InstitutionId <= 0 || validation.LoggedInStaffMemberId <= 0)
        {
            throw new UnauthorizedAccessException(validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        return (validation.InstitutionId, validation.LoggedInStaffMemberId);
    }
}
