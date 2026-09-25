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
            .WithName("GetChedCertificate")
            .WithSummary("Get a CHED certificate")
            .WithDescription(
                "Retrieves the CHED certificate with the given reference from TRACES NT, mapped to the "
                    + "DEFRA UN/CEFACT CHED profile. Text is localised using the Accept-Language header."
            )
            .Produces<DefraUNVTDCHEDProfile>(200, MediaTypeAttribute.For<DefraUNVTDCHEDProfile>())
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
                    + "and file name."
            )
            .Produces<Stream>(StatusCodes.Status200OK, "application/octet-stream")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapGet("certificates/cheds", Find)
            .WithName("FindChedCertificates")
            .WithSummary("Find updated CHED certificates")
            .WithDescription(
                "Returns a page of summaries of CHED certificates updated between updatedFrom and "
                    + "updatedBefore. Use offset and pageSize to page through the results."
            )
            .Produces<DefraUNVTDCHEDSummaryProfile>(200, MediaTypeAttribute.For<DefraUNVTDCHEDSummaryProfile>())
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

    private static async Task<IResult> GetAttachment(
        string id,
        long attachmentId,
        IChedCertificateService chedCertificateService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);
        var ched = await chedCertificateService.GetChedCertificate(id, languageCode);
        if (ched?.SPSCertificate == null)
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Ched certificate '{id}' was not found."
            );

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
