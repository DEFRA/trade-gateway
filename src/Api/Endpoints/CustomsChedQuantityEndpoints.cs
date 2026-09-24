using Api.Contract;
using Api.Filters;
using Api.Mapping;
using Api.Utils.Http;
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
            .WithName("GetChedQuantities")
            .WithSummary("Read the quantity position of a CHED.")
            .WithDescription(
                "Returns what remains available on the CHED and, when declarations hold "
                    + "quantities, what they reserve and have consumed."
            )
            .Produces<ChedQuantityLedger>(200, MediaTypeAttribute.For<ChedQuantityLedger>())
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapPut("customs/cheds/{chedId}/declarations/{mrn}/reservation", PutReservation)
            .WithName("ReserveChedQuantities")
            .WithSummary("Reserve quantities of a CHED against a customs declaration.")
            .WithDescription(
                "Creates a reservation against the declaration's MRN for the supplied consignment items. "
                    + "Returns the allocation result or an error from TRACES indicating why it failed."
            )
            .Validates<ChedReservationRequest>()
            .Produces<ChedDeclarationReservation>(200, MediaTypeAttribute.For<ChedDeclarationReservation>())
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapPost("customs/cheds/{chedId}/declarations/{mrn}/reservation/manual-release", ForceWriteOff)
            .WithName("ForceWriteOff")
            .WithSummary("Force a customs write-off against a declaration.")
            .WithDescription(
                "Creates a customs write-off against the declaration's MRN for the supplied "
                    + "consignment items, without waiting for the declaration to clear. Returns 200 "
                    + "when TRACES executes it, or the upstream failure as a problem response."
            )
            .Validates<ChedReservationInterventionRequest>()
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapPut("customs/cheds/{chedId}/declarations/{mrn}/reservation/manual-release", AmendWriteOff)
            .WithName("AmendWriteOff")
            .WithSummary("Amend an existing customs write-off.")
            .WithDescription(
                "Replaces the quantities written off against the declaration's MRN with the "
                    + "supplied consignment items. Returns 200 when TRACES executes the amendment, "
                    + "or the upstream failure."
            )
            .Validates<ChedReservationInterventionRequest>()
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapDelete("customs/cheds/{chedId}/declarations/{mrn}/reservation/manual-release", DeleteWriteOff)
            .WithName("DeleteWriteOff")
            .WithSummary("Delete a customs write-off.")
            .WithDescription(
                "Removes the write-off held against the declaration's MRN. Returns 200 when "
                    + "TRACES executes the deletion, or the upstream failure."
            )
            .Validates<ChedReservationInterventionRequest>()
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapPut("customs/cheds/{chedId}/declarations/{mrn}/reservation/release", Release)
            .WithName("ReleaseReservation")
            .WithSummary("Release a CHED reservation.")
            .WithDescription(
                "Releases the quantities reserved against the declaration's MRN. "
                    + "If successful, returns a 200, if not then it will respond with the TRACES error code."
            )
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapDelete("customs/cheds/{chedId}/declarations/{mrn}/reservation", DeleteReservation)
            .WithName("DeleteReservation")
            .WithSummary("Delete a CHED reservation.")
            .WithDescription(
                "Cancels the reservation held against the declaration's MRN. "
                    + "If successful, returns a 200, if not then it will respond with the TRACES error code."
            )
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);
    }

    /// <param name="chedId" example="CHEDA.GB.2024.1020304">CHED reference whose quantity position is read.</param>
    /// <param name="customsChedService">Reads the quantity position from TracesNT.</param>
    /// <param name="acceptLanguage">Preferred language for returned names, as a BCP 47 language tag.</param>
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

    /// <param name="chedId" example="CHEDA.GB.2024.1020304">CHED reference the reservation is made against.</param>
    /// <param name="mrn">Movement Reference Number of the customs declaration holding the reservation.</param>
    /// <param name="request">Consignment items to reserve.</param>
    /// <param name="customsChedService">Sends the reservation to TracesNT.</param>
    /// <param name="loggerFactory">Creates the logger for the refusal path.</param>
    /// <param name="acceptLanguage">Preferred language for returned names, as a BCP 47 language tag.</param>
    private static async Task<IResult> PutReservation(
        string chedId,
        string mrn,
        [FromBody] ChedReservationRequest request,
        ICustomsChedService customsChedService,
        ILoggerFactory loggerFactory,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
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

    /// <param name="chedId" example="CHEDA.GB.2024.1020304">CHED reference the write-off applies to.</param>
    /// <param name="mrn">Movement Reference Number of the customs declaration.</param>
    /// <param name="request">Consignment items to write off.</param>
    /// <param name="customsChedService">Sends the write-off to TracesNT.</param>
    /// <param name="acceptLanguage">Preferred language for returned names, as a BCP 47 language tag.</param>
    private static Task<IResult> ForceWriteOff(
        string chedId,
        string mrn,
        [FromBody] ChedReservationInterventionRequest request,
        ICustomsChedService customsChedService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        return ProcessWriteIntervention(
            chedId,
            mrn,
            request,
            customsChedService,
            InterventionType.ForceWriteOff,
            acceptLanguage
        );
    }

    /// <param name="chedId" example="CHEDA.GB.2024.1020304">CHED reference the write-off applies to.</param>
    /// <param name="mrn">Movement Reference Number of the customs declaration.</param>
    /// <param name="request">Consignment items to write off.</param>
    /// <param name="customsChedService">Sends the amendment to TracesNT.</param>
    /// <param name="acceptLanguage">Preferred language for returned names, as a BCP 47 language tag.</param>
    private static Task<IResult> AmendWriteOff(
        string chedId,
        string mrn,
        [FromBody] ChedReservationInterventionRequest request,
        ICustomsChedService customsChedService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        return ProcessWriteIntervention(
            chedId,
            mrn,
            request,
            customsChedService,
            InterventionType.DeleteWriteOff,
            acceptLanguage
        );
    }

    /// <param name="chedId" example="CHEDA.GB.2024.1020304">CHED reference the write-off applies to.</param>
    /// <param name="mrn">Movement Reference Number of the customs declaration.</param>
    /// <param name="request">Consignment items identifying the write-off to delete.</param>
    /// <param name="customsChedService">Sends the deletion to TracesNT.</param>
    /// <param name="acceptLanguage">Preferred language for returned names, as a BCP 47 language tag.</param>
    private static Task<IResult> DeleteWriteOff(
        string chedId,
        string mrn,
        [FromBody] ChedReservationInterventionRequest request,
        ICustomsChedService customsChedService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        return ProcessWriteIntervention(
            chedId,
            mrn,
            request,
            customsChedService,
            InterventionType.DeleteWriteOff,
            acceptLanguage
        );
    }

    /// <param name="chedId" example="CHEDA.GB.2024.1020304">CHED reference whose reservation is released.</param>
    /// <param name="mrn">Movement Reference Number of the customs declaration holding the reservation.</param>
    /// <param name="customsChedService">Sends the release to TracesNT.</param>
    /// <param name="acceptLanguage">Preferred language for returned names, as a BCP 47 language tag.</param>
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

    private static async Task<IResult> ProcessWriteIntervention(
        string chedId,
        string mrn,
        ChedReservationInterventionRequest request,
        ICustomsChedService customsChedService,
        InterventionType interventionType,
        string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);
        var response = await customsChedService.ReservationIntervention(
            chedId,
            mrn,
            request.ConsignmentItems.ToCertexConsignmentItems().ToArray(),
            interventionType.ToCertexInterventionType(),
            languageCode,
            request.TaricDocument
        );

        var outcome = response?.QuantityManagementOutcome;

        // If successful there is nothing to report
        if (QuantityManagementOutcomes.IsSuccess(outcome))
        {
            return Results.Ok();
        }

        // interpret the issue and report accordingly
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

    /// <param name="chedId" example="CHEDA.GB.2024.1020304">CHED reference whose reservation is deleted.</param>
    /// <param name="mrn">Movement Reference Number of the customs declaration holding the reservation.</param>
    /// <param name="customsChedService">Sends the cancellation to TracesNT.</param>
    /// <param name="acceptLanguage">Preferred language for returned names, as a BCP 47 language tag.</param>
    private static async Task<IResult> DeleteReservation(
        string chedId,
        string mrn,
        ICustomsChedService customsChedService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);
        var response = await customsChedService.DeleteReservation(chedId, mrn, languageCode);

        var outcome = response?.QuantityManagementOutcome;

        // A clean cancellation has nothing to report. Outcome 04 does — the CHED status
        // changed mid-clearance — so it always comes back with a body.
        if (QuantityManagementOutcomes.IsSuccess(outcome))
        {
            return Results.NoContent();
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
