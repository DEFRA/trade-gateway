using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Certificate;

/// <summary>
/// The state of a CHED attachment download. Returned by the attachment endpoint in every case, and by
/// the download endpoint when there is no file to return yet.
/// </summary>
public sealed record AttachmentDownloadStatus
{
    /// <summary>The certificate reference.</summary>
    [JsonPropertyName("certificateId")]
    public required string CertificateId { get; init; }

    /// <summary>The attachment's document id.</summary>
    [JsonPropertyName("attachmentId")]
    public required long AttachmentId { get; init; }

    /// <summary>One of <c>Requested</c>, <c>Downloading</c>, <c>Scanning</c>, <c>Available</c> or <c>Failed</c>.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>When the request was last progressed (UTC).</summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>The attachment's file name, once it has been fetched from TRACES.</summary>
    [JsonPropertyName("fileName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileName { get; init; }

    /// <summary>The attachment's content type, once it has been fetched from TRACES.</summary>
    [JsonPropertyName("contentType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentType { get; init; }

    /// <summary>The attachment's size in bytes, once it has been fetched from TRACES.</summary>
    [JsonPropertyName("contentLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ContentLength { get; init; }

    /// <summary>Where to download the file from; only when <see cref="Status"/> is <c>Available</c>.</summary>
    [JsonPropertyName("downloadUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DownloadUrl { get; init; }

    /// <summary>Why the download failed; only when <see cref="Status"/> is <c>Failed</c>.</summary>
    [JsonPropertyName("failure")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AttachmentDownloadFailure? Failure { get; init; }
}

/// <summary>Why a CHED attachment download failed.</summary>
public sealed record AttachmentDownloadFailure
{
    /// <summary>
    /// <c>DDF_01</c> TRACES service unavailable, <c>DDF_02</c> document retrieval timeout, <c>DDF_03</c>
    /// TRACES request rate limited, <c>DDF_04</c> CDP uploader failure, <c>DDF_05</c> S3 storage failure,
    /// <c>DDF_06</c> invalid credentials, <c>DDF_07</c> permission denied, <c>DDF_08</c> malware detected,
    /// <c>DDF_09</c> unsupported file type, <c>DDF_10</c> file exceeds maximum size, <c>DDF_11</c>
    /// document not found or no longer available, <c>DDF_99</c> any other reason.
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>The failure reason.</summary>
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    /// <summary>
    /// Whether requesting the attachment again starts a new download (<c>DDF_01</c> to <c>DDF_05</c>
    /// and <c>DDF_99</c>). Other failures are returned as they are.
    /// </summary>
    [JsonPropertyName("retriable")]
    public bool Retriable { get; init; }

    /// <summary>When the failure happened (UTC).</summary>
    [JsonPropertyName("occurredAt")]
    public DateTimeOffset OccurredAt { get; init; }
}
