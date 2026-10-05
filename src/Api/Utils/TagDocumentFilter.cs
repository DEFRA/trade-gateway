using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Utils;

internal sealed class TagDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = new HashSet<OpenApiTag>
        {
            new()
            {
                Name = "ChedEndpoints",
                Description =
                    "CHED certificates. Fetch a single certificate by its identifier, or search by update window.",
            },
            new()
            {
                Name = "CustomsChedQuantityEndpoints",
                Description =
                    "Customs quantity management for a CHED: the quantity position available, the reservations held against declarations, and manual write-offs.",
            },
            new()
            {
                Name = "DocomEndpoints",
                Description = "DOCOM certificates. Fetch a single certificate by its identifier.",
            },
            new()
            {
                Name = "IntraEndpoints",
                Description =
                    "INTRA certificates. Fetch a single certificate by its identifier, or search by update window.",
            },
            new()
            {
                Name = "ReferenceDataEndpoints",
                Description =
                    "Reference data: classification sections, classification trees and their nodes, and metadata lists.",
            },
        };

        swaggerDoc.ExternalDocs = new OpenApiExternalDocs
        {
#pragma warning disable S1075
            Url = new Uri("https://github.com/DEFRA/trade-gateway"),
#pragma warning restore S1075
            Description = "Source, README and the API decision records.",
        };
    }
}
