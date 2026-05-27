using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Nodes;
using Trade.Gateway.Api.Contract;

namespace Api.Utils;

public class ConstValueSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var constAttr = context.MemberInfo?.GetCustomAttribute<ConstValueAttribute>();
        if (constAttr is null) return;

        if (schema is OpenApiSchema openApiSchema)
            openApiSchema.Enum = [JsonValue.Create(constAttr.Value)];
    }
}
