// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines application exceptions with standardized HTTP metadata.
// ================================================================

using skolenza.application.Constants;
using System.Net;

namespace skolenza.application.Exceptions
{
    /// <summary>
    /// Represents an expected application failure that the error middleware can serialize safely.
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// Gets the HTTP status code selected by the centralized exception middleware.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Gets the stable application error code written to the error response.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes an application exception while preserving the legacy constructor contract.
        /// </summary>
        /// <param name="message">The safe message to return to the client.</param>
        /// <param name="statusCode">The HTTP status code for the error.</param>
        public ApiException(string message, int statusCode = (int)HttpStatusCode.BadRequest)
            : this(ResolveErrorCode(statusCode), message, statusCode)
        {
        }

        /// <summary>
        /// Initializes an application exception with explicit standardized error metadata.
        /// </summary>
        /// <param name="errorCode">The stable application error code.</param>
        /// <param name="message">The safe message to return to the client.</param>
        /// <param name="statusCode">The HTTP status code for the error.</param>
        public ApiException(string errorCode, string message, int statusCode)
            : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }

        private static string ResolveErrorCode(int statusCode) => statusCode switch
        {
            (int)HttpStatusCode.BadRequest => AppConstants.ErrorCodes.Validation,
            (int)HttpStatusCode.Unauthorized => AppConstants.ErrorCodes.Unauthorized,
            (int)HttpStatusCode.Forbidden => AppConstants.ErrorCodes.Forbidden,
            (int)HttpStatusCode.NotFound => AppConstants.ErrorCodes.NotFound,
            (int)HttpStatusCode.Conflict => AppConstants.ErrorCodes.Conflict,
            _ => AppConstants.ErrorCodes.InternalServerError
        };
    }
}
