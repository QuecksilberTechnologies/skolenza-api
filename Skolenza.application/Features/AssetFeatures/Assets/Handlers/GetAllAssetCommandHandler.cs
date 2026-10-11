// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves institution-owned assets from authenticated requests.
// ================================================================

using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.DTOS.AssetDTO.asset;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace skolenza.application.Features.AssetFeatures.Assets.Handlers;

#region Query

/// <summary>Represents the request to retrieve assets.</summary>
public class GetAllAssetCommand : IRequest<ApiResponse<List<GetAssetResponseDTO>>>
{
    /// <summary>Initializes a new instance of the <see cref="GetAllAssetCommand"/> class.</summary>
    public GetAllAssetCommand(GetAssetRequestDTO dto) => DTO = dto;

    /// <summary>Gets the client-supplied asset filters.</summary>
    public GetAssetRequestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>Handles retrieval of institution-owned assets.</summary>
public class GetAllAssetCommandHandler
    : IRequestHandler<GetAllAssetCommand, ApiResponse<List<GetAssetResponseDTO>>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdEncoderService _idEncoderService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IConfiguration _configuration;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>Initializes a new instance of the <see cref="GetAllAssetCommandHandler"/> class.</summary>
    public GetAllAssetCommandHandler(
        IUnitOfWork unitOfWork,
        IIdEncoderService idEncoderService,
        IFileStorageService fileStorageService,
        IConfiguration configuration,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _idEncoderService = idEncoderService;
        _fileStorageService = fileStorageService;
        _configuration = configuration;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <inheritdoc />
    public async Task<ApiResponse<List<GetAssetResponseDTO>>> Handle(
        GetAllAssetCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO is null)
        {
            throw new ValidationErrorException("Invalid request.");
        }

        // Resolve the trusted institution context separately from client filters.
        var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(validation.ErrorMessage);
        }

        var pagedAssets = await _unitOfWork.AssetRepository.GetAssetsByFilterAsync(
            validation.InstitutionId,
            request.DTO,
            cancellationToken);
        var encryptedList = ProjectionHelper.ToGetAssetResponseDTOs(
            pagedAssets.Data,
            _idEncoderService,
            validation.Claims.InstitutionEncriptionKey,
            _configuration,
            _fileStorageService);

        return ApiResponse<List<GetAssetResponseDTO>>.SuccessPaginatedOnly(
            Data: encryptedList,
            PageNumber: pagedAssets.PageNumber,
            PageSize: pagedAssets.PageSize,
            TotalRecords: pagedAssets.TotalCount,
            TotalPages: pagedAssets.TotalPages,
            Message: "Assets fetched successfully.",
            HasUploadedAll: pagedAssets.HasUploadedAll);
    }

    #endregion
}

#endregion
