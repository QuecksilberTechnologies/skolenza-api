// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Returns active master feature-page and operation metadata.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.FeaturePages;
using skolenza.application.Features.FeaturePages.Queries;
using skolenza.application.Interfaces;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.FeaturePages.Handlers;

/// <summary>
/// Handles the public read-only feature-pages request.
/// </summary>
public sealed class GetFeaturePagesQueryHandler
    : IRequestHandler<GetFeaturePagesQuery, ApiResponse<IReadOnlyCollection<FeaturePageResponseDTO>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetFeaturePagesQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides the Module master repository.</param>
    public GetFeaturePagesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Retrieves active master feature pages without token or permission validation.
    /// </summary>
    /// <param name="request">The parameterless feature-pages request.</param>
    /// <param name="cancellationToken">A token used to cancel request processing.</param>
    /// <returns>The active feature headers with their flat operational leaf pages and operation details.</returns>
    public async Task<ApiResponse<IReadOnlyCollection<FeaturePageResponseDTO>>> Handle(
        GetFeaturePagesQuery request,
        CancellationToken cancellationToken)
    {
        var featurePages = await _unitOfWork.ModuleRepository
            .GetActiveFeaturePagesAsync(request.Scope, cancellationToken);

        return ApiResponse<IReadOnlyCollection<FeaturePageResponseDTO>>.Success(
            featurePages,
            AppConstants.SuccessMessages.FeaturePagesRetrievedSuccessfully);
    }
}
