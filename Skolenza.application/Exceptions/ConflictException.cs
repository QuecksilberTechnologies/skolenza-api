// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines business-conflict errors for centralized responses.
// ================================================================

using skolenza.application.Constants;
using System.Net;

namespace skolenza.application.Exceptions
{
    /// <summary>
    /// Represents an expected business conflict, such as a duplicate resource.
    /// </summary>
    public sealed class ConflictException : ApiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class.
        /// </summary>
        /// <param name="message">The safe public conflict message.</param>
        /// <param name="errorCode">An optional stable code for a specific business dependency.</param>
        public ConflictException(string message, string? errorCode = null)
            : base(errorCode ?? AppConstants.ErrorCodes.Conflict, message, (int)HttpStatusCode.Conflict)
        {
        }
    }
}
