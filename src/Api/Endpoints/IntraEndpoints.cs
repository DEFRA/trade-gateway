using Api.Contract;
using Api.Mapping;
using Api.Utils.Http;
using Microsoft.AspNetCore.Mvc;
using TracesNT.Services;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Endpoints;

public static class IntraEndpoints
{
    public static void UseIntraEndpoints(this IEndpointRouteBuilder app)
    {

        app.MapGet("intras/{id}", Get)
            .Produces<DefraUNVTDINTRAProfile>(200, MediaTypeAttribute.For<DefraUNVTDINTRAProfile>())
            .ProducesProblem(StatusCodes.Status404NotFound)
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
                title: "Not Found",
                detail: $"Intra certificate '{id}' was not found."
            );

        return Results.Json(
            IntraMapper.Map(certificate, context),
            contentType: MediaTypeAttribute.For<DefraUNVTDINTRAProfile>()
        );
    }
}
