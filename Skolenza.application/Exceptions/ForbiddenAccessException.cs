// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines permission-denied errors for centralized responses.
// ================================================================

using skolenza.application.Constants;
using System.Net;

namespace skolenza.application.Exceptions
{
    /// <summary>
    /// Represents a request from an authenticated principal without sufficient permission.
    /// </summary>
    public sealed class ForbiddenAccessException : ApiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ForbiddenAccessException"/> class.
        /// </summary>
        /// <param name="message">The safe public permission-denied message.</param>
        public ForbiddenAccessException(string message)
            : base(AppConstants.ErrorCodes.Forbidden, message, (int)HttpStatusCode.Forbidden)
        {
        }
    }
}
