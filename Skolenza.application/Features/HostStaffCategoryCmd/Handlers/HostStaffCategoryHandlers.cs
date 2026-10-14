using MediatR;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Staff;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;

namespace skolenza.application.Features.HostStaffCategoryCmd.Handlers;

public sealed record GetHostStaffCategoriesQuery(PermissionRequestDTO Permission)
    : IRequest<ApiResponse<IReadOnlyList<HostStaffCategoryResponseDTO>>>;

public sealed record SaveHostStaffCategoryCommand(SaveHostStaffCategoryRequestDTO DTO)
    : IRequest<ApiResponse<HostStaffCategoryResponseDTO>>;

internal static class HostStaffCategoryAuthorization
{
    public static async Task ValidateAsync(PermissionRequestDTO permission, string operation,
        ICommonRequestService commonRequestService, IStoreProcedureRepository storeProcedureRepository,
        CancellationToken cancellationToken)
    {
        var moduleCode = await commonRequestService.GetModuleCodeAsync(permission.ModuleId);
        var operationName = await commonRequestService.GetActiveOperationNameAsync(permission.OperationId);
        if (!string.Equals(moduleCode, AppConstants.StaffCategories.HostModuleCode, StringComparison.Ordinal) ||
            !string.Equals(operationName, operation, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        await HostRuntimePermissionValidator.ValidateAsync(commonRequestService, storeProcedureRepository,
            permission.ModuleId, permission.OperationId, cancellationToken);
    }
}

public sealed class GetHostStaffCategoriesQueryHandler(IStaffCategoryRepository repository,
    ICommonRequestService commonRequestService, IStoreProcedureRepository storeProcedureRepository)
    : IRequestHandler<GetHostStaffCategoriesQuery, ApiResponse<IReadOnlyList<HostStaffCategoryResponseDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<HostStaffCategoryResponseDTO>>> Handle(
        GetHostStaffCategoriesQuery request, CancellationToken cancellationToken)
    {
        await HostStaffCategoryAuthorization.ValidateAsync(request.Permission, "View", commonRequestService,
            storeProcedureRepository, cancellationToken);
        return ApiResponse<IReadOnlyList<HostStaffCategoryResponseDTO>>.Success(
            await repository.GetAllAsync(cancellationToken), "Staff Categories retrieved successfully.");
    }
}

public sealed class SaveHostStaffCategoryCommandHandler(IStaffCategoryRepository repository,
    ICommonRequestService commonRequestService, IStoreProcedureRepository storeProcedureRepository)
    : IRequestHandler<SaveHostStaffCategoryCommand, ApiResponse<HostStaffCategoryResponseDTO>>
{
    public async Task<ApiResponse<HostStaffCategoryResponseDTO>> Handle(
        SaveHostStaffCategoryCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DTO.Code) || string.IsNullOrWhiteSpace(request.DTO.Name))
        {
            throw new ValidationErrorException("Code and name are required.");
        }
        await HostStaffCategoryAuthorization.ValidateAsync(request.DTO, request.DTO.Id.HasValue ? "Update" : "Add",
            commonRequestService, storeProcedureRepository, cancellationToken);
        return ApiResponse<HostStaffCategoryResponseDTO>.Success(await repository.SaveAsync(request.DTO, cancellationToken),
            "Staff Category saved successfully.");
    }
}
