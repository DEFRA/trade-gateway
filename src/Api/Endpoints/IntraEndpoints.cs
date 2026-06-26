using Api.Constants;
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
            .Produces<DefraUNVTDINTRAProfile>(200, MediaTypeAttribute.For<DefraUNVTDINTRAProfile>())
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapGet("certificates/intras", Find)
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
                title: ResponseTitles.NotFound,
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
            query.UpdatedFrom!.Value,
            query.UpdatedBefore!.Value,
            query.Offset,
            query.PageSize,
            query.AcceptLanguage!
        );
        return Results.Json(
            IntraMapper.Map(certificates.FindEuIntraCertificateResponse1),
            contentType: MediaTypeAttribute.For<DefraUNVTDINTRASummaryProfile>()
        );
    }
}
