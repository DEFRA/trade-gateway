using System.Globalization;
using System.Text.Json.Serialization;
using Defra.TradeImports.Messaging;

namespace Api.Attachments;

/// <summary>
/// Internal message asking the download consumer to fetch a CHED attachment from TRACES and hand it to
/// the CDP uploader. De-duplicated, and grouped, on the attachment id.
/// </summary>
public sealed record ChedAttachmentDownloadRequested(
    [property: JsonPropertyName("chedId")] string ChedId,
    [property: JsonPropertyName("attachmentId")] long AttachmentId
) : IMessage
{
    [JsonIgnore]
    public string DuplicationId => AttachmentId.ToString(CultureInfo.InvariantCulture);

    [JsonIgnore]
    public string MessageGroupId => DuplicationId;
}
