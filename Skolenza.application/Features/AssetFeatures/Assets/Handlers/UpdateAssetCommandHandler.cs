// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates institution-owned assets and optional asset images.
// ================================================================

using skolenza.application.Common.Helpers;
using skolenza.application.Common.Helpers.EncryptionHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.AssetDTO.asset;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace skolenza.application.Features.AssetFeatures.Assets.Handlers;

#region Command

/// <summary>Represents the request to update an asset.</summary>
public class UpdateAssetCommand : IRequest<ApiResponse<GetAssetResponseDTO>>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateAssetCommand"/> class.</summary>
    public UpdateAssetCommand(UpdateAssetRequestDTO dto) => DTO = dto;

    /// <summary>Gets the client-supplied asset update values.</summary>
    public UpdateAssetRequestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>Handles updates to institution-owned assets.</summary>
public class UpdateAssetCommandHandler : IRequestHandler<UpdateAssetCommand, ApiResponse<GetAssetResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAssetCommandHandler> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>Initializes a new instance of the <see cref="UpdateAssetCommandHandler"/> class.</summary>
    public UpdateAssetCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateAssetCommandHandler> logger,
        IFileStorageService fileStorageService,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _fileStorageService = fileStorageService;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <inheritdoc />
    public async Task<ApiResponse<GetAssetResponseDTO>> Handle(
        UpdateAssetCommand request,
        CancellationToken cancellationToken)
    {
        string? uploadedFileKey = null;
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            if (request.DTO is null || request.DTO.Id <= 0)
            {
                throw new ValidationErrorException(
                    "Invalid request.",
                    new List<string> { "Asset Id is required." });
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
                    "Invalid Institution authorization context while updating Asset. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
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

            // Load the institution-owned entity before applying client changes.
            var asset = await _unitOfWork.AssetRepository.GetSingleRecordForInstitutionAsync(
                request.DTO.Id,
                institutionId,
                cancellationToken);
            if (asset is null)
            {
                throw new KeyNotFoundException("Asset not found.");
            }

            asset.AssetName = request.DTO.AssetName ?? asset.AssetName;
            asset.AssetTypeId = request.DTO.AssetTypeId;
            asset.Company = request.DTO.Company ?? asset.Company;
            asset.ModelNo = request.DTO.ModelNo ?? asset.ModelNo;
            asset.Size = request.DTO.Size ?? asset.Size;
            asset.Weight = request.DTO.Weight ?? asset.Weight;
            asset.Color = request.DTO.Color ?? asset.Color;
            asset.IsRepairable = request.DTO.IsRepairable;
            asset.Price = request.DTO.Price;
            asset.SerialNumber = request.DTO.SerialNumber ?? asset.SerialNumber;
            asset.Barcode = request.DTO.Barcode ?? asset.Barcode;
            asset.AssetStatusId = request.DTO.AssetStatusId;
            asset.IsAssigned = request.DTO.IsAssigned;
            asset.IsActive = request.DTO.IsActive;
            asset.PurchaseDate = request.DTO.PurchaseDate;
            asset.WarrantyExpiryDate = request.DTO.WarrantyExpiryDate;
            asset.UpdatedById = userStaffMemberId;
            asset.UpdatedDateTime = DateTime.UtcNow;
            asset.Qrcode = JsonConvert.SerializeObject(new
            {
                asset.Id,
                asset.AssetName,
                asset.AssetTypeId,
                asset.Company,
                asset.ModelNo,
                asset.SerialNumber,
                asset.Barcode,
                asset.AssetStatusId,
                asset.IsAssigned,
                asset.PurchaseDate,
                asset.WarrantyExpiryDate,
                asset.IsRepairable
            });

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

            var updatedAsset = await _unitOfWork.AssetRepository.UpdateAsync(asset, assetImagePath);
            if (updatedAsset is null)
            {
                throw new ApiException("Asset update failed.", 500);
            }

            await _unitOfWork.CommitTransactionAsync();
            return ApiResponse<GetAssetResponseDTO>.Success(updatedAsset, "Asset updated successfully.");
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

            _logger.LogError(ex, "Asset update failed.");
            throw;
        }
    }

    #endregion
}

#endregion
