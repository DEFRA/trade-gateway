using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Defra.TradeImports.CdpUploader;
using Microsoft.Extensions.Options;

namespace Api.Attachments;

public enum ScanResultsOutcome
{
    Recorded,

    /// <summary>Already processed (the uploader redelivers callbacks until it gets a 2xx).</summary>
    AlreadyProcessed,

    /// <summary>The upload has not finished scanning; the uploader should call again.</summary>
    NotFinal,

    /// <summary>Missing metadata.</summary>
    Invalid,

    /// <summary>No such attachment, or the token does not match its current upload.</summary>
    Unknown,
}

public interface IScanResultsHandler
{
    Task<ScanResultsOutcome> Handle(UploadStatusResponse payload, CancellationToken cancellationToken);
}

/// <summary>
/// Applies the CDP uploader's scan-results callback to the attachment record: available, with its S3
/// location, or failed with the uploader's error code (<c>FILE_VIRUS</c> and so on).
/// </summary>
public class ScanResultsHandler(
    IChedAttachmentRepository repository,
    IOptions<ChedAttachmentOptions> options,
    TimeProvider timeProvider,
    ILogger<ScanResultsHandler> logger
) : IScanResultsHandler
{
    public async Task<ScanResultsOutcome> Handle(UploadStatusResponse payload, CancellationToken cancellationToken)
    {
        var chedId = payload.GetMetadata(ScanResultsMetadata.ChedId);
        var token = payload.GetMetadata(ScanResultsMetadata.CallbackToken);
        if (
            string.IsNullOrWhiteSpace(chedId)
            || string.IsNullOrWhiteSpace(token)
            || !long.TryParse(
                payload.GetMetadata(ScanResultsMetadata.AttachmentId),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var attachmentId
            )
        )
        {
            logger.LogWarning("Scan results callback without attachment metadata ignored");
            return ScanResultsOutcome.Invalid;
        }

        var record = await repository.Get(ChedAttachmentEntity.CreateId(chedId, attachmentId), cancellationToken);
        if (record?.CallbackToken is null || !TokensMatch(record.CallbackToken, token))
        {
            logger.LogWarning(
                "Scan results callback for unknown or superseded upload of CHED attachment {ChedId} - {AttachmentId}",
                chedId,
                attachmentId
            );
            return ScanResultsOutcome.Unknown;
        }

        if (record.Status != ChedAttachmentStatus.Scanning)
            return ScanResultsOutcome.AlreadyProcessed;

        var file = payload.GetFile();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // The uploader only checks the declared type against mimeTypes; check what it actually found.
        if (
            file is { IsComplete: true, DetectedContentType: { } detected }
            && !AttachmentFileTypes.IsAllowed(options.Value.AllowedFileTypes, detected)
        )
        {
            record.Status = ChedAttachmentStatus.Failed;
            record.Error = AttachmentErrorCodes.ContentNotAllowed(detected, now);
            logger.LogWarning(
                "CHED attachment {Id} was declared {ContentType} but its content is {Detected}",
                record.Id,
                record.ContentType,
                detected
            );
        }
        else if (file is { IsComplete: true })
        {
            record.Status = ChedAttachmentStatus.Available;
            record.S3Bucket = file.S3Bucket;
            record.S3Key = file.S3Key;
            record.ChecksumSha256 = file.ChecksumSha256;
            record.ContentLength = file.ContentLength ?? record.ContentLength;
            record.Error = null;
        }
        else if (file is { IsRejected: true })
        {
            record.Status = ChedAttachmentStatus.Failed;
            record.Error = AttachmentErrorCodes.ScanRejected(file.ErrorCode, file.ErrorMessage, now);
            logger.LogWarning(
                "CHED attachment {Id} was rejected by the scan: {Code} {Message}",
                record.Id,
                record.Error.Code,
                file.ErrorMessage
            );
        }
        else if (file is null && payload.IsReady)
        {
            // Ready with no file: the uploader received nothing, so treat it as its own failure.
            record.Status = ChedAttachmentStatus.Failed;
            record.Error = AttachmentErrorCodes.ScanRejected(null, null, now);
        }
        else
        {
            return ScanResultsOutcome.NotFinal;
        }

        record.UpdatedAt = now;
        await repository.Replace(record, cancellationToken);

        logger.LogInformation("CHED attachment {Id} is now {Status}", record.Id, record.Status);
        return ScanResultsOutcome.Recorded;
    }

    private static bool TokensMatch(string expected, string actual) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));
}
