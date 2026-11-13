using System.Globalization;
using System.Text.Json;
using Api.Mapping;
using Defra.TradeImports.CdpUploader;
using Defra.TradeImports.Messaging.Consuming;
using Microsoft.Extensions.Options;
using TracesNT.Services;

namespace Api.Attachments;

/// <summary>
/// Downloads a CHED attachment from TRACES and hands it to the CDP uploader, which scans it and moves
/// it to S3, then calls back on <c>/attachments/scan-results</c>. Every outcome, failures included, is
/// written to the attachment's Mongo record and the message is completed: a retriable failure is
/// downloaded again when the attachment is next requested, not by SQS redelivery. Only a failure to
/// write the record itself is rethrown.
/// </summary>
public class ChedAttachmentDownloadConsumer(
    IChedCertificateService chedCertificateService,
    IChedAttachmentRepository repository,
    ICdpUploaderClient uploader,
    IOptions<ChedAttachmentOptions> options,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory
) : IMessageConsumer
{
    // Attachments are binary; the language only affects the certificate text, which is not used.
    private const string LanguageCode = "en";

    private readonly ILogger _logger = loggerFactory.CreateLogger<ChedAttachmentDownloadConsumer>();

    public async Task ConsumeAsync(MessageContext context, CancellationToken cancellationToken = default)
    {
        var message =
            JsonSerializer.Deserialize<ChedAttachmentDownloadRequested>(context.Body)
            ?? throw new InvalidOperationException($"Message {context.MessageId} has no body");

        var record = await LoadOrCreate(message, cancellationToken);

        if (record.Status == ChedAttachmentStatus.Available)
        {
            _logger.LogInformation("CHED attachment {Id} is already available, ignoring request", record.Id);
            return;
        }

        if (
            record.Status == ChedAttachmentStatus.Failed
            && record.Error is { } previous
            && !AttachmentFailureCodes.IsRetriable(previous.Code)
        )
        {
            _logger.LogInformation(
                "CHED attachment {Id} failed with {Code}, which will not change on retry; ignoring request",
                record.Id,
                record.Error.Code
            );
            return;
        }

        record.Status = ChedAttachmentStatus.Downloading;
        record.Attempts++;
        record.Error = null;
        record.UploadId = null;
        record.CallbackToken = null;
        record.UpdatedAt = Now;
        await repository.Replace(record, cancellationToken);

        AttachmentError? error;
        try
        {
            error = await DownloadAndUpload(record, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Recorded and settled. A shutdown cancellation is not caught: the message is then
            // redelivered, and a record left mid-download is requested again once it goes stale.
            error = AttachmentFailureCodes.FromException(ex, Now);
        }

        if (error is not null)
            await Fail(record, error);
    }

    private async Task<AttachmentError?> DownloadAndUpload(
        ChedAttachmentEntity record,
        CancellationToken cancellationToken
    )
    {
        var ched = await chedCertificateService.GetChedCertificate(record.ChedId, LanguageCode);
        if (ched?.SPSCertificate == null)
            return ChedNotFound(record);

        if (DraftNotification.IsDraft(ched.SPSCertificate))
        {
            DraftNotification.LogIgnored(loggerFactory, "CHED", record.ChedId);
            return ChedNotFound(record);
        }

        var fileName = ChedAttachmentLocator.FindFileName(ched.SPSCertificate, record.AttachmentId);
        if (fileName == null)
            return AttachmentNotFound(record);

        var attachment = await chedCertificateService.GetChedCertificateAttachment(
            record.ChedId,
            record.AttachmentId,
            fileName
        );
        if (attachment?.Attachment == null)
            return AttachmentNotFound(record);

        var settings = options.Value;

        record.FileName = string.IsNullOrWhiteSpace(attachment.fileName) ? fileName : attachment.fileName;
        record.ContentType = string.IsNullOrWhiteSpace(attachment.contentType)
            ? "application/octet-stream"
            : attachment.contentType;
        record.ContentLength = attachment.Attachment.LongLength;

        if (settings.MaxFileSize is { } maxFileSize && record.ContentLength > maxFileSize)
        {
            return AttachmentFailureCodes.Create(
                AttachmentFailureCodes.FileTooLarge,
                AttachmentErrorSource.Traces,
                Now,
                detail: $"{record.ContentLength} bytes, maximum {maxFileSize}"
            );
        }

        // Checked before anything is uploaded. The resolved type is what the file is declared to the
        // uploader as (which checks it against mimeTypes) and what it is served as.
        var mimeType = AttachmentFileTypes.Resolve(settings.AllowedFileTypes, attachment.contentType, record.FileName);
        if (mimeType is null)
            return UnsupportedFileType(record, settings);

        record.ContentType = mimeType;
        var callbackToken = Guid.NewGuid().ToString("N");

        var upload = await uploader.Initiate(
            new InitiateUploadRequest
            {
                Redirect = "/",
                S3Bucket = settings.S3Bucket,
                S3Path = $"{settings.S3Path.TrimEnd('/')}/{record.ChedId}/{record.AttachmentId}",
                MaxFileSize = settings.MaxFileSize,
                MimeTypes = AttachmentFileTypes.MimeTypes(settings.AllowedFileTypes),
                Metadata = new Dictionary<string, string>
                {
                    [ScanResultsMetadata.ChedId] = record.ChedId,
                    [ScanResultsMetadata.AttachmentId] = record.AttachmentId.ToString(CultureInfo.InvariantCulture),
                    [ScanResultsMetadata.CallbackToken] = callbackToken,
                },
            },
            cancellationToken
        );

        // Saved before the upload, so a fast scan callback always finds the upload it belongs to.
        record.Status = ChedAttachmentStatus.Scanning;
        record.UploadId = upload.UploadId;
        record.CallbackToken = callbackToken;
        record.UpdatedAt = Now;
        await repository.Replace(record, cancellationToken);

        await uploader.Upload(upload.UploadUrl!, attachment.Attachment, record.FileName, mimeType, cancellationToken);

        _logger.LogInformation(
            "CHED attachment {Id} handed to the CDP uploader as upload {UploadId}",
            record.Id,
            upload.UploadId
        );
        return null;
    }

    private async Task Fail(ChedAttachmentEntity record, AttachmentError error)
    {
        _logger.LogWarning(
            "CHED attachment {Id} failed with {Code} ({Reason}) from {Source}: {Detail}",
            record.Id,
            error.Code,
            error.Message,
            error.Source,
            error.Detail
        );

        record.Status = ChedAttachmentStatus.Failed;
        record.Error = error;
        record.UpdatedAt = Now;

        // Not the message's token: the failure must be recorded even while shutting down.
        await repository.Replace(record, CancellationToken.None);
    }

    private async Task<ChedAttachmentEntity> LoadOrCreate(
        ChedAttachmentDownloadRequested message,
        CancellationToken cancellationToken
    )
    {
        var id = ChedAttachmentEntity.CreateId(message.ChedId, message.AttachmentId);
        var record = await repository.Get(id, cancellationToken);
        if (record is not null)
            return record;

        // The record expired, or was removed after a failed publish, while the message was queued.
        record = ChedAttachmentEntity.NewRequest(message.ChedId, message.AttachmentId, Now);
        return await repository.TryInsert(record, cancellationToken)
            ? record
            : await repository.Get(id, cancellationToken) ?? record;
    }

    private AttachmentError ChedNotFound(ChedAttachmentEntity record) =>
        AttachmentFailureCodes.Create(
            AttachmentFailureCodes.NotFound,
            AttachmentErrorSource.Traces,
            Now,
            $"Ched certificate '{record.ChedId}' was not found."
        );

    private AttachmentError UnsupportedFileType(ChedAttachmentEntity record, ChedAttachmentOptions settings) =>
        AttachmentFailureCodes.Unsupported(
            record.ContentType,
            record.FileName,
            AttachmentFileTypes.MimeTypes(settings.AllowedFileTypes),
            Now
        );

    private AttachmentError AttachmentNotFound(ChedAttachmentEntity record) =>
        AttachmentFailureCodes.Create(
            AttachmentFailureCodes.NotFound,
            AttachmentErrorSource.Traces,
            Now,
            $"Ched certificate attachment '{record.ChedId} - {record.AttachmentId}' was not found."
        );

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;
}

public static class ScanResultsMetadata
{
    public const string ChedId = "chedId";
    public const string AttachmentId = "attachmentId";
    public const string CallbackToken = "callbackToken";
}
