using MediatR;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Student;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;

namespace skolenza.application.Features.StudentCmd;

#region Requests

public sealed record GetStudentEducationIdentifierMastersQuery(StudentEducationIdentifierMasterFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentEducationIdentifierMasterResponseDTO>>>;

public sealed record CreateStudentEducationIdentifierMasterCommand(StudentEducationIdentifierMasterRequestDTO DTO)
    : IRequest<ApiResponse<StudentEducationIdentifierMasterResponseDTO>>;

public sealed record UpdateStudentEducationIdentifierMasterCommand(
    long Id,
    StudentEducationIdentifierMasterRequestDTO DTO)
    : IRequest<ApiResponse<StudentEducationIdentifierMasterResponseDTO>>;

public sealed record DeleteStudentEducationIdentifierMasterCommand(
    long Id,
    StudentEducationIdentifierMasterDeleteDTO DTO)
    : IRequest<ApiResponse<bool>>;

#endregion

#region Handlers

public sealed class GetStudentEducationIdentifierMastersQueryHandler(
    IStudentEducationIdentifierMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStudentEducationIdentifierMastersQuery, ApiResponse<List<StudentEducationIdentifierMasterResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentEducationIdentifierMasterResponseDTO>>> Handle(
        GetStudentEducationIdentifierMastersQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.GetPageAsync(actor.InstitutionId, request.DTO, cancellationToken);
        return ApiResponse<List<StudentEducationIdentifierMasterResponseDTO>>.SuccessPaginated(
            result.Data,
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            "Student education identifier masters retrieved successfully.");
    }
}

public sealed class CreateStudentEducationIdentifierMasterCommandHandler(
    IStudentEducationIdentifierMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<CreateStudentEducationIdentifierMasterCommand, ApiResponse<StudentEducationIdentifierMasterResponseDTO>>
{
    public async Task<ApiResponse<StudentEducationIdentifierMasterResponseDTO>> Handle(
        CreateStudentEducationIdentifierMasterCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.SaveAsync(
            null, actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentEducationIdentifierMasterResponseDTO>.Success(
            result, "Student education identifier master created successfully.");
    }
}

public sealed class UpdateStudentEducationIdentifierMasterCommandHandler(
    IStudentEducationIdentifierMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<UpdateStudentEducationIdentifierMasterCommand, ApiResponse<StudentEducationIdentifierMasterResponseDTO>>
{
    public async Task<ApiResponse<StudentEducationIdentifierMasterResponseDTO>> Handle(
        UpdateStudentEducationIdentifierMasterCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.SaveAsync(
            request.Id, actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentEducationIdentifierMasterResponseDTO>.Success(
            result, "Student education identifier master updated successfully.");
    }
}

public sealed class DeleteStudentEducationIdentifierMasterCommandHandler(
    IStudentEducationIdentifierMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<DeleteStudentEducationIdentifierMasterCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteStudentEducationIdentifierMasterCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        await repository.DeleteAsync(request.Id, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<bool>.Success(true, "Student education identifier master deactivated successfully.");
    }
}

#endregion
