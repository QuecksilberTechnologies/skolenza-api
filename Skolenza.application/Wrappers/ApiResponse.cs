// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the standardized application response envelope for successful responses and middleware-generated errors.
// ================================================================

using System.Text.Json.Serialization;

namespace skolenza.application.Wrappers
{
    /// <summary>
    /// Represents the standardized application response envelope returned by handlers and error middleware.
    /// </summary>
    /// <typeparam name="T">The response payload type.</typeparam>
    public class ApiResponse<T>
    {
        #region Constructors

        /// <summary>
        /// Initializes an empty response envelope for middleware and legacy object-initializer callers.
        /// </summary>
        public ApiResponse()
        {
        }

        #endregion

        #region Core Response Properties

        /// <summary>
        /// Gets or sets whether the request completed successfully.
        /// </summary>
        public bool IsSucceeded { get; set; }

        /// <summary>
        /// Gets or sets the application-level response message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the response payload.
        /// </summary>
        public T Data { get; set; } = default!;

        #endregion

        #region Error Metadata

        /// <summary>
        /// Gets or sets the validation or application error details written by centralized middleware.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Gets or sets the stable application error code written by centralized middleware.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorCode { get; set; }

        #endregion

        #region Pagination Metadata

        /// <summary>
        /// Gets or sets the current page number when the payload is paged.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PageNumber { get; set; }

        /// <summary>
        /// Gets or sets the page size when the payload is paged.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of records when the payload is paged.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TotalRecords { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages when the payload is paged.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TotalPages { get; set; }

        #endregion

        #region Optional Response Metadata

        /// <summary>
        /// Gets or sets whether a module-specific primary record was marked.
        /// Retained for response compatibility pending the dedicated paging and response redesign.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsPrimaryMarked { get; set; }

        /// <summary>
        /// Gets or sets whether a module-specific document set has been uploaded.
        /// Retained for response compatibility pending the dedicated paging and response redesign.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? HasAllDocUploaded { get; set; }

        /// <summary>
        /// Gets or sets a module-specific completion percentage.
        /// Retained for response compatibility pending the dedicated paging and response redesign.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? CompletionPercentage { get; set; }

        #endregion

        #region Success Factories

        /// <summary>
        /// Creates a successful application response containing the supplied data.
        /// </summary>
        /// <param name="data">The response payload.</param>
        /// <param name="message">The application-level success message.</param>
        /// <returns>A successful API response.</returns>
        public static ApiResponse<T> Success(T data, string message = "")
        {
            return new ApiResponse<T>
            {
                IsSucceeded = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Creates a successful paginated application response.
        /// </summary>
        public static ApiResponse<T> SuccessPaginated(
            T data,
            int pageNumber,
            int pageSize,
            int totalRecords,
            int totalPages,
            string message = "")
        {
            return new ApiResponse<T>
            {
                IsSucceeded = true,
                Message = message,
                Data = data,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages
            };
        }

        /// <summary>
        /// Creates a successful paginated response with document-completion metadata.
        /// </summary>
        public static ApiResponse<T> SuccessPaginatedPercentage(
            T Data,
            int PageNumber,
            int PageSize,
            int TotalRecords,
            int TotalPages,
            string Message = "",
            bool? HasUploadedAll = null,
            double? CompletionPercentage = null)
        {
            return new ApiResponse<T>
            {
                IsSucceeded = true,
                Message = Message,
                Data = Data,
                PageNumber = PageNumber,
                PageSize = PageSize,
                TotalRecords = TotalRecords,
                TotalPages = TotalPages,
                HasAllDocUploaded = HasUploadedAll,
                CompletionPercentage = CompletionPercentage
            };
        }

        /// <summary>
        /// Creates a successful paginated response with document-upload metadata.
        /// </summary>
        public static ApiResponse<T> SuccessPaginatedOnly(
            T Data,
            int PageNumber,
            int PageSize,
            int TotalRecords,
            int TotalPages,
            string Message = "",
            bool? HasUploadedAll = null)
        {
            return new ApiResponse<T>
            {
                IsSucceeded = true,
                Message = Message,
                Data = Data,
                PageNumber = PageNumber,
                PageSize = PageSize,
                TotalRecords = TotalRecords,
                TotalPages = TotalPages,
                HasAllDocUploaded = HasUploadedAll
            };
        }

        #endregion
    }
}
