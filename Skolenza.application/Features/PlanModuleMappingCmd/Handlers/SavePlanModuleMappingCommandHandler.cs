// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Validates and synchronizes eligible Module assignments for Subscription Plans.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOs.PlanModule;
using skolenza.application.Exceptions;
using skolenza.application.Features.PlanModuleMappingCmd;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.PlanModuleMappingCmd.Handlers;

#region Command

/// <summary>
/// Represents the request to atomically synchronize Module selections for a Subscription Plan.
/// </summary>
public sealed class SavePlanModuleMappingCommand
    : IRequest<ApiResponse<SavePlanModuleMappingResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SavePlanModuleMappingCommand"/> class.
    /// </summary>
    /// <param name="requestDTO">The requested Subscription Plan Module selections.</param>
    public SavePlanModuleMappingCommand(SavePlanModuleMappingRequestDTO? requestDTO)
    {
        RequestDTO = requestDTO;
    }

    /// <summary>
    /// Gets the requested Subscription Plan Module selections.
    /// </summary>
    public SavePlanModuleMappingRequestDTO? RequestDTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles Host-authorized, atomic delta synchronization of Subscription Plan Module mappings.
/// </summary>
public sealed class SavePlanModuleMappingCommandHandler
    : IRequestHandler<SavePlanModuleMappingCommand, ApiResponse<SavePlanModuleMappingResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly ILogger<SavePlanModuleMappingCommandHandler> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SavePlanModuleMappingCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides transactional Subscription Plan mapping persistence operations.</param>
    /// <param name="commonRequestService">Validates the authenticated Host request.</param>
    /// <param name="logger">The logger used for mapping synchronization diagnostics.</param>
    public SavePlanModuleMappingCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        ILogger<SavePlanModuleMappingCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _logger = logger;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Validates and synchronizes eligible Module selections for one non-deleted Subscription Plan.
    /// </summary>
    /// <param name="request">The command to process.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The applied Module mapping delta summary.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the request, Host identity, or Module selection is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when the Subscription Plan is unavailable or soft deleted.</exception>
    public async Task<ApiResponse<SavePlanModuleMappingResponseDTO>> Handle(
        SavePlanModuleMappingCommand request,
        CancellationToken cancellationToken)
    {
        // Validate the Host identity before modifying Subscription configuration.
        var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        if (request.RequestDTO is null || request.RequestDTO.SubscriptionPlanId <= 0 || hostUserId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var subscriptionPlan = await _unitOfWork.SubscriptionRepository
            .GetNonDeletedSubscriptionPlanByIdAsync(request.RequestDTO.SubscriptionPlanId, cancellationToken);

        if (subscriptionPlan is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.SubscriptionPlanNotFound);
        }

        // Load only Modules that are currently eligible for Plan assignment.
        var eligibleModules = await _unitOfWork.PlanModuleMappingRepository
            .GetEligibleModulesForPlanMappingAsync(cancellationToken);
        var hierarchy = PlanModuleHierarchy.Create(eligibleModules);
        var requestedModuleIds = request.RequestDTO.ModuleIds ?? Array.Empty<int>();

        if (!hierarchy.TryExpandSelection(requestedModuleIds, out var selectedModuleIds))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidSubscriptionPlanModule);
        }

        var transactionStarted = false;
        var utcNow = DateTime.UtcNow;
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            var response = await _unitOfWork.PlanModuleMappingRepository
                .SynchronizeMappingsAsync(
                    subscriptionPlan.Id,
                    selectedModuleIds,
                    request.RequestDTO.Remark,
                    hostUserId,
                    utcNow,
                    cancellationToken);

            // Persist the complete mapping delta once within the transaction boundary.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            transactionStarted = false;

            _logger.LogInformation(
                "Synchronized Subscription Plan Module mappings. SubscriptionPlanId: {SubscriptionPlanId}; Selected: {SelectedCount}; Added: {AddedCount}; Reactivated: {ReactivatedCount}; Deactivated: {DeactivatedCount}.",
                response.SubscriptionPlanId,
                response.SelectedModuleCount,
                response.AddedCount,
                response.ReactivatedCount,
                response.DeactivatedCount);

            return ApiResponse<SavePlanModuleMappingResponseDTO>.Success(
                response,
                AppConstants.SuccessMessages.SubscriptionPlanModuleMappingSavedSuccessfully);
        }
        catch
        {
            if (transactionStarted)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }

            throw;
        }
    }

    #endregion
}

#endregion
