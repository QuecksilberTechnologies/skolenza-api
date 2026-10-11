// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves active public subscription plans without requiring authentication.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOs.SubscriptionModule;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.SubscriptionCmd.Handlers;

#region Query

/// <summary>
/// Represents the public request to retrieve active subscription plans.
/// </summary>
public sealed class GetSubscriptionPlanQuery : IRequest<ApiResponse<List<SubscriptionActivePlanDTO>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSubscriptionPlanQuery"/> class.
    /// </summary>
    /// <param name="requestDTO">The subscription plan filter request.</param>
    public GetSubscriptionPlanQuery(SubscriptionPlanRequestDTO? requestDTO)
    {
        RequestDTO = requestDTO;
    }

    /// <summary>
    /// Gets the subscription plan filter request.
    /// </summary>
    public SubscriptionPlanRequestDTO? RequestDTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles public subscription plan queries.
/// </summary>
public sealed class GetSubscriptionPlanQueryHandler
    : IRequestHandler<GetSubscriptionPlanQuery, ApiResponse<List<SubscriptionActivePlanDTO>>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSubscriptionPlanQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides subscription plan query operations.</param>
    public GetSubscriptionPlanQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    #endregion

    #region Handle

    /// <summary>
/// Retrieves active, non-deleted subscription plans for pre-login users.
    /// </summary>
    /// <param name="request">The query to process.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The filtered subscription plan response.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the query request is missing.</exception>
    public async Task<ApiResponse<List<SubscriptionActivePlanDTO>>> Handle(
        GetSubscriptionPlanQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request?.RequestDTO is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var plans = await _unitOfWork.SubscriptionRepository
            .GetAllPlansAsync(true, cancellationToken);

        // Build the application response in the handler layer.
        return ApiResponse<List<SubscriptionActivePlanDTO>>.Success(
            plans,
            AppConstants.SuccessMessages.SubscriptionPlansRetrievedSuccessfully);
    }

    #endregion
}

#endregion
