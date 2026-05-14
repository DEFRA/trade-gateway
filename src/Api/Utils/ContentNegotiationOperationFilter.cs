using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Utils;

internal sealed class ContentNegotiationOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var multiContentResponses = context
            .ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IProducesResponseTypeMetadata>()
            .GroupBy(m => m.StatusCode)
            .Where(g => g.Count() > 1);

        foreach (var group in multiContentResponses)
        {
            if (operation.Responses is null)
                break;
            if (!operation.Responses.TryGetValue(group.Key.ToString(), out var response) || response?.Content is null)
                continue;

            response.Content.Clear();
            foreach (var produces in group)
            {
                if (produces.Type is null)
                    continue;
                var schema = context.SchemaGenerator.GenerateSchema(produces.Type, context.SchemaRepository);
                foreach (var contentType in produces.ContentTypes)
                    response.Content[contentType] = new OpenApiMediaType { Schema = schema };
            }
        }
    }
}
