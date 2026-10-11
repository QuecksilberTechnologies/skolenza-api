// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines not-found application errors for centralized responses.
// ================================================================

using skolenza.application.Constants;
using System.Net;

namespace skolenza.application.Exceptions
{
    /// <summary>
    /// Represents an expected request for a resource that does not exist.
    /// </summary>
    public sealed class NotFoundException : ApiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class.
        /// </summary>
        /// <param name="message">The safe public not-found message.</param>
        public NotFoundException(string message)
            : base(AppConstants.ErrorCodes.NotFound, message, (int)HttpStatusCode.NotFound)
        {
        }
    }
}
