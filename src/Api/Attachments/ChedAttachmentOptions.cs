using System.ComponentModel.DataAnnotations;

namespace Api.Attachments;

public class ChedAttachmentOptions
{
    public const string SectionName = "ChedAttachments";

    /// <summary>FIFO topic the endpoint publishes download requests to.</summary>
    [Required]
    public required string TopicArn { get; init; }

    /// <summary>FIFO queue, subscribed to <see cref="TopicArn"/>, that the download consumer reads.</summary>
    [Required]
    public required string QueueUrl { get; init; }

    /// <summary>Lets a deployment (or the tests) run the API without polling the queue.</summary>
    public bool ConsumerEnabled { get; init; } = true;

    /// <summary>
    /// SQS visibility timeout for a download. Must cover the TRACES download plus the hand-off to the
    /// CDP uploader; a message that outlives it is delivered again.
    /// </summary>
    [Range(30, 43200)]
    public int VisibilityTimeoutSeconds { get; init; } = 120;

    /// <summary>Bucket the CDP uploader writes scanned attachments to. Must be in the uploader's allowlist.</summary>
    [Required]
    public required string S3Bucket { get; init; }

    /// <summary>Folder in <see cref="S3Bucket"/>; files land under <c>{S3Path}/{chedId}/{attachmentId}/</c>.</summary>
    [Required]
    public string S3Path { get; init; } = "ched-attachments";

    /// <summary>
    /// How long an attachment record lives in Mongo (TTL index on <c>createdAt</c>). Keep it shorter
    /// than the bucket's expiry rule (40 days) so a record never points at an object that has gone.
    /// Changing it updates the existing TTL index in place on startup.
    /// </summary>
    public TimeSpan RecordTimeToLive { get; init; } = TimeSpan.FromDays(30);

    /// <summary>
    /// A request still in progress (requested, downloading or scanning) is assumed lost, and queued
    /// again, when the endpoint sees it untouched for this long. Failures with a retriable code are
    /// queued again on the next request, whatever their age.
    /// </summary>
    public TimeSpan RetryAfter { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>Optional size limit passed to the CDP uploader, in bytes.</summary>
    public long? MaxFileSize { get; init; }

    /// <summary>
    /// The file types attachments may be; anything else fails with <c>DDF_09</c> before
    /// it is uploaded. Also sent to the CDP uploader as its <c>mimeTypes</c>, and the scanner's detected
    /// type must fit the declared one. Set in appsettings.json; no default here, as
    /// configuration binding appends to a list rather than replacing it.
    /// </summary>
    [MinLength(1)]
    public List<AllowedFileType> AllowedFileTypes { get; init; } = [];
}
