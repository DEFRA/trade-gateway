using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Certificate;

/// <summary>
/// Returned with 202 Accepted while an attachment is being fetched from TRACES and malware scanned.
/// Ask again for the same URL; once the file is ready it is returned with 200.
/// </summary>
public sealed record AttachmentDownloadStatus
{
    /// <summary>The certificate reference.</summary>
    [JsonPropertyName("certificateId")]
    public required string CertificateId { get; init; }

    /// <summary>The attachment's document id.</summary>
    [JsonPropertyName("attachmentId")]
    public required long AttachmentId { get; init; }

    /// <summary>One of <c>Requested</c>, <c>Downloading</c> or <c>Scanning</c>.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>When the request was last progressed (UTC).</summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}
