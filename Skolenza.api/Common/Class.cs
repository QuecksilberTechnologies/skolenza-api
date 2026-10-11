namespace skolenza.api.Common
{
    using Microsoft.OpenApi;
    using Swashbuckle.AspNetCore.SwaggerGen;

    //public class NullSchemaFilter : ISchemaFilter
    //{
    //    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    //    {
    //        if (schema?.Properties == null) return;

    //        foreach (var prop in schema.Properties)
    //        {
    //            if (prop.Value.Type is "string" or "integer" or "boolean" or "number")
    //            {
    //                // ✅ Remove "string", "0", "false", etc. from Example if present
    //                if (prop.Value.Example != null &&
    //                    prop.Value.Example.ToString().Trim().ToLower() is "string" or "0" or "false")
    //                {
    //                    prop.Value.Example = null;
    //                }

    //                // ✅ Always set Default to null for cleaner payload
    //                prop.Value.Default = null;
    //            }
    //        }
    //    }
    //}
}
