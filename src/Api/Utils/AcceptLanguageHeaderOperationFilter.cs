using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Utils;

internal sealed class AcceptLanguageHeaderOperationFilter : IOperationFilter
{
    private const string HeaderName = "Accept-Language";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters is null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            if (parameter.Name != HeaderName || parameter.In != ParameterLocation.Header)
                continue;

            parameter.Description =
                "Preferred language for returned names, as a BCP 47 language tag. Defaults to en when absent or not recognised.";
        }
    }
}
