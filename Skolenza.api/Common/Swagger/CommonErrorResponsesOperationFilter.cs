// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Adds common API error responses to Swagger operations.
// ================================================================

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace skolenza.api.Common.Swagger;

/// <summary>
/// Documents the HTTP error responses produced by the centralized API pipeline.
/// </summary>
public sealed class CommonErrorResponsesOperationFilter : IOperationFilter
{
    #region Public Methods

    /// <summary>
    /// Adds common exception-middleware and authorization responses without replacing endpoint-specific documentation.
    /// </summary>
    /// <param name="operation">The Swagger operation being generated.</param>
    /// <param name="context">The operation metadata context.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        AddResponse(operation, StatusCodes.Status400BadRequest, "The request is invalid.");
        AddResponse(operation, StatusCodes.Status401Unauthorized, "Authentication is required or the access token is invalid.");
        AddResponse(operation, StatusCodes.Status403Forbidden, "The authenticated user is not authorized for this action.");
        AddResponse(operation, StatusCodes.Status404NotFound, "The requested resource was not found.");
        AddResponse(operation, StatusCodes.Status500InternalServerError, "An unexpected server error occurred.");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Adds an error response only when the action has not documented that status code explicitly.
    /// </summary>
    private static void AddResponse(OpenApiOperation operation, int statusCode, string description)
    {
        var responseCode = statusCode.ToString();
        if (!operation.Responses.ContainsKey(responseCode))
        {
            operation.Responses.Add(responseCode, new OpenApiResponse
            {
                Description = description
            });
        }
    }

    #endregion
}
