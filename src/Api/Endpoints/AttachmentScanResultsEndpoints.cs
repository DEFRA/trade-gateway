using Api.Attachments;
using Defra.TradeImports.CdpUploader;

namespace Api.Endpoints;

public static class AttachmentScanResultsEndpoints
{
    public const string Path = "attachments/scan-results";

    /// <summary>
    /// The CDP uploader's scan-results callback. The uploader sends no credential, so the endpoint is
    /// anonymous and kept out of the OpenAPI document; a callback is only applied when it carries the
    /// random token generated for the attachment's current upload. In CDP it is reached on the
    /// service's internal cdp-int address, not through the API gateway.
    /// </summary>
    public static void UseAttachmentScanResultsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(Path, Handle).AllowAnonymous().ExcludeFromDescription();
    }

    private static async Task<IResult> Handle(
        UploadStatusResponse payload,
        IScanResultsHandler handler,
        CancellationToken cancellationToken
    ) =>
        await handler.Handle(payload, cancellationToken) switch
        {
            ScanResultsOutcome.Recorded or ScanResultsOutcome.AlreadyProcessed => Results.Ok(),
            // Not a 2xx, so the uploader keeps the callback and delivers it again.
            ScanResultsOutcome.NotFinal => Results.Conflict(),
            ScanResultsOutcome.Invalid => Results.BadRequest(),
            _ => Results.NotFound(),
        };
}
