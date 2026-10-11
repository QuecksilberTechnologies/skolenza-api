// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles institution subscription plan information queries.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOs.Institution;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.SubscriptionCmd.Handlers;

#region Query

/// <summary>
/// Represents the request to retrieve subscription information for a institution.
/// </summary>
public sealed class GetInstitutionSubscriptionPlanQuery
    : IRequest<ApiResponse<InstitutionSubscriptionPlanResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetInstitutionSubscriptionPlanQuery"/> class.
    /// </summary>
    /// <param name="requestDTO">The institution subscription query criteria.</param>
    public GetInstitutionSubscriptionPlanQuery(InstitutionSubscriptionPlanRequestDTO? requestDTO)
    {
        RequestDTO = requestDTO;
    }

    /// <summary>
    /// Gets the institution subscription query criteria.
    /// </summary>
    public InstitutionSubscriptionPlanRequestDTO? RequestDTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles institution subscription plan information queries without changing their existing access semantics.
/// </summary>
public sealed class GetInstitutionSubscriptionPlanQueryHandler
    : IRequestHandler<GetInstitutionSubscriptionPlanQuery, ApiResponse<InstitutionSubscriptionPlanResponseDTO>>
{
    #region Fields

    private readonly IInstitutionSubscriptionRepository _institutionSubscriptionRepository;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GetInstitutionSubscriptionPlanQueryHandler"/> class.
    /// </summary>
    /// <param name="institutionSubscriptionRepository">Provides institution subscription read operations.</param>
    public GetInstitutionSubscriptionPlanQueryHandler(
        IInstitutionSubscriptionRepository institutionSubscriptionRepository)
    {
        _institutionSubscriptionRepository = institutionSubscriptionRepository;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Retrieves the current subscription information for the requested institution.
    /// </summary>
    /// <param name="request">The query to process.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The institution subscription plan response.</returns>
    /// <exception cref="ValidationErrorException">Thrown when institution criteria are invalid.</exception>
    public async Task<ApiResponse<InstitutionSubscriptionPlanResponseDTO>> Handle(
        GetInstitutionSubscriptionPlanQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request?.RequestDTO is null || request.RequestDTO.InstitutionId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var subscriptionPlan = await _institutionSubscriptionRepository
            .GetValidateInstitutionPlan(request.RequestDTO);

        return ApiResponse<InstitutionSubscriptionPlanResponseDTO>.Success(
            subscriptionPlan,
            AppConstants.SuccessMessages.InstitutionSubscriptionPlanRetrievedSuccessfully);
    }

    #endregion
}

#endregion
