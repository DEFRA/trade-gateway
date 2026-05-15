using Api.Models;
using Microsoft.Extensions.Options;
using System.ServiceModel;
using TracesNT;
using TracesNT.WebServices;

namespace Api.Endpoints;

public static class IntraEndpoints
{
    public static void UseIntraEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("intra/{id}", Get)
            .Produces<IntraCertificate>(200, MediaTypeAttribute.For<IntraCertificate>())
            .Produces<IntraCertificateV2>(200, MediaTypeAttribute.For<IntraCertificateV2>());
    }

    private static async Task<IResult> Get(string id, 
        HttpRequest request, 
        EuIntraCertificatePortClient euIntraCertificatePort,
        IOptions<TracesNtConfig> tracesOptions)
    {
        try
        {
            var certificateResponse = await euIntraCertificatePort.getEuIntraCertificateAsync(
                new SecurityHeaderType(),
                tracesOptions.Value.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.EN,
                [],
                new GetEuIntraCertificateRequestType { ID = id });

            if (certificateResponse?.GetEuIntraCertificateResponse1?.SPSCertificate == null)
            {
                return Results.NotFound($"Not found {id}");
            }

            var consignment = new Consignment
            {
                Package = certificateResponse
                    .GetEuIntraCertificateResponse1
                    .SPSCertificate
                    ?.SPSConsignment
                    ?.IncludedSPSConsignmentItem
                    ?.FirstOrDefault()
                    ?.NatureIdentificationSPSCargo
                    ?.FirstOrDefault()
                    ?.TypeCode
                    ?.name
            };

            var acceptedTypes = request.GetTypedHeaders().Accept;

            if (acceptedTypes.Any(h => h.MediaType == MediaTypeAttribute.For<IntraCertificate>()))
            {
                return Results.Json(
                    new IntraCertificate
                    {
                        Ref = id, 
                        Consignment = consignment
                    },
                    contentType: MediaTypeAttribute.For<IntraCertificate>()
                );
            }

            return Results.Json(
                new IntraCertificateV2 { Id = id, Consignment = consignment },
                contentType: MediaTypeAttribute.For<IntraCertificateV2>()
            );
        }
        catch (FaultException<EuIntraCertificateNotFoundExceptionType> ex)
        {
            return Results.NotFound($"Not found {ex.Detail.CertificateIdentifier}");
        }
        finally
        {
            await ClientUtilities.CloseClient(euIntraCertificatePort);
        }
    }
}
