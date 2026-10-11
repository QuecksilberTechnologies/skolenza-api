// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves filtered and paginated Subscription Plans for authenticated Host administration.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOs.SubscriptionModule;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.SubscriptionCmd.Handlers;

#region Query

/// <summary>
/// Represents the Host request to retrieve filtered and paginated subscription plans.
/// </summary>
public sealed class GetHostSubscriptionPlansQuery
    : IRequest<PagedApiResponse<SubscriptionActivePlanDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetHostSubscriptionPlansQuery"/> class.
    /// </summary>
    /// <param name="requestDTO">The Host subscription-plan filters and paging request.</param>
    public GetHostSubscriptionPlansQuery(HostSubscriptionPlanListRequestDTO? requestDTO)
    {
        RequestDTO = requestDTO;
    }

    /// <summary>
    /// Gets the Host subscription-plan filters and paging request.
    /// </summary>
    public HostSubscriptionPlanListRequestDTO? RequestDTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles authenticated Host retrieval of filtered and paginated subscription plans.
/// </summary>
public sealed class GetHostSubscriptionPlansQueryHandler
    : IRequestHandler<GetHostSubscriptionPlansQuery, PagedApiResponse<SubscriptionActivePlanDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GetHostSubscriptionPlansQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides subscription-plan query operations.</param>
    /// <param name="commonRequestService">Validates the current Host principal.</param>
    public GetHostSubscriptionPlansQueryHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Retrieves a database-paged set of non-deleted subscription plans for the current Host administrator.
    /// </summary>
    /// <param name="request">The query to process.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The requested subscription-plan page.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the list request is missing.</exception>
    public async Task<PagedApiResponse<SubscriptionActivePlanDTO>> Handle(
        GetHostSubscriptionPlansQuery request,
        CancellationToken cancellationToken)
    {
        // Validate the current Host identity before reading management data.
        await _commonRequestService.ValidateHostUserRequestAsync();

        if (request?.RequestDTO is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var pageNumber = request.RequestDTO.PageNumber > 0 ? request.RequestDTO.PageNumber : 1;
        var pageSize = request.RequestDTO.PageSize > 0 ? request.RequestDTO.PageSize : 10;

        var plans = await _unitOfWork.SubscriptionRepository.GetHostPlansAsync(
            request.RequestDTO.Search,
            request.RequestDTO.IsActive,
            pageNumber,
            pageSize,
            cancellationToken);

        return PagedApiResponse<SubscriptionActivePlanDTO>.Success(
            plans,
            AppConstants.SuccessMessages.SubscriptionPlansRetrievedSuccessfully);
    }

    #endregion
}

#endregion
