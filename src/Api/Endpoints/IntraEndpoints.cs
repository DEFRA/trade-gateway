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
            .WithName("GetIntra")
            .WithSummary("Fetch a single INTRA certificate by its identifier.")
            .WithDescription(
                "Returns the full INTRA profile: consignment, parties, classification and notes. A draft "
                    + "certificate (statusCode 47) is not served and returns 404."
            )
            .Produces<DefraUNVTDINTRAProfile>(200, MediaTypeAttribute.For<DefraUNVTDINTRAProfile>())
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapGet("certificates/intras", Find)
            .WithName("FindIntras")
            .WithSummary("Search INTRA certificates updated in a time window.")
            .WithDescription(
                "Returns a paged summary of INTRA certificates updated between updatedFrom and "
                    + "updatedBefore (both UTC). Paginate with offset and pageSize; hasMore indicates "
                    + "whether there are more pages to follow. Draft certificates (statusCode 47) "
                    + "are omitted from items; hasMore is computed from the "
                    + "unfiltered upstream page."
            )
            .Produces<DefraUNVTDINTRASummaryProfile>(200, MediaTypeAttribute.For<DefraUNVTDINTRASummaryProfile>())
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);
    }

    /// <param name="id" example="CHEDA.GB.2024.1020304">Certificate reference of the INTRA to fetch.</param>
    /// <param name="euIntraCertificateService">Reads the certificate from TracesNT.</param>
    /// <param name="loggerFactory">Creates the logger for the draft path.</param>
    /// <param name="acceptLanguage">Preferred language for returned names, as a BCP 47 language tag.</param>
    private static async Task<IResult> Get(
        string id,
        IEuIntraCertificateService euIntraCertificateService,
        ILoggerFactory loggerFactory,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);
        var context = new MappingContext(languageCode);
        var certificate = await euIntraCertificateService.GetEuIntraCertificate(id, languageCode);
        if (certificate?.SPSCertificate == null)
            return IntraNotFound(id);

        if (DraftNotification.IsDraft(certificate.SPSCertificate))
        {
            DraftNotification.LogIgnored(loggerFactory, id);
            return IntraNotFound(id);
        }

        return Results.Json(
            IntraMapper.Map(certificate, context),
            contentType: MediaTypeAttribute.For<DefraUNVTDINTRAProfile>()
        );
    }

    /// <param name="query">Update window and paging options; see the query parameters.</param>
    /// <param name="euIntraCertificateService">Reads the certificates from TracesNT.</param>
    /// <param name="loggerFactory">Creates the logger for the draft path.</param>
    private static async Task<IResult> Find(
        [AsParameters] FindCertificatesRequest query,
        [FromServices] IEuIntraCertificateService euIntraCertificateService,
        ILoggerFactory loggerFactory
    )
    {
        var certificates = await euIntraCertificateService.FindEuIntraCertificates(
            query.UpdatedFrom!.Value.UtcDateTime,
            query.UpdatedBefore!.Value.UtcDateTime,
            query.Offset!.Value,
            query.PageSize!.Value,
            query.AcceptLanguage!
        );
        var page = certificates.FindEuIntraCertificateResponse1;
        foreach (var draft in (page.EuIntraCertificateResult ?? []).Where(result => DraftNotification.IsDraft(result)))
            DraftNotification.LogIgnored(loggerFactory, draft.ID);
        return Results.Json(
            IntraMapper.Map(page),
            contentType: MediaTypeAttribute.For<DefraUNVTDINTRASummaryProfile>()
        );
    }

    private static IResult IntraNotFound(string id) =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Intra certificate '{id}' was not found.");
}
