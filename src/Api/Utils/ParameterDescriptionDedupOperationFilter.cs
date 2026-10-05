using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Utils;

internal sealed class ParameterDescriptionDedupOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters is null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            if (
                parameter.Schema is not null
                && !string.IsNullOrEmpty(parameter.Description)
                && parameter.Schema.Description == parameter.Description
            )
            {
                parameter.Schema.Description = null;
            }
        }
    }
}
