// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Safely soft-deletes Subscription Plans and removes their owned Module mapping records.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.SubscriptionModule;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.SubscriptionCmd.Handlers;

#region Command

/// <summary>
/// Represents the request to soft delete a subscription plan.
/// </summary>
public class DeleteSubscriptionPlanCommand : IRequest<ApiResponse<bool>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSubscriptionPlanCommand"/> class.
    /// </summary>
    /// <param name="request">The subscription plan deletion request.</param>
    public DeleteSubscriptionPlanCommand(DeleteSubscriptionPlanRequestDTO request)
    {
        Request = request;
    }

    /// <summary>
    /// Gets the subscription plan deletion request.
    /// </summary>
    public DeleteSubscriptionPlanRequestDTO Request { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles Host-authorized soft deletion of subscription plans.
/// </summary>
public class DeleteSubscriptionPlanCommandHandler
    : IRequestHandler<DeleteSubscriptionPlanCommand, ApiResponse<bool>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly ILogger<DeleteSubscriptionPlanCommandHandler> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSubscriptionPlanCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides subscription plan persistence operations.</param>
    /// <param name="commonRequestService">Validates the current authenticated Host user.</param>
    /// <param name="logger">The logger used for mapping cleanup diagnostics.</param>
    public DeleteSubscriptionPlanCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        ILogger<DeleteSubscriptionPlanCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _logger = logger;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Soft deletes a Subscription Plan and permanently removes all owned Module mappings atomically.
    /// </summary>
    /// <param name="request">The delete command to process.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>A successful response when the plan is soft deleted.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the command contains an invalid identifier.</exception>
    /// <exception cref="NotFoundException">Thrown when the requested plan is unavailable or already deleted.</exception>
    public async Task<ApiResponse<bool>> Handle(
        DeleteSubscriptionPlanCommand request,
        CancellationToken cancellationToken)
    {
        // Validate the authenticated Host user before processing client-provided data.
        var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        if (request?.Request is null || request.Request.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        if (hostUserId <= 0 || hostUserId > int.MaxValue)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        // Load the active subscription plan targeted for deletion.
        var subscriptionPlan = await _unitOfWork.SubscriptionRepository
            .GetNonDeletedSubscriptionPlanByIdAsync(request.Request.Id, cancellationToken);

        if (subscriptionPlan is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.SubscriptionPlanNotFound);
        }

        var transactionStarted = false;
        var utcNow = DateTime.UtcNow;
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            // InstitutionEnabledModule/InstitutionEnabledOperation are independent institution snapshots.
            // Removing a retired plan's source mappings must not remove or invalidate
            // any entitlement rows already assigned to a institution.
            var deletedMappingCount = await _unitOfWork.PlanModuleMappingRepository
                .DeleteAllBySubscriptionPlanIdAsync(subscriptionPlan.Id, cancellationToken);

            // Apply server-controlled soft-delete audit values without physically deleting the plan.
            subscriptionPlan.IsSoftDeleted = true;
            subscriptionPlan.DeletedById = (int)hostUserId;
            subscriptionPlan.DeletedDateTime = utcNow;

            await _unitOfWork.SubscriptionRepository
                .SoftDeleteSubscriptionPlanAsync(subscriptionPlan, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            transactionStarted = false;

            _logger.LogInformation(
                "Soft-deleted Subscription Plan and removed owned Module mappings. SubscriptionPlanId: {SubscriptionPlanId}; DeletedMappings: {DeletedMappings}.",
                subscriptionPlan.Id,
                deletedMappingCount);

            return ApiResponse<bool>.Success(
                true,
                AppConstants.SuccessMessages.SubscriptionPlanDeletedSuccessfully);
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
