// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides a flattened paginated API response.
// ================================================================

using skolenza.application.DTOS.Pagination;

namespace skolenza.application.Wrappers;

/// <summary>
/// Represents a successful paginated response with the collection and pagination metadata at the same JSON level.
/// </summary>
/// <typeparam name="T">The type of item in the paginated collection.</typeparam>
public sealed class PagedApiResponse<T>
{
    #region Response Properties

    /// <summary>
    /// Gets or sets whether the request completed successfully.
    /// </summary>
    public bool IsSucceeded { get; set; }

    /// <summary>
    /// Gets or sets the application-level response message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the paginated collection.
    /// </summary>
    public List<T> Data { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of matching records.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current one-based page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of records requested per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of available pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets whether a previous page is available.
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Gets or sets whether a next page is available.
    /// </summary>
    public bool HasNext { get; set; }

    /// <summary>
    /// Gets or sets optional document-upload completion information.
    /// </summary>
    public bool? HasUploadedAll { get; set; }

    /// <summary>
    /// Gets or sets optional primary-record information.
    /// </summary>
    public bool? IsPrimaryMarked { get; set; }

    /// <summary>
    /// Gets or sets optional completion percentage information.
    /// </summary>
    public double? CompletionPercentage { get; set; }

    #endregion

    #region Success Factory

    /// <summary>
    /// Creates a successful flattened response from an existing paged query result.
    /// </summary>
    /// <param name="pagedResult">The paged result returned by the repository.</param>
    /// <param name="message">The application-level success message.</param>
    /// <returns>A successful flattened paginated API response.</returns>
    public static PagedApiResponse<T> Success(
        PagedResponseDTO<T> pagedResult,
        string message = "")
    {
        ArgumentNullException.ThrowIfNull(pagedResult);

        return new PagedApiResponse<T>
        {
            IsSucceeded = true,
            Message = message,
            Data = pagedResult.Data,
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages,
            HasPrevious = pagedResult.HasPrevious,
            HasNext = pagedResult.HasNext,
            HasUploadedAll = pagedResult.HasUploadedAll,
            IsPrimaryMarked = pagedResult.IsPrimaryMarked,
            CompletionPercentage = pagedResult.CompletionPercentage
        };
    }

    #endregion
}
