// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves the eligible Module hierarchy and current mapping state for Subscription Plans.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOs.PlanModule;
using skolenza.application.Exceptions;
using skolenza.application.Features.PlanModuleMappingCmd;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.PlanModuleMappingCmd.Handlers;

#region Query

/// <summary>
/// Represents the request to retrieve selectable Module options for a Subscription Plan.
/// </summary>
public sealed class GetPlanModuleMappingOptionsQuery
    : IRequest<ApiResponse<PlanModuleMappingOptionsResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetPlanModuleMappingOptionsQuery"/> class.
    /// </summary>
    /// <param name="subscriptionPlanId">The Subscription Plan identifier.</param>
    public GetPlanModuleMappingOptionsQuery(int subscriptionPlanId)
    {
        SubscriptionPlanId = subscriptionPlanId;
    }

    /// <summary>
    /// Gets the Subscription Plan identifier.
    /// </summary>
    public int SubscriptionPlanId { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles Host-authorized retrieval of selectable Subscription Plan Module options.
/// </summary>
public sealed class GetPlanModuleMappingOptionsQueryHandler
    : IRequestHandler<GetPlanModuleMappingOptionsQuery, ApiResponse<PlanModuleMappingOptionsResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPlanModuleMappingOptionsQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides Subscription Plan and Module mapping query operations.</param>
    /// <param name="commonRequestService">Validates the authenticated Host request.</param>
    public GetPlanModuleMappingOptionsQueryHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Retrieves the eligible Institution- and Common-scope Module hierarchy with current active mapping selections.
    /// </summary>
    /// <param name="request">The query to process.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The eligible Module hierarchy with current mapping selections.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the Subscription Plan identifier is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when the Subscription Plan is unavailable or soft deleted.</exception>
    public async Task<ApiResponse<PlanModuleMappingOptionsResponseDTO>> Handle(
        GetPlanModuleMappingOptionsQuery request,
        CancellationToken cancellationToken)
    {
        // Validate the Host identity before exposing Subscription configuration.
        await _commonRequestService.ValidateHostUserRequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        if (request.SubscriptionPlanId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var subscriptionPlan = await _unitOfWork.SubscriptionRepository
            .GetNonDeletedSubscriptionPlanByIdAsync(request.SubscriptionPlanId, cancellationToken);

        if (subscriptionPlan is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.SubscriptionPlanNotFound);
        }

        // Load hierarchy data and current selections with two read-only, set-based queries.
        var eligibleModules = await _unitOfWork.PlanModuleMappingRepository
            .GetEligibleModulesForPlanMappingAsync(cancellationToken);
        var mappedModuleIds = await _unitOfWork.PlanModuleMappingRepository
            .GetActiveMappedModuleIdsAsync(request.SubscriptionPlanId, cancellationToken);
        var hierarchy = PlanModuleHierarchy.Create(eligibleModules);

        var response = new PlanModuleMappingOptionsResponseDTO
        {
            SubscriptionPlanId = request.SubscriptionPlanId,
            Modules = hierarchy.BuildOptions(mappedModuleIds.ToHashSet())
        };

        return ApiResponse<PlanModuleMappingOptionsResponseDTO>.Success(
            response,
            AppConstants.SuccessMessages.SubscriptionPlanModuleOptionsRetrievedSuccessfully);
    }

    #endregion
}

#endregion
