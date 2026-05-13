using System.Net;
using Api.Models;

namespace Api.Endpoints;

public static class IntraEndpoints
{
    public static void UseIntraEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("intra/{id}", Get)
            .Produces<IntraCertificate>(200, MediaTypeAttribute.For<IntraCertificate>())
            .Produces<IntraCertificateV2>(200, MediaTypeAttribute.For<IntraCertificateV2>());
    }

    private static IResult Get(string id, HttpRequest request)
    {
        var consignment = new Consignment { Package = "Package 1" };
        var acceptedTypes = request.GetTypedHeaders().Accept;

        if (acceptedTypes.Any(h => h.MediaType == MediaTypeAttribute.For<IntraCertificate>()))
        {
            return Results.Json(
                new IntraCertificate { Ref = id, Consignment = consignment },
                contentType: MediaTypeAttribute.For<IntraCertificate>()
            );
        }

        return Results.Json(
            new IntraCertificateV2 { Id = id, Consignment = consignment },
            contentType: MediaTypeAttribute.For<IntraCertificateV2>()
        );
    }
}
