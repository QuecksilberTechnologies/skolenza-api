// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles centralized, standardized application exception responses.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.Wrappers;
using System.Net;
using System.Text.Json;

namespace skolenza.api.Middlewares
{
    /// <summary>
    /// Converts expected application exceptions and unexpected failures into one safe API error contract.
    /// </summary>
    public class ErrorHandlerMiddleware
    {
        #region Fields

        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandlerMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the request pipeline.</param>
        /// <param name="logger">The logger used for unexpected application failures.</param>
        public ErrorHandlerMiddleware(
            RequestDelegate next,
            ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        #endregion

        #region Exception Mapping

        /// <summary>
        /// Invokes the next middleware and serializes mapped exceptions when a request fails.
        /// </summary>
        /// <param name="context">The active HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    await _next(context);
                    return;
                }

                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                await HandleExceptionAsync(
                    context,
                    HttpStatusCode.Unauthorized,
                    AppConstants.ErrorCodes.Unauthorized,
                    ex.Message);
            }
            catch (ValidationErrorException ex)
            {
                await HandleExceptionAsync(
                    context,
                    HttpStatusCode.BadRequest,
                    ex.ErrorCode,
                    ex.Message,
                    ex.Errors);
            }
            catch (ApiException ex)
            {
                await HandleExceptionAsync(
                    context,
                    (HttpStatusCode)ex.StatusCode,
                    ex.ErrorCode,
                    ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "A requested resource was not found.");
                await HandleExceptionAsync(
                    context,
                    HttpStatusCode.NotFound,
                    AppConstants.ErrorCodes.NotFound,
                    AppConstants.ErrorMessages.ResourceNotFound);
            }
            catch (NullReferenceException ex)
            {
                _logger.LogError(ex, "Null reference error");
                await HandleExceptionAsync(
                    context,
                    HttpStatusCode.InternalServerError,
                    AppConstants.ErrorCodes.InternalServerError,
                    AppConstants.ErrorMessages.RequiredDataMissing);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled exception. RequestId: {RequestId}",
                    context.TraceIdentifier);
                await HandleExceptionAsync(
                    context,
                    HttpStatusCode.InternalServerError,
                    AppConstants.ErrorCodes.InternalServerError,
                    "The server could not complete this request. Please try again. " +
                    "If it continues, contact support with the request ID below.",
                    new List<string> { $"Request ID: {context.TraceIdentifier}" });
            }
        }

        #endregion

        #region Error Response

        /// <summary>
        /// Writes the standardized application error envelope to the response.
        /// </summary>
        /// <param name="context">The active HTTP context.</param>
        /// <param name="statusCode">The HTTP status for the error.</param>
        /// <param name="errorCode">The stable application error code.</param>
        /// <param name="message">The safe public error message.</param>
        /// <param name="errors">Optional validation details.</param>
        internal static async Task HandleExceptionAsync(
            HttpContext context,
            HttpStatusCode statusCode,
            string errorCode,
            string message,
            List<string>? errors = null)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new ApiResponse<object>
            {
                IsSucceeded = false,
                ErrorCode = errorCode,
                Message = message,
                Data = null!,
                Errors = errors ?? new List<string>()
            };

            var result = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(result);
        }

        #endregion
    }
}
