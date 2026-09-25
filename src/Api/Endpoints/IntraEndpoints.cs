using Api.Contract;
using Api.Mapping;
using Api.Models;
using Api.Utils.Http;
using Microsoft.AspNetCore.Mvc;
using TracesNT.Services;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Endpoints;

public static class IntraEndpoints
{
    public static void UseIntraEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("certificates/intras/{id}", Get)
            .WithName("GetIntraCertificate")
            .WithSummary("Get an INTRA certificate")
            .WithDescription(
                "Retrieves the EU INTRA trade certificate with the given reference from TRACES NT, mapped to "
                    + "the DEFRA UN/CEFACT INTRA profile. Text is localised using the Accept-Language header."
            )
            .Produces<DefraUNVTDINTRAProfile>(200, MediaTypeAttribute.For<DefraUNVTDINTRAProfile>())
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapGet("certificates/intras", Find)
            .WithName("FindIntraCertificates")
            .WithSummary("Find updated INTRA certificates")
            .WithDescription(
                "Returns a page of summaries of EU INTRA trade certificates updated between updatedFrom and "
                    + "updatedBefore. Use offset and pageSize to page through the results."
            )
            .Produces<DefraUNVTDINTRASummaryProfile>(200, MediaTypeAttribute.For<DefraUNVTDINTRASummaryProfile>())
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);
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
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Intra certificate '{id}' was not found."
            );
        return Results.Json(
            IntraMapper.Map(certificate, context),
            contentType: MediaTypeAttribute.For<DefraUNVTDINTRAProfile>()
        );
    }

    private static async Task<IResult> Find(
        [AsParameters] FindCertificatesRequest query,
        [FromServices] IEuIntraCertificateService euIntraCertificateService
    )
    {
        var certificates = await euIntraCertificateService.FindEuIntraCertificates(
            query.UpdatedFrom!.Value.UtcDateTime,
            query.UpdatedBefore!.Value.UtcDateTime,
            query.Offset!.Value,
            query.PageSize!.Value,
            query.AcceptLanguage!
        );
        return Results.Json(
            IntraMapper.Map(certificates.FindEuIntraCertificateResponse1),
            contentType: MediaTypeAttribute.For<DefraUNVTDINTRASummaryProfile>()
        );
    }
}
