// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates Subscription Plan state and synchronizes dependent Module mappings atomically.
// ================================================================

using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOs.SubscriptionModule;
using skolenza.application.DTOS.SubscriptionModule;
using skolenza.application.Exceptions;
using skolenza.application.Features.PlanModuleMappingCmd;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.SubscriptionCmd.Handlers;

#region Command

/// <summary>
/// Represents the request to update a subscription plan selected by a route identifier.
/// </summary>
public sealed class UpdateSubscriptionPlanCommand : IRequest<ApiResponse<SubscriptionActivePlanDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSubscriptionPlanCommand"/> class.
    /// </summary>
    /// <param name="subscriptionPlanId">The subscription plan identifier from the route.</param>
    /// <param name="requestDTO">The editable subscription plan data.</param>
    public UpdateSubscriptionPlanCommand(
        long subscriptionPlanId,
        UpdateSubscriptionRequestDTO? requestDTO)
    {
        SubscriptionPlanId = subscriptionPlanId;
        RequestDTO = requestDTO;
    }

    /// <summary>
    /// Gets the subscription plan identifier from the route.
    /// </summary>
    public long SubscriptionPlanId { get; }

    /// <summary>
    /// Gets the editable subscription plan data.
    /// </summary>
    public UpdateSubscriptionRequestDTO? RequestDTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles Host-authorized updates of existing, non-deleted subscription plans.
/// </summary>
public sealed class UpdateSubscriptionPlanCommandHandler
    : IRequestHandler<UpdateSubscriptionPlanCommand, ApiResponse<SubscriptionActivePlanDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSubscriptionPlanCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides subscription plan persistence operations.</param>
    /// <param name="mapper">Maps editable request values onto the loaded entity and maps the response.</param>
    /// <param name="commonRequestService">Validates the current authenticated Host request.</param>
    public UpdateSubscriptionPlanCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Updates an existing Subscription Plan and synchronizes Module mappings when its active state changes.
    /// </summary>
    /// <param name="request">The command to process.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The updated subscription plan response.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the request or route identifier is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when the plan is unavailable or already soft deleted.</exception>
    public async Task<ApiResponse<SubscriptionActivePlanDTO>> Handle(
        UpdateSubscriptionPlanCommand request,
        CancellationToken cancellationToken)
    {
        // Validate the authenticated Host request before processing client-provided data.
        var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        if (request?.RequestDTO is null ||
            request.SubscriptionPlanId <= 0 ||
            request.SubscriptionPlanId > int.MaxValue ||
            hostUserId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        // Load the existing non-deleted entity before mapping editable values.
        var subscriptionPlan = await _unitOfWork.SubscriptionRepository
            .GetNonDeletedSubscriptionPlanByIdAsync((int)request.SubscriptionPlanId, cancellationToken);

        if (subscriptionPlan is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.SubscriptionPlanNotFound);
        }

        var wasActive = subscriptionPlan.IsActive;

        // AutoMapper ignores the entity identifier and all server-controlled delete audit fields.
        _mapper.Map(request.RequestDTO, subscriptionPlan);
        var utcNow = DateTime.UtcNow;
        subscriptionPlan.UpdatedById = hostUserId;
        subscriptionPlan.UpdatedDateTime = utcNow;

        var isStatusChanged = wasActive != subscriptionPlan.IsActive;
        IReadOnlyCollection<int> eligibleModuleIds = Array.Empty<int>();

        if (isStatusChanged && subscriptionPlan.IsActive)
        {
            // Build the current eligibility set before reactivating historical mapping rows.
            var eligibleModules = await _unitOfWork.PlanModuleMappingRepository
                .GetEligibleModulesForPlanMappingAsync(cancellationToken);
            eligibleModuleIds = PlanModuleHierarchy.Create(eligibleModules).ModuleIds;
        }

        var transactionStarted = false;
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            if (isStatusChanged)
            {
                // Deactivate every mapping when disabled; reactivate only currently eligible mappings when enabled.
                await _unitOfWork.PlanModuleMappingRepository
                    .SynchronizePlanMappingStatusAsync(
                        subscriptionPlan.Id,
                        subscriptionPlan.IsActive,
                        eligibleModuleIds,
                        hostUserId,
                        utcNow,
                        cancellationToken);
            }

            var updatedSubscriptionPlan = await _unitOfWork.SubscriptionRepository
                .UpdateSubscriptionPlanAsync(subscriptionPlan, cancellationToken);

            // Persist the plan and every prepared mapping change in one transaction.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            transactionStarted = false;

            // Map the persisted entity to the subscription response model.
            var response = _mapper.Map<SubscriptionActivePlanDTO>(updatedSubscriptionPlan);

            return ApiResponse<SubscriptionActivePlanDTO>.Success(
                response,
                AppConstants.SuccessMessages.SubscriptionPlanUpdatedSuccessfully);
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
