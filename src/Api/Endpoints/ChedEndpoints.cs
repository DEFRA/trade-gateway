using Api.Constants;
using Api.Contract;
using Api.Mapping;
using Api.Utils.Http;
using Microsoft.AspNetCore.Mvc;
using TracesNT.Services;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Endpoints;

public static class ChedEndpoints
{
    public static void UseChedEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("cheds/{id}", Get)
            .Produces<DefraUNVTDCHEDProfile>(200, MediaTypeAttribute.For<DefraUNVTDCHEDProfile>())
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);
    }

    private static async Task<IResult> Get(
        string id,
        IChedCertificateService chedCertificateService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);
        var context = new MappingContext(languageCode);
        var certificate = await chedCertificateService.GetChedCertificate(id, languageCode);
        if (certificate?.SPSCertificate == null)
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: ResponseTitles.NotFound,
                detail: $"Ched certificate '{id}' was not found."
            );
        return Results.Json(
            ChedMapper.Map(certificate, context),
            contentType: MediaTypeAttribute.For<DefraUNVTDCHEDProfile>()
        );
    }
}
