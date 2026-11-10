using Api.Attachments;
using Api.Contract;
using Api.Mapping;
using Api.Models;
using Api.Utils.Http;
using Microsoft.AspNetCore.Mvc;
using TracesNT.Services;
using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Endpoints;

public static class ChedEndpoints
{
    private const string RetryAfterSeconds = "5";

    public static void UseChedEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("certificates/cheds/{id}", Get)
            .WithName("GetChed")
            .WithSummary("Fetch a single CHED certificate by its reference.")
            .WithDescription(
                "Returns the full CHED profile. A draft certificate (statusCode 47) is not served and returns 404."
            )
            .Produces<DefraUNVTDCHEDProfile>(200, MediaTypeAttribute.For<DefraUNVTDCHEDProfile>())
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapGet("certificates/cheds/{id}/attachments/{attachmentId}", GetAttachment)
            .WithName("GetChedCertificateAttachment")
            .WithSummary("Get a CHED certificate attachment")
            .WithDescription(
                "Downloads a supporting document attached to the CHED certificate. The attachment id is the "
                    + "document id from the certificate's referenced document attachment "
                    + "(uri:documentid:{attachmentId}).\n\n"
                    + "Attachments are fetched from TRACES in the background and malware scanned before they "
                    + "are served. The first request queues the download and returns 202 Accepted with a "
                    + "Retry-After header; ask again for the same URL until the file is returned with 200, "
                    + "with its original content type and file name. Scanned files are kept for 180 days.\n\n"
                    + "Only PDF attachments are currently supported; others return 422 UNSUPPORTED_FILE_TYPE.\n\n"
                    + "A failed download returns a problem with an `errorCode`: CHED_NOT_FOUND or "
                    + "ATTACHMENT_NOT_FOUND (404, which includes draft certificates, statusCode 47), "
                    + "TRACES_PERMISSION_DENIED (403), UNSUPPORTED_FILE_TYPE (422, not a supported type), "
                    + "FILE_VIRUS, FILE_EMPTY, FILE_TOO_LARGE or FILE_INVALID_TYPE (422, the file failed its "
                    + "scan, including content that is not the type it claims to be), TRACES_UNAVAILABLE, UPLOAD_FAILED "
                    + "or FILE_UPLOAD_FAILED (502) and TRACES_INVALID_REQUEST or UNEXPECTED_ERROR (500). "
                    + "Failures other than a failed scan are tried again on a request made 15 minutes later."
            )
            .Produces<Stream>(StatusCodes.Status200OK, "application/octet-stream")
            .Produces<AttachmentDownloadStatus>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .AllowAnonymous();

        app.MapGet("certificates/cheds", Find)
            .WithName("FindCheds")
            .WithSummary("Search CHED certificates updated in a time window.")
            .WithDescription(
                "Returns a paged summary of CHED certificates updated between updatedFrom and "
                    + "updatedBefore (both UTC). Paginate with offset and pageSize; hasMore indicates "
                    + "whether there are more pages to follow. Draft certificates (statusCode 47) "
                    + "are excluded by the upstream query."
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
        ILoggerFactory loggerFactory,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);
        var context = new MappingContext(languageCode);
        var certificate = await chedCertificateService.GetChedCertificate(id, languageCode);
        if (certificate?.SPSCertificate == null)
            return ChedNotFound(id);

        if (DraftNotification.IsDraft(certificate.SPSCertificate))
        {
            DraftNotification.LogIgnored(loggerFactory, "CHED", id);
            return ChedNotFound(id);
        }

        return Results.Json(
            ChedMapper.Map(certificate, context),
            contentType: MediaTypeAttribute.For<DefraUNVTDCHEDProfile>()
        );
    }

    private static async Task<IResult> GetAttachment(
        string id,
        long attachmentId,
        IChedAttachmentService attachmentService,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var lookup = await attachmentService.GetOrRequest(id, attachmentId, cancellationToken);

        switch (lookup)
        {
            case ChedAttachmentLookup.Ready ready:
                httpContext.Response.RegisterForDisposeAsync(ready.File);
                return Results.Stream(
                    ready.File.Content,
                    ready.Record.ContentType ?? "application/octet-stream",
                    ready.Record.FileName
                );

            case ChedAttachmentLookup.Failed failed:
                return Results.Problem(
                    statusCode: AttachmentErrorCodes.ToStatusCode(failed.Error.Code),
                    detail: failed.Error.Message,
                    extensions: new Dictionary<string, object?>
                    {
                        ["errorCode"] = failed.Error.Code,
                        ["errorSource"] = failed.Error.Source.ToString(),
                    }
                );

            default:
                httpContext.Response.Headers.RetryAfter = RetryAfterSeconds;
                return Results.Accepted(
                    $"/certificates/cheds/{Uri.EscapeDataString(id)}/attachments/{attachmentId}",
                    new AttachmentDownloadStatus
                    {
                        CertificateId = lookup.Record.ChedId,
                        AttachmentId = lookup.Record.AttachmentId,
                        Status = lookup.Record.Status.ToString(),
                        UpdatedAt = new DateTimeOffset(lookup.Record.UpdatedAt, TimeSpan.Zero),
                    }
                );
        }
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

    private static IResult ChedNotFound(string id) =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Ched certificate '{id}' was not found.");
}
