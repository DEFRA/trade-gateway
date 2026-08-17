using Api.Constants;
using Api.Contract;
using Api.Mapping;
using Api.Utils.Http;
using Microsoft.AspNetCore.Mvc;
using TracesNT.Services;
using Trade.Gateway.Api.Contract.Customs;

namespace Api.Endpoints;

/// <summary>
/// Customs quantity management, under its own <c>customs/</c> prefix rather than beneath
/// <c>certificates/</c> so that the existing <c>ched-reader</c> grant on
/// <c>/certificates/cheds/**</c> cannot silently confer access to customs quantity data.
/// </summary>
public static class CustomsChedQuantityEndpoints
{
    public static void UseCustomsChedQuantityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("customs/cheds/{chedId}/quantities", GetQuantities)
            .Produces<ChedQuantityLedger>(200, MediaTypeAttribute.For<ChedQuantityLedger>())
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);
    }

    private static async Task<IResult> GetQuantities(
        string chedId,
        ICustomsChedService customsChedService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);
        var response = await customsChedService.GetChedQuantitySummary(chedId, languageCode);

        // A response with no certificate is how TracesNT reports an unknown CHED — the port's single
        // untyped fault says nothing, so this is the only not-found signal available.
        if (response?.ChedCertificate is null)
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: ResponseTitles.NotFound,
                detail: $"Ched certificate '{chedId}' was not found."
            );

        // The CHED exists but carries no quantity position: 502, never 404 and never an empty
        // ledger. Absent and empty are identical on the wire, so an empty-but-successful response
        // would assert "nothing is reserved" on no evidence.
        var summary = response.QuantityManagementSummary;
        if (summary is null)
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: ResponseTitles.BadGateway,
                detail: $"TracesNT returned no quantity management summary for CHED '{chedId}'."
            );

        return Results.Json(
            ChedQuantityMapper.MapLedger(summary),
            contentType: MediaTypeAttribute.For<ChedQuantityLedger>()
        );
    }
}
