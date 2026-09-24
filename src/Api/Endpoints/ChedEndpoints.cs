using Api.Contract;
using Api.Mapping;
using Api.Models;
using Api.Utils.Http;
using Microsoft.AspNetCore.Mvc;
using TracesNT.Services;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Endpoints;

public static class ChedEndpoints
{
    public static void UseChedEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("certificates/cheds/{id}", Get)
            .WithName("GetChed")
            .WithSummary("Fetch a single CHED certificate by its reference.")
            .WithDescription("Returns the full CHED profile.")
            .Produces<DefraUNVTDCHEDProfile>(200, MediaTypeAttribute.For<DefraUNVTDCHEDProfile>())
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapGet("certificates/cheds", Find)
            .WithName("FindCheds")
            .WithSummary("Search CHED certificates updated in a time window.")
            .WithDescription(
                "Returns a paged summary of CHED certificates updated between updatedFrom and "
                    + "updatedBefore (both UTC). Paginate with offset and pageSize; hasMore indicates "
                    + "whether there are more pages to follow."
            )
            .Produces<DefraUNVTDCHEDSummaryProfile>(200, MediaTypeAttribute.For<DefraUNVTDCHEDSummaryProfile>())
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
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
                detail: $"Ched certificate '{id}' was not found."
            );
        return Results.Json(
            ChedMapper.Map(certificate, context),
            contentType: MediaTypeAttribute.For<DefraUNVTDCHEDProfile>()
        );
    }

    private static async Task<IResult> Find(
        [AsParameters] FindCertificatesRequest query,
        [FromServices] IChedCertificateService chedCertificateService
    )
    {
        var certificates = await chedCertificateService.FindChedCertificates(
            query.UpdatedFrom!.Value.UtcDateTime,
            query.UpdatedBefore!.Value.UtcDateTime,
            query.Offset!.Value,
            query.PageSize!.Value,
            query.AcceptLanguage!
        );
        return Results.Json(
            ChedMapper.Map(certificates.FindChedCertificateResponse1),
            contentType: MediaTypeAttribute.For<DefraUNVTDCHEDSummaryProfile>()
        );
    }
}
