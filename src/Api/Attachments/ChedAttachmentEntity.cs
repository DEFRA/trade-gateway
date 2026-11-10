using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Api.Attachments;

public enum ChedAttachmentStatus
{
    /// <summary>A download has been queued and not yet picked up.</summary>
    Requested,

    /// <summary>The consumer is fetching the attachment from TRACES.</summary>
    Downloading,

    /// <summary>Handed to the CDP uploader; waiting for the malware scan callback.</summary>
    Scanning,

    /// <summary>Scanned clean and stored in S3.</summary>
    Available,

    /// <summary>See <see cref="ChedAttachmentEntity.Error"/>.</summary>
    Failed,
}

public enum AttachmentErrorSource
{
    Traces,
    Uploader,
    Scan,
    Internal,
}

public sealed class AttachmentError
{
    [BsonRepresentation(BsonType.String)]
    public required AttachmentErrorSource Source { get; init; }

    /// <summary>Stable code, returned to the caller. See <see cref="AttachmentErrorCodes"/>.</summary>
    public required string Code { get; init; }

    /// <summary>Safe to return to the caller.</summary>
    public required string Message { get; init; }

    /// <summary>Internal diagnostic (exception type and message). Never returned to the caller.</summary>
    public string? Detail { get; init; }

    /// <summary>Whether asking again later could succeed; false for e.g. a file that failed its virus scan.</summary>
    public bool Retryable { get; init; }

    public DateTime OccurredAt { get; init; }
}

/// <summary>
/// The state of one CHED attachment download. Expires <see cref="ChedAttachmentOptions.RecordTimeToLive"/>
/// after <see cref="CreatedAt"/>.
/// </summary>
public sealed class ChedAttachmentEntity
{
    [BsonId]
    public required string Id { get; init; }

    public required string ChedId { get; init; }

    public required long AttachmentId { get; init; }

    [BsonRepresentation(BsonType.String)]
    public ChedAttachmentStatus Status { get; set; }

    public string? FileName { get; set; }

    public string? ContentType { get; set; }

    public long? ContentLength { get; set; }

    /// <summary>The CDP uploader's id for the current upload.</summary>
    public string? UploadId { get; set; }

    /// <summary>
    /// Random value sent in the upload's metadata and echoed back on the scan callback, which proves
    /// the callback belongs to the current upload (the callback carries no other credential).
    /// </summary>
    public string? CallbackToken { get; set; }

    public string? S3Bucket { get; set; }

    public string? S3Key { get; set; }

    public string? ChecksumSha256 { get; set; }

    public AttachmentError? Error { get; set; }

    /// <summary>How many times the download has been attempted.</summary>
    public int Attempts { get; set; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; set; }

    public static string CreateId(string chedId, long attachmentId) => $"{chedId}/{attachmentId}";

    public static ChedAttachmentEntity NewRequest(string chedId, long attachmentId, DateTime now) =>
        new()
        {
            Id = CreateId(chedId, attachmentId),
            ChedId = chedId,
            AttachmentId = attachmentId,
            Status = ChedAttachmentStatus.Requested,
            CreatedAt = now,
            UpdatedAt = now,
        };
}
