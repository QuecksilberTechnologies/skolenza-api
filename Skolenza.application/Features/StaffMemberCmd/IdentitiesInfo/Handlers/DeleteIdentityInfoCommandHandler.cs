// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Soft deletes one StaffMember identity record.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.Sensitive;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.IdentitiesInfo.Handlers;

#region Command

public sealed class DeleteIdentityInfoCommand : IRequest<ApiResponse<bool>>
{
    public DeleteIdentityRequestDTO DTO { get; }

    public DeleteIdentityInfoCommand(DeleteIdentityRequestDTO dto)
    {
        DTO = dto;
    }
}

#endregion

#region Handler

public sealed class DeleteIdentityInfoCommandHandler
    : IRequestHandler<DeleteIdentityInfoCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<DeleteIdentityInfoCommandHandler> _logger;

    public DeleteIdentityInfoCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IFileStorageService fileStorageService,
        ILogger<DeleteIdentityInfoCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteIdentityInfoCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

        if (!validation.Success)
            throw new UnauthorizedAccessException(validation.ErrorMessage);

        if (request?.DTO == null || request.DTO.StaffMemberIdentityId <= 0)
            throw new ValidationErrorException("Invalid identity id.");

        var identity = await _unitOfWork.StaffMemberIdentityRepository
            .GetSingleRecordAsync(request.DTO.StaffMemberIdentityId, true)
            ?? throw new ApiException("Identity record not found.", 404);

        if (!await _commonRequestService.CanAccessStaffMemberDataAsync(
                validation,
                identity.StaffMemberId,
                StaffMemberDataAccessRequirement.PersonalDetails,
                cancellationToken))
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

        if (validation.RoleTypeId != ConstantValues.RoleTypeAdmin &&
            (identity.IsInfoVerified || !identity.IsEditAllowed))
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            identity.IsActive = false;
            identity.IsSoftDeleted = true;
            identity.SoftDeletedById = validation.UserStaffMemberId;
            identity.DeletedDateTime = DateTime.UtcNow;

            if (!await _unitOfWork.StaffMemberIdentityRepository.SoftDeleteAsync(
                    identity,
                    cancellationToken))
                throw new ApiException("Identity delete failed.", 500);

            await _unitOfWork.CommitTransactionAsync();

            if (!string.IsNullOrWhiteSpace(identity.DocumentFilePath))
            {
                try
                {
                    await _fileStorageService.DeleteFileAsync(identity.DocumentFilePath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to delete identity file after soft delete");
                }
            }

            return ApiResponse<bool>.Success(true, "Identity deleted successfully.");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}

#endregion
