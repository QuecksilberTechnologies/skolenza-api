// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles locality and locality-type option requests.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.Location;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.LocationCmd.Handlers;

#region Queries

public sealed record GetLocalityQuery(GetLocalityOptionRequestDTO DTO)
    : IRequest<ApiResponse<List<GetLocalityOptionResponseDTO>>>;

public sealed record GetLocalityTypeQuery(GetLocalityTypeOptionRequestDTO DTO)
    : IRequest<ApiResponse<List<GetLocalityTypeOptionResponseDTO>>>;

#endregion

#region Handlers

public sealed class GetLocalityQueryHandler
    : IRequestHandler<GetLocalityQuery, ApiResponse<List<GetLocalityOptionResponseDTO>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLocalityQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<GetLocalityOptionResponseDTO>>> Handle(
        GetLocalityQuery request,
        CancellationToken cancellationToken)
    {
        if (request.DTO == null || !request.DTO.TodaysDate.HasValue)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
        }

        if (request.DTO.DistrictId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        if (!await _unitOfWork.LocationRepository.IsActiveDistrictAsync(request.DTO.DistrictId))
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        var localities = await _unitOfWork.LocationRepository.GetLocalityOptionAsync(request.DTO);
        return ApiResponse<List<GetLocalityOptionResponseDTO>>.Success(
            localities,
            AppConstants.SuccessMessages.LocalitiesRetrieved);
    }
}

public sealed class GetLocalityTypeQueryHandler
    : IRequestHandler<GetLocalityTypeQuery, ApiResponse<List<GetLocalityTypeOptionResponseDTO>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLocalityTypeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<GetLocalityTypeOptionResponseDTO>>> Handle(
        GetLocalityTypeQuery request,
        CancellationToken cancellationToken)
    {
        if (request.DTO == null || !request.DTO.TodaysDate.HasValue)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
        }

        var localityTypes = await _unitOfWork.LocationRepository.GetLocalityTypeOptionAsync(request.DTO);
        return ApiResponse<List<GetLocalityTypeOptionResponseDTO>>.Success(
            localityTypes,
            AppConstants.SuccessMessages.LocalityTypesRetrieved);
    }
}

#endregion
