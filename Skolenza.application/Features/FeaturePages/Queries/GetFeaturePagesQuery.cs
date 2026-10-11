// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the public read-only request for feature-page metadata.
// ================================================================

using skolenza.application.DTOS.FeaturePages;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.FeaturePages.Queries;

/// <summary>
/// Represents a request for active master feature pages and operations, optionally limited to one scope.
/// </summary>
public sealed class GetFeaturePagesQuery
    : IRequest<ApiResponse<IReadOnlyCollection<FeaturePageResponseDTO>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetFeaturePagesQuery"/> class.
    /// </summary>
    /// <param name="scope">1 for Institution, 2 for Host, 3 for Common, or <see langword="null"/> for every scope.</param>
    public GetFeaturePagesQuery(short? scope)
    {
        Scope = scope;
    }

    /// <summary>
    /// Gets the optional feature-page scope filter.
    /// </summary>
    public short? Scope { get; }
}
