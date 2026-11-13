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
            .WithSummary("Get the status of a CHED certificate attachment")
            .WithDescription(
                "Returns the state of a supporting document attached to the CHED certificate, as JSON. The "
                    + "attachment id is the document id from the certificate's referenced document attachment "
                    + "(uri:documentid:{attachmentId}).\n\n"
                    + "Attachments are fetched from TRACES in the background and malware scanned before they "
                    + "can be downloaded. The first request queues the download and returns 202 Accepted with a "
                    + "Retry-After header; ask again until the status is Available (200), when downloadUrl "
                    + "gives the file's download link. Scanned files are kept for 30 days.\n\n"
                    + "Supported attachment types are PDF, JPEG, PNG, Word (.doc) and Excel (.xls).\n\n"
                    + AttachmentFailuresDescription
            )
            .Produces<AttachmentDownloadStatus>(StatusCodes.Status200OK)
            .Produces<AttachmentDownloadStatus>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .Produces<AttachmentDownloadStatus>(StatusCodes.Status403Forbidden)
            .Produces<AttachmentDownloadStatus>(StatusCodes.Status404NotFound)
            .Produces<AttachmentDownloadStatus>(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Produces<AttachmentDownloadStatus>(StatusCodes.Status502BadGateway);

        app.MapGet("certificates/cheds/{id}/attachments/{attachmentId}/download", DownloadAttachment)
            .WithName("DownloadChedCertificateAttachment")
            .WithSummary("Download a CHED certificate attachment")
            .WithDescription(
                "Returns the scanned attachment's file, with its original content type and file name, once "
                    + "its status is Available. Until then it answers exactly as the attachment status "
                    + "endpoint does: 202 Accepted while the download is in progress (queueing it if it has "
                    + "not been requested), or the failure.\n\n"
                    + AttachmentFailuresDescription
            )
            .Produces<Stream>(StatusCodes.Status200OK, "application/octet-stream")
            .Produces<AttachmentDownloadStatus>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .Produces<AttachmentDownloadStatus>(StatusCodes.Status403Forbidden)
            .Produces<AttachmentDownloadStatus>(StatusCodes.Status404NotFound)
            .Produces<AttachmentDownloadStatus>(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Produces<AttachmentDownloadStatus>(StatusCodes.Status502BadGateway);

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

    private const string AttachmentFailuresDescription =
        "A failed download has status Failed and a failure with its code, reason and whether it is "
        + "retriable. Retriable failures are downloaded again on the next request, which returns 202: "
        + "DDF_01 TRACES service unavailable, DDF_02 document retrieval timeout, DDF_03 TRACES request "
        + "rate limited, DDF_04 CDP uploader failure, DDF_05 S3 storage failure and DDF_99 any other "
        + "reason. The others are returned as they are: DDF_06 invalid credentials (502), DDF_07 "
        + "permission denied (403), DDF_08 malware detected, DDF_09 unsupported file type and DDF_10 "
        + "file exceeds maximum size (422), and DDF_11 document not found or no longer available "
        + "(404, which includes draft certificates, statusCode 47).";

    private static async Task<IResult> GetAttachment(
        string id,
        long attachmentId,
        IChedAttachmentService attachmentService,
        HttpContext httpContext,
        CancellationToken cancellationToken
    ) => AttachmentStatus(await attachmentService.GetOrRequest(id, attachmentId, cancellationToken), httpContext);

    private static async Task<IResult> DownloadAttachment(
        string id,
        long attachmentId,
        IChedAttachmentService attachmentService,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var lookup = await attachmentService.OpenOrRequest(id, attachmentId, cancellationToken);
        if (lookup is not ChedAttachmentLookup.Ready ready)
            return AttachmentStatus(lookup, httpContext);

        httpContext.Response.RegisterForDisposeAsync(ready.File);
        if (ready.File.ContentLength is { } contentLength)
            httpContext.Response.ContentLength = contentLength;

        return Results.Stream(
            ready.File.Content,
            ready.Record.ContentType ?? "application/octet-stream",
            ready.Record.FileName
        );
    }

    private static IResult AttachmentStatus(ChedAttachmentLookup lookup, HttpContext httpContext)
    {
        var record = lookup.Record;
        var status = new AttachmentDownloadStatus
        {
            CertificateId = record.ChedId,
            AttachmentId = record.AttachmentId,
            Status =
                lookup is ChedAttachmentLookup.Failed ? nameof(ChedAttachmentStatus.Failed) : record.Status.ToString(),
            UpdatedAt = new DateTimeOffset(record.UpdatedAt, TimeSpan.Zero),
            FileName = record.FileName,
            ContentType = record.ContentType,
            ContentLength = record.ContentLength,
        };

        switch (lookup)
        {
            case ChedAttachmentLookup.Available or ChedAttachmentLookup.Ready:
                return Results.Json(status with { DownloadUrl = DownloadUrl(record) });

            case ChedAttachmentLookup.Failed { Error: var error }:
                return Results.Json(
                    status with
                    {
                        Failure = new AttachmentDownloadFailure
                        {
                            Code = error.Code,
                            Reason = error.Message,
                            Retriable = AttachmentFailureCodes.IsRetriable(error.Code),
                            OccurredAt = new DateTimeOffset(error.OccurredAt, TimeSpan.Zero),
                        },
                    },
                    statusCode: AttachmentFailureCodes.ToStatusCode(error.Code)
                );

            default:
                httpContext.Response.Headers.RetryAfter = RetryAfterSeconds;
                return Results.Json(status, statusCode: StatusCodes.Status202Accepted);
        }
    }

    private static string DownloadUrl(ChedAttachmentEntity record) =>
        $"/certificates/cheds/{Uri.EscapeDataString(record.ChedId)}/attachments/{record.AttachmentId}/download";

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
