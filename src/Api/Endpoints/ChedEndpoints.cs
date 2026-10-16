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
    public static void UseChedEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("certificates/cheds/{id}", Get)
            .WithName("GetChed")
            .WithSummary("Fetch a single CHED certificate by its reference.")
            .WithDescription(
                "Returns the full CHED profile. A draft certificate (statusCode 47) is not served " + "and returns 404."
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
                    + "(uri:documentid:{attachmentId}); the file is returned with its original content type "
                    + "and file name. A draft certificate (statusCode 47) is not served "
                    + "and returns 404."
            )
            .Produces<Stream>(StatusCodes.Status200OK, "application/octet-stream")
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
                    + "whether there are more pages to follow. Draft certificates (statusCode 47) "
                    + "are omitted from items; hasMore is computed from the "
                    + "unfiltered upstream page."
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
            DraftNotification.LogIgnored(loggerFactory, id);
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
        IChedCertificateService chedCertificateService,
        ILoggerFactory loggerFactory,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);
        var ched = await chedCertificateService.GetChedCertificate(id, languageCode);
        if (ched?.SPSCertificate == null)
            return ChedNotFound(id);

        if (DraftNotification.IsDraft(ched.SPSCertificate))
        {
            DraftNotification.LogIgnored(loggerFactory, id);
            return ChedNotFound(id);
        }

        var filename = FindAttachmentFileName(ched.SPSCertificate, attachmentId);
        if (filename == null)
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Ched certificate attachment '{id} - {attachmentId}' was not found."
            );

        var attachment = await chedCertificateService.GetChedCertificateAttachment(id, attachmentId, filename);
        if (attachment?.Attachment == null)
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Ched certificate attachment '{id} - {attachmentId}' was not found."
            );

        return Results.Bytes(attachment.Attachment, attachment.contentType, attachment.fileName);
    }

    // TracesNT identifies attachments by a uri of the form "uri:documentid:{documentId}", with the
    // filename alongside it on the same BinaryObjectType.
    private static string? FindAttachmentFileName(SPSCertificateType certificate, long attachmentId)
    {
        var tradeLineItemDocuments =
            certificate
                .SPSConsignment?.IncludedSPSConsignmentItem?.SelectMany(item => item.IncludedSPSTradeLineItem ?? [])
                .SelectMany(lineItem => lineItem.ReferenceSPSReferencedDocument ?? [])
            ?? [];

        return (certificate.SPSExchangedDocument?.ReferenceSPSReferencedDocument ?? [])
            .Concat(tradeLineItemDocuments)
            .SelectMany(document => document.AttachmentBinaryObject ?? [])
            .FirstOrDefault(binaryObject => binaryObject.uri == $"uri:documentid:{attachmentId}")
            ?.filename;
    }

    private static async Task<IResult> Find(
        [AsParameters] FindCertificatesRequest query,
        [FromServices] IChedCertificateService chedCertificateService,
        ILoggerFactory loggerFactory
    )
    {
        var certificates = await chedCertificateService.FindChedCertificates(
            query.UpdatedFrom!.Value.UtcDateTime,
            query.UpdatedBefore!.Value.UtcDateTime,
            query.Offset!.Value,
            query.PageSize!.Value,
            query.AcceptLanguage!
        );
        var page = certificates.FindChedCertificateResponse1;

        foreach (var draft in (page.ChedCertificateResult ?? []).Where(result => DraftNotification.IsDraft(result)))
            DraftNotification.LogIgnored(loggerFactory, draft.ID);

        return Results.Json(ChedMapper.Map(page), contentType: MediaTypeAttribute.For<DefraUNVTDCHEDSummaryProfile>());
    }

    private static IResult ChedNotFound(string id) =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Ched certificate '{id}' was not found.");
}
