// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Host-authorized creation of subscription plans.
// ================================================================

using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOs.SubscriptionModule;
using skolenza.application.DTOS.SubscriptionModule;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.SubscriptionCmd.Handlers;

#region Command

/// <summary>
/// Represents the request to create a subscription plan.
/// </summary>
public sealed class CreateSubscriptionPlanCommand : IRequest<ApiResponse<SubscriptionActivePlanDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSubscriptionPlanCommand"/> class.
    /// </summary>
    /// <param name="requestDTO">The subscription plan data to create.</param>
    public CreateSubscriptionPlanCommand(CreateSubscriptionRequestDTO? requestDTO)
    {
        RequestDTO = requestDTO;
    }

    /// <summary>
    /// Gets the subscription plan creation request.
    /// </summary>
    public CreateSubscriptionRequestDTO? RequestDTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles Host-authorized creation of subscription plans.
/// </summary>
public sealed class CreateSubscriptionPlanCommandHandler
    : IRequestHandler<CreateSubscriptionPlanCommand, ApiResponse<SubscriptionActivePlanDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSubscriptionPlanCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides subscription plan persistence operations.</param>
    /// <param name="mapper">Maps the request and persisted entity to their appropriate models.</param>
    /// <param name="commonRequestService">Validates the current authenticated Host request.</param>
    public CreateSubscriptionPlanCommandHandler(
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
    /// Creates a subscription plan with server-controlled soft-delete defaults.
    /// </summary>
    /// <param name="request">The command to process.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The created subscription plan response.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the creation request is missing.</exception>
    public async Task<ApiResponse<SubscriptionActivePlanDTO>> Handle(
        CreateSubscriptionPlanCommand request,
        CancellationToken cancellationToken)
    {
        // Validate the authenticated Host request before processing client-provided data.
        var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        if (request?.RequestDTO is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var subscriptionPlan = _mapper.Map<SubscriptionPlan>(request.RequestDTO);
        var utcNow = DateTime.UtcNow;

        // Apply server-controlled create and soft-delete defaults before persistence.
        subscriptionPlan.AddedById = hostUserId;
        subscriptionPlan.AddedDateTime = utcNow;
        subscriptionPlan.UpdatedById = null;
        subscriptionPlan.UpdatedDateTime = null;
        subscriptionPlan.IsSoftDeleted = false;
        subscriptionPlan.DeletedById = null;
        subscriptionPlan.DeletedDateTime = null;

        var createdSubscriptionPlan = await _unitOfWork.SubscriptionRepository
            .AddSubscriptionPlanAsync(subscriptionPlan, cancellationToken);

        // Map the persisted entity to the subscription response model.
        var response = _mapper.Map<SubscriptionActivePlanDTO>(createdSubscriptionPlan);

        return ApiResponse<SubscriptionActivePlanDTO>.Success(
            response,
            AppConstants.SuccessMessages.SubscriptionPlanCreatedSuccessfully);
    }

    #endregion
}

#endregion
