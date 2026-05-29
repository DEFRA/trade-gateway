using Api.Contract;
using Api.Mapping;
using Api.Utils.Http;
using Microsoft.AspNetCore.Mvc;
using TracesNT.Services;
using Trade.Gateway.Api.Contract;

namespace Api.Endpoints;

public static class IntraEndpoints
{
    public static void UseIntraEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("intra/{id}", Get)
            .Produces<DefraUNVTDINTRAProfile>(200, MediaTypeAttribute.For<DefraUNVTDINTRAProfile>());
    }

    private static async Task<IResult> Get(
        string id,
        IEuIntraCertificateService euIntraCertificateService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);
        var context = new MappingContext(languageCode);

        var certificate = await euIntraCertificateService.GetEuIntraCertificate(id, languageCode);

        if (certificate?.SPSCertificate == null)
            return Results.NotFound($"Not found {id}");

        return Results.Json(
            IntraMapper.Map(certificate, context),
            contentType: MediaTypeAttribute.For<DefraUNVTDINTRAProfile>()
        );
    }
}
