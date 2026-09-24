using Api.Contract;
using Api.Mapping;
using Api.Utils.Http;
using Microsoft.AspNetCore.Mvc;
using TracesNT.Services;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Endpoints;

public static class DocomEndpoints
{
    public static void UseDocomEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("certificates/docoms/{id}", Get)
            .WithName("GetDocom")
            .WithSummary("Fetch a single DOCOM certificate by its identifier.")
            .WithDescription("Returns the full DOCOM profile: consignment, parties and supporting documents.")
            .Produces<DefraUNVTDDOCOMProfile>(200, MediaTypeAttribute.For<DefraUNVTDDOCOMProfile>())
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);
    }

    /// <param name="id" example="CHEDA.GB.2024.1020304">Certificate reference of the DOCOM to fetch.</param>
    /// <param name="docomCertificateService">Reads the certificate from TracesNT.</param>
    /// <param name="acceptLanguage">Preferred language for returned names, as a BCP 47 language tag.</param>
    private static async Task<IResult> Get(
        string id,
        IDocomCertificateService docomCertificateService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);
        var context = new MappingContext(languageCode);
        var certificate = await docomCertificateService.GetDocomCertificate(id, languageCode);
        if (certificate?.SPSCertificate == null)
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Docom certificate '{id}' was not found."
            );
        return Results.Json(
            DocomMapper.Map(certificate, context),
            contentType: MediaTypeAttribute.For<DefraUNVTDDOCOMProfile>()
        );
    }
}
