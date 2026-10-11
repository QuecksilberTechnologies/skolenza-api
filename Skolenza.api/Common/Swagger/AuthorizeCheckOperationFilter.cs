using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;


namespace skolenza.api.Common.Swagger
{

    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAuthorize =
                context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any() == true ||
                context.MethodInfo.GetCustomAttributes(true).OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any();

            if (hasAuthorize)
            {
                operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    //[ new OpenApiSecurityScheme
                    //    {
                    //        //Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    //    }
                    //] = new string[] { }
                }
            };
            }
        }
    }
    

}
