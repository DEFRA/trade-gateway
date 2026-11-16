using Defra.TradeImports.Messaging.Publishing;
using Microsoft.Extensions.Options;

namespace Api.Attachments;

public abstract record ChedAttachmentLookup(ChedAttachmentEntity Record)
{
    /// <summary>Scanned and stored in S3.</summary>
    public sealed record Available(ChedAttachmentEntity Record) : ChedAttachmentLookup(Record);

    /// <summary>Scanned, stored and opened; <see cref="File"/> must be disposed (the response does so).</summary>
    public sealed record Ready(ChedAttachmentEntity Record, StoredAttachment File) : ChedAttachmentLookup(Record);

    /// <summary>Queued, downloading or being scanned.</summary>
    public sealed record Pending(ChedAttachmentEntity Record) : ChedAttachmentLookup(Record);

    /// <summary>Failed with a code that is not retried, or the stored file could not be read.</summary>
    public sealed record Failed(ChedAttachmentEntity Record, AttachmentError Error) : ChedAttachmentLookup(Record);
}

public interface IChedAttachmentService
{
    /// <summary>
    /// The attachment's state. An attachment never seen before, or one that failed with a retriable
    /// code, is queued for download.
    /// </summary>
    Task<ChedAttachmentLookup> GetOrRequest(string chedId, long attachmentId, CancellationToken cancellationToken);

    /// <summary>
    /// As <see cref="GetOrRequest"/>, but opens the stored file when it is available
    /// (<see cref="ChedAttachmentLookup.Ready"/>). A file missing from the bucket is queued again.
    /// </summary>
    Task<ChedAttachmentLookup> OpenOrRequest(string chedId, long attachmentId, CancellationToken cancellationToken);
}

public class ChedAttachmentService(
    IChedAttachmentRepository repository,
    IAttachmentStore store,
    ITopicPublisher<ChedAttachmentDownloadRequested> publisher,
    IOptions<ChedAttachmentOptions> options,
    TimeProvider timeProvider,
    ILogger<ChedAttachmentService> logger
) : IChedAttachmentService
{
    public async Task<ChedAttachmentLookup> GetOrRequest(
        string chedId,
        long attachmentId,
        CancellationToken cancellationToken
    )
    {
        var id = ChedAttachmentEntity.CreateId(chedId, attachmentId);
        var record = await repository.Get(id, cancellationToken);

        if (record is null)
        {
            var created = ChedAttachmentEntity.NewRequest(chedId, attachmentId, Now);
            created.Requests = 1;
            if (await repository.TryInsert(created, cancellationToken))
            {
                await PublishNew(created, cancellationToken);
                return new ChedAttachmentLookup.Pending(created);
            }

            // Another request created it first; carry on with theirs.
            record = await repository.Get(id, cancellationToken) ?? created;
        }

        switch (record.Status)
        {
            case ChedAttachmentStatus.Available:
                return new ChedAttachmentLookup.Available(record);

            case ChedAttachmentStatus.Failed:
                var error = record.Error ?? UnknownError(record);
                if (AttachmentFailureCodes.IsRetriable(error.Code) || IsNowAllowed(record, error))
                {
                    logger.LogInformation(
                        "Retrying CHED attachment {Id} after {Code} at {OccurredAt}",
                        record.Id,
                        error.Code,
                        error.OccurredAt
                    );
                    await Requeue(record, cancellationToken);
                    return new ChedAttachmentLookup.Pending(record);
                }

                return new ChedAttachmentLookup.Failed(record, error);

            default:
                // Requested, Downloading or Scanning. A request stuck that long has lost its message
                // (the consumer died mid-download, or the scan callback never came), so ask again.
                if (IsStuck(record))
                {
                    logger.LogWarning(
                        "CHED attachment {Id} has been {Status} since {UpdatedAt}; requesting it again",
                        record.Id,
                        record.Status,
                        record.UpdatedAt
                    );
                    await Requeue(record, cancellationToken);
                }

                return new ChedAttachmentLookup.Pending(record);
        }
    }

    public async Task<ChedAttachmentLookup> OpenOrRequest(
        string chedId,
        long attachmentId,
        CancellationToken cancellationToken
    )
    {
        var lookup = await GetOrRequest(chedId, attachmentId, cancellationToken);
        if (lookup is not ChedAttachmentLookup.Available { Record: var record })
            return lookup;

        StoredAttachment? file;
        try
        {
            file = await store.Open(record.S3Bucket!, record.S3Key!, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Not recorded: the file is stored, the bucket just could not be read this time.
            logger.LogWarning(ex, "Could not read CHED attachment {Id} from {Bucket}", record.Id, record.S3Bucket);
            return new ChedAttachmentLookup.Failed(record, AttachmentFailureCodes.FromException(ex, Now));
        }

        if (file is not null)
            return new ChedAttachmentLookup.Ready(record, file);

        logger.LogWarning(
            "CHED attachment {Id} is recorded as available but {Bucket}/{Key} is missing; downloading again",
            record.Id,
            record.S3Bucket,
            record.S3Key
        );
        await Requeue(record, cancellationToken);
        return new ChedAttachmentLookup.Pending(record);
    }

    private async Task PublishNew(ChedAttachmentEntity record, CancellationToken cancellationToken)
    {
        try
        {
            await Publish(record, cancellationToken);
        }
        catch
        {
            // Without its message the record would sit as Requested; remove it so the caller can retry.
            await repository.Delete(record.Id, CancellationToken.None);
            throw;
        }
    }

    private async Task Requeue(ChedAttachmentEntity record, CancellationToken cancellationToken)
    {
        var previousStatus = record.Status;
        var previousUpdatedAt = record.UpdatedAt;

        // Saved before publishing so the consumer, which may start at once, never sees a stale status.
        record.Status = ChedAttachmentStatus.Requested;
        record.UpdatedAt = Now;
        record.Requests++;
        await repository.Replace(record, cancellationToken);

        try
        {
            await Publish(record, cancellationToken);
        }
        catch
        {
            record.Status = previousStatus;
            record.UpdatedAt = previousUpdatedAt;
            record.Requests--;
            await repository.Replace(record, CancellationToken.None);
            throw;
        }
    }

    private Task Publish(ChedAttachmentEntity record, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new ChedAttachmentDownloadRequested(record.ChedId, record.AttachmentId, record.Requests),
            cancellationToken: cancellationToken
        );

    // A type rejected before upload is asked for again, at once, after the allowed list gains it.
    private bool IsNowAllowed(ChedAttachmentEntity record, AttachmentError error) =>
        error is { Code: AttachmentFailureCodes.UnsupportedFileType, Source: AttachmentErrorSource.Traces }
        && AttachmentFileTypes.Resolve(options.Value.AllowedFileTypes, record.ContentType, record.FileName) is not null;

    private bool IsStuck(ChedAttachmentEntity record) => Now - record.UpdatedAt >= options.Value.RetryAfter;

    private static AttachmentError UnknownError(ChedAttachmentEntity record) =>
        AttachmentFailureCodes.Create(AttachmentFailureCodes.Other, AttachmentErrorSource.Internal, record.UpdatedAt);

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;
}
