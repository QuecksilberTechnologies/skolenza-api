// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Creates institution-owned assets and optional asset images.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Helpers.EncryptionHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.AssetDTO.asset;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace skolenza.application.Features.AssetFeatures.Assets.Handlers;

#region Command

/// <summary>Represents the request to create an asset.</summary>
public class AddAssetCommand : IRequest<ApiResponse<GetAssetResponseDTO>>
{
    /// <summary>Initializes a new instance of the <see cref="AddAssetCommand"/> class.</summary>
    public AddAssetCommand(AddAssetRequestDTO dto) => DTO = dto;

    /// <summary>Gets the client-supplied asset values.</summary>
    public AddAssetRequestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>Handles creation of institution-owned assets.</summary>
public class AddAssetCommandHandler : IRequestHandler<AddAssetCommand, ApiResponse<GetAssetResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AddAssetCommandHandler> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICommonRequestService _commonRequestService;
    private readonly IConfiguration _configuration;

    #endregion

    #region Constructor

    /// <summary>Initializes a new instance of the <see cref="AddAssetCommandHandler"/> class.</summary>
    public AddAssetCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AddAssetCommandHandler> logger,
        IFileStorageService fileStorageService,
        ICommonRequestService commonRequestService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _fileStorageService = fileStorageService;
        _commonRequestService = commonRequestService;
        _configuration = configuration;
    }

    #endregion

    #region Handle

    /// <inheritdoc />
    public async Task<ApiResponse<GetAssetResponseDTO>> Handle(AddAssetCommand request, CancellationToken cancellationToken)
    {
        string? uploadedFileKey = null;
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            if (request.DTO is null)
            {
                throw new ValidationErrorException("Invalid request data.");
            }

            #region Institution Request Validation

            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
            if (!validation.Success)
            {
                throw new UnauthorizedAccessException(
                    validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
            }

            #endregion

            #region Trusted Request Context

            long userStaffMemberId = validation.LoggedInStaffMemberId;
            long institutionId = validation.InstitutionId;
            int tokenRoleId = validation.RoleId;

            if (userStaffMemberId <= 0 || institutionId <= 0 || tokenRoleId <= 0)
            {
                _logger.LogWarning(
                    "Invalid Institution authorization context while creating Asset. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
                    institutionId,
                    userStaffMemberId,
                    tokenRoleId);
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            #endregion

            #region Runtime Permission Validation

            var permissionResult = await _unitOfWork.StoreProcedureRepository
                .CheckInstitutionStaffMemberPermissionAsync(
                    institutionId,
                    userStaffMemberId,
                    tokenRoleId,
                    request.DTO.ModuleId,
                    request.DTO.OperationId,
                    cancellationToken);

            InstitutionRuntimePermissionValidator.EnsureAllowed(permissionResult);

            #endregion

            // Map client-editable values and apply server-controlled context.
            var asset = _mapper.Map<Asset>(request.DTO);
            asset.InstitutionId = institutionId;
            asset.AddedById = userStaffMemberId;
            asset.AddedDateTime = DateTime.UtcNow;
            asset.IsActive = true;
            asset.IsSoftDeleted = false;
            asset.UpdatedDateTime = null;
            asset.DeletedDateTime = null;

            var assetStatus = await _unitOfWork.AssetStatusRepository.GetByIdForInstitutionAsync(
                asset.AssetStatusId,
                institutionId,
                cancellationToken);
            if (assetStatus is null)
            {
                throw new ValidationErrorException("Invalid AssetStatusId.");
            }

            string? assetImagePath = null;
            if (request.DTO.AssetImageFile is { Length: > 0 })
            {
                var cleanName = EncryptionSanitizer.CleanEncodedInput(request.DTO.AssetName ?? "asset")
                    .ToLowerInvariant()
                    .Replace(" ", "_");
                var fileName = $"asset-{cleanName}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                var folderPath = $"{ConstantValues.InstitutionFolder}-{institutionId}/{ConstantValues.AssetsFolder}";
                uploadedFileKey = await _fileStorageService.UploadFileAsync(
                    request.DTO.AssetImageFile,
                    folderPath,
                    fileName);
                assetImagePath = uploadedFileKey;
            }

            var insertedAsset = await _unitOfWork.AssetRepository.AddAsync(asset, assetImagePath!);
            if (insertedAsset is null)
            {
                throw new ApiException("Asset creation failed.", 500);
            }

            var qrJson = JsonConvert.SerializeObject(new
            {
                insertedAsset.AssetId,
                insertedAsset.AssetName,
                insertedAsset.AssetTypeId,
                insertedAsset.Company,
                insertedAsset.ModelNo,
                insertedAsset.SerialNumber,
                insertedAsset.Barcode,
                insertedAsset.AssetStatusId,
                insertedAsset.StatusName,
                insertedAsset.PurchaseDate,
                insertedAsset.WarrantyExpiryDate,
                insertedAsset.IsRepairable,
                insertedAsset.IsAssigned
            });
            await _unitOfWork.AssetRepository.UpdateQrCodeAsync(insertedAsset.AssetId, qrJson);

            var baseUrl = _configuration["FileSettings:BaseUrl"] ?? string.Empty;
            insertedAsset.AssetImagePath = string.IsNullOrEmpty(insertedAsset.AssetImagePath)
                ? null
                : $"{baseUrl}{insertedAsset.AssetImagePath}";

            await _unitOfWork.CommitTransactionAsync();
            return ApiResponse<GetAssetResponseDTO>.Success(insertedAsset, "Asset created successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            if (!string.IsNullOrEmpty(uploadedFileKey))
            {
                try
                {
                    await _fileStorageService.DeleteFileAsync(uploadedFileKey);
                }
                catch (Exception deleteException)
                {
                    _logger.LogError(deleteException, "Failed to delete the rolled-back asset image.");
                }
            }

            _logger.LogError(ex, "Asset creation failed.");
            throw;
        }
    }

    #endregion
}

#endregion
