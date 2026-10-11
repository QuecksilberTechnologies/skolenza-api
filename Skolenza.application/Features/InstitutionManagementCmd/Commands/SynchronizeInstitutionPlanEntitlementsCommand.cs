// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Adds missing Institution entitlement snapshot rows from the current active subscription plan.
// ================================================================

using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.Institution;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.InstitutionManagementCmd.Commands;

/// <summary>
/// Represents the explicit Host request to add missing entitlement snapshot records for a Institution's active plan.
/// </summary>
public sealed class SynchronizeInstitutionPlanEntitlementsCommand(
    SynchronizeInstitutionPlanEntitlementsRequestDTO? requestDTO)
    : IRequest<ApiResponse<SynchronizeInstitutionPlanEntitlementsResponseDTO>>
{
    /// <summary>Gets the submitted encrypted Institution selection and Host permission metadata.</summary>
    public SynchronizeInstitutionPlanEntitlementsRequestDTO? RequestDTO { get; } = requestDTO;
}

/// <summary>
/// Handles the explicit, additive synchronization from active plan mappings to Institution entitlement snapshot tables.
/// </summary>
public sealed class SynchronizeInstitutionPlanEntitlementsCommandHandler
    : IRequestHandler<SynchronizeInstitutionPlanEntitlementsCommand, ApiResponse<SynchronizeInstitutionPlanEntitlementsResponseDTO>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly IIdEncoderService _idEncoderService;

    /// <summary>Initializes a new instance of the entitlement synchronization handler.</summary>
    public SynchronizeInstitutionPlanEntitlementsCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _idEncoderService = idEncoderService;
    }

    /// <summary>
    /// Validates Host access and adds only missing active plan module and operation snapshot rows in one transaction.
    /// </summary>
    public async Task<ApiResponse<SynchronizeInstitutionPlanEntitlementsResponseDTO>> Handle(
        SynchronizeInstitutionPlanEntitlementsCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request?.RequestDTO
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);

        var hostContext = await _commonRequestService.ValidateHostUserPermissionRequestAsync();

        var institutionId = HostInstitutionIdentifierProtector.Decrypt(
            dto.InstitutionId,
            hostContext.InstitutionEncryptionKey,
            _idEncoderService);

        var institution = await _unitOfWork.InstitutionRepository
            .GetHostManagedInstitutionByIdAsync(institutionId, cancellationToken);
        if (institution is null || !institution.IsActive)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var syncResult = await _unitOfWork.InstitutionModuleConfigurationRepository
                .SynchronizeMissingActivePlanEntitlementsAsync(
                    institutionId,
                    hostContext.HostUserId,
                    cancellationToken)
                ?? throw new NotFoundException("No active subscription plan was found for this Institution.");

            // Persist newly staged entitlement rows before deriving the Institution Admin permission delta.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Add only missing permissions for the active Institution Admin role. Existing rows are retained.
            await _unitOfWork.UserRolesPermissionOnModuleRepository
                .AddMissingInstitutionAdminPermissionsAsync(
                    institutionId,
                    hostContext.HostUserId,
                    cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return ApiResponse<SynchronizeInstitutionPlanEntitlementsResponseDTO>.Success(
                new SynchronizeInstitutionPlanEntitlementsResponseDTO
                {
                    InstitutionId = HostInstitutionIdentifierProtector.Encrypt(
                        institutionId,
                        hostContext.InstitutionEncryptionKey,
                        _idEncoderService),
                    SubscriptionPlanId = syncResult.SubscriptionPlanId,
                    SourceModuleCount = syncResult.SourceModuleCount,
                    AddedModuleCount = syncResult.AddedModuleCount,
                    ExistingModuleCount = syncResult.ExistingModuleCount,
                    SourceOperationCount = syncResult.SourceOperationCount,
                    AddedOperationCount = syncResult.AddedOperationCount,
                    ExistingOperationCount = syncResult.ExistingOperationCount,
                    Modules = syncResult.Modules.ToList(),
                    Operations = syncResult.Operations.ToList()
                },
                AppConstants.SuccessMessages.InstitutionPlanEntitlementsSynchronizedSuccessfully);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
