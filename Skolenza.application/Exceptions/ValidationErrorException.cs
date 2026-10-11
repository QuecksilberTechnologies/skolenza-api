// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines validation failures for centralized error responses.
// ================================================================

using skolenza.application.Constants;

namespace skolenza.application.Exceptions
{
    /// <summary>
    /// Represents an expected validation failure and its structured error details.
    /// </summary>
    public class ValidationErrorException : Exception
    {
        /// <summary>
        /// Gets the stable error code for a validation failure.
        /// </summary>
        public string ErrorCode => AppConstants.ErrorCodes.Validation;

        /// <summary>
        /// Gets the validation details to serialize in the standard error response.
        /// </summary>
        public List<string> Errors { get; }

        /// <summary>
        /// Initializes a validation error containing one validation message.
        /// </summary>
        /// <param name="message">The validation message.</param>
        public ValidationErrorException(string message)
            : base(message)
        {
            Errors = new List<string> { message };
        }

        /// <summary>
        /// Initializes a validation error containing structured validation details.
        /// </summary>
        /// <param name="message">The summary validation message.</param>
        /// <param name="errors">The individual validation errors.</param>
        public ValidationErrorException(string message, List<string> errors)
            : base(message)
        {
            Errors = errors;
        }
    }
}
