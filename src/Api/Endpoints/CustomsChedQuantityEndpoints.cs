using Amazon.Runtime.Internal;
using Api.Contract;
using Api.Extensions;
using Api.Mapping;
using Api.Utils.Http;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TracesNT.Services;
using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Customs;

namespace Api.Endpoints;

/// <summary>
/// Customs quantity management, under its own <c>customs/</c> prefix rather than beneath
/// <c>certificates/</c> so that the existing <c>ched-reader</c> grant on
/// <c>/certificates/cheds/**</c> cannot silently confer access to customs quantity data.
/// The two halves of the same upstream operation: the ledger read sends
/// <c>QuantityManagementIndication = "0"</c>, the reservation sends <c>"1"</c> and mutates
/// state upstream.
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

        app.MapPut("customs/cheds/{chedId}/declarations/{mrn}/reservation", PutReservation)
            .Produces<ChedDeclarationReservation>(200, MediaTypeAttribute.For<ChedDeclarationReservation>())
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapPut("customs/cheds/{chedId}/declarations/{mrn}/release", Release)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
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
                detail: $"Ched certificate '{chedId}' was not found."
            );

        // The CHED exists but carries no quantity position: 502, never 404 and never an empty
        // ledger. Absent and empty are identical on the wire, so an empty-but-successful response
        // would assert "nothing is reserved" on no evidence.
        var summary = response.QuantityManagementSummary;
        if (summary is null)
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                detail: $"TracesNT returned no quantity management summary for CHED '{chedId}'."
            );

        return Results.Json(
            ChedQuantityMapper.MapLedger(summary),
            contentType: MediaTypeAttribute.For<ChedQuantityLedger>()
        );
    }

    private static async Task<IResult> PutReservation(
        string chedId,
        string mrn,
        ChedReservationRequest request,
        ICustomsChedService customsChedService,
        IValidator<ChedReservationRequest> validator,
        ILoggerFactory loggerFactory,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        // Run explicitly: the minimal API validation pipeline validates the request object without
        // descending into items[], so the item rules would otherwise go unenforced.
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var items = ChedQuantityMapper.MapReservationItems(request.Items);
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);
        var response = await customsChedService.ReserveChedQuantities(chedId, mrn, items, languageCode);

        // A response with no certificate is how TracesNT reports an unknown CHED — the port's single
        // untyped fault says nothing, so this is the only not-found signal available.
        if (response?.ChedCertificate is null)
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Ched certificate '{chedId}' was not found."
            );

        // Neither "reserved" nor "refused" is safe to infer.
        if (!response.ReservationResultSpecified)
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                detail: $"TracesNT did not state a reservation result for CHED '{chedId}'."
            );

        if (!response.ReservationResult)
            return Refused(chedId, mrn, response, loggerFactory);

        var reservationSummary = response.QuantityManagementSummary;
        if (reservationSummary is null)
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                detail: $"TracesNT accepted the reservation for CHED '{chedId}' but returned no quantity summary."
            );

        var reservation = ChedQuantityMapper.MapDeclarationReservation(reservationSummary, mrn);

        // Empty arrays would tell the caller this declaration holds nothing, one line after telling
        // them the reservation succeeded.
        if (reservation.Reserved.Length == 0 && reservation.Consumed.Length == 0)
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                detail: $"TracesNT accepted the reservation but reported no allocation for declaration '{mrn}'."
            );

        return Results.Json(reservation, contentType: MediaTypeAttribute.For<ChedDeclarationReservation>());
    }

    private static async Task<IResult> Release(
       string chedId,
       string mrn,
       ICustomsChedService customsChedService,
       [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
   )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);
        var response = await customsChedService.Release(chedId, mrn, languageCode);

        var outcome = response?.QuantityManagementOutcome;

        // A clean cancellation has nothing to report. Outcome 04 does — the CHED status
        // changed mid-clearance — so it always comes back with a body.
        if (QuantityManagementOutcomes.IsSuccess(outcome))
        {
            return Results.Ok();
        }

        return Results.Problem(
            title: "Quantity management request not executed",
            detail: QuantityManagementOutcomes.Describe(outcome),
            statusCode: QuantityManagementOutcomes.ToStatusCode(outcome),
            extensions: new Dictionary<string, object?>
            {
                ["chedId"] = chedId,
                ["mrn"] = mrn,
                ["outcome"] = outcome,
                ["chedStatus"] = response?.StatusCode,
            }
        );
    }

    /// <summary>
    /// The upstream <c>ReservationFailureReason</c> is a code, published only once decoded against
    /// the gateway's own table — a value outside it is reported as unrecognised rather than echoed,
    /// since the element is free text on the wire (ADR-0002 §4). The raw value is always logged.
    /// </summary>
    private static IResult Refused(
        string chedId,
        string mrn,
        ProcessedChedInformationResponseType response,
        ILoggerFactory loggerFactory
    )
    {
        loggerFactory
            .CreateLogger(typeof(CustomsChedQuantityEndpoints).FullName!)
            .LogWarning(
                "TracesNT refused reservation of CHED {ChedId} against declaration {Mrn}: {FailureReason}",
                chedId,
                mrn,
                response.ReservationFailureReason
            );

        var failedItem = response.ReservationFailureConsignmentItem;
        var reason = ReservationFailureReasons.Decode(response.ReservationFailureReason);

        return Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            detail: $"TracesNT refused the reservation of CHED '{chedId}' against declaration '{mrn}'.",
            extensions: new Dictionary<string, object?>
            {
                ["failureReason"] = reason is null
                    ? null
                    : new { code = reason.Value.Code, description = reason.Value.Description },
                ["failedItem"] = failedItem is null
                    ? null
                    : new
                    {
                        goodsItemNumber = failedItem.GoodsItemNumber,
                        documentLineItemNumber = failedItem.DocumentLineItemNumber,
                    },
            }
        );
    }
}
