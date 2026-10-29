using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Student;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.BillingCmd;

public sealed record GetHostCountryCurrencyMastersQuery(CountryCurrencyMasterFilterDTO DTO)
    : IRequest<ApiResponse<List<CountryCurrencyMasterResponseDTO>>>;

public sealed record SaveHostCountryCurrencyMasterCommand(long? Id, CountryCurrencyMasterRequestDTO DTO)
    : IRequest<ApiResponse<CountryCurrencyMasterResponseDTO>>;

public sealed record DeactivateHostCountryCurrencyMasterCommand(long Id, PermissionRequestDTO Permission)
    : IRequest<ApiResponse<bool>>;

public sealed class GetHostCountryCurrencyMastersQueryHandler(
    IHostCountryCurrencyMasterRepository repository,
    ICommonRequestService common,
    IStoreProcedureRepository stored)
    : IRequestHandler<GetHostCountryCurrencyMastersQuery, ApiResponse<List<CountryCurrencyMasterResponseDTO>>>
{
    public async Task<ApiResponse<List<CountryCurrencyMasterResponseDTO>>> Handle(
        GetHostCountryCurrencyMastersQuery request,
        CancellationToken cancellationToken)
    {
        await HostBillingConfigurationAuthorization.ValidateAsync(
            request.DTO, common, stored, cancellationToken,
            BillingConstants.HostCountryCurrencyMasterModuleCode, "View");
        var page = await repository.GetPageAsync(request.DTO, cancellationToken);
        return ApiResponse<List<CountryCurrencyMasterResponseDTO>>.SuccessPaginated(
            page.Data, page.PageNumber, page.PageSize, page.TotalCount, page.TotalPages,
            "Country currency mappings retrieved successfully.");
    }
}

public sealed class SaveHostCountryCurrencyMasterCommandHandler(
    IHostCountryCurrencyMasterRepository repository,
    ICommonRequestService common,
    IStoreProcedureRepository stored)
    : IRequestHandler<SaveHostCountryCurrencyMasterCommand, ApiResponse<CountryCurrencyMasterResponseDTO>>
{
    public async Task<ApiResponse<CountryCurrencyMasterResponseDTO>> Handle(
        SaveHostCountryCurrencyMasterCommand request,
        CancellationToken cancellationToken)
    {
        var operation = request.Id.HasValue ? "Update" : "Add";
        var hostUserId = await HostBillingConfigurationAuthorization.ValidateAsync(
            request.DTO, common, stored, cancellationToken,
            BillingConstants.HostCountryCurrencyMasterModuleCode, operation);
        var result = await repository.SaveAsync(request.Id, request.DTO, hostUserId, cancellationToken);
        return ApiResponse<CountryCurrencyMasterResponseDTO>.Success(
            result, request.Id.HasValue ? "Country currency mapping updated successfully." : "Country currency mapping created successfully.");
    }
}

public sealed class DeactivateHostCountryCurrencyMasterCommandHandler(
    IHostCountryCurrencyMasterRepository repository,
    ICommonRequestService common,
    IStoreProcedureRepository stored)
    : IRequestHandler<DeactivateHostCountryCurrencyMasterCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeactivateHostCountryCurrencyMasterCommand request,
        CancellationToken cancellationToken)
    {
        var hostUserId = await HostBillingConfigurationAuthorization.ValidateAsync(
            request.Permission, common, stored, cancellationToken,
            BillingConstants.HostCountryCurrencyMasterModuleCode, "Delete");
        await repository.DeactivateAsync(request.Id, hostUserId, cancellationToken);
        return ApiResponse<bool>.Success(true, "Country currency mapping deactivated successfully.");
    }
}
