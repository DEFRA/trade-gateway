using System.Globalization;
using System.Text.Json.Serialization;
using Defra.TradeImports.Messaging;

namespace Api.Attachments;

/// <summary>
/// Internal message asking the download consumer to fetch a CHED attachment from TRACES and hand it to
/// the CDP uploader. Grouped on the attachment id and de-duplicated on the attachment id and request
/// number: two requests for the same attempt collapse into one, while a retry is a new message.
/// </summary>
public sealed record ChedAttachmentDownloadRequested(
    [property: JsonPropertyName("chedId")] string ChedId,
    [property: JsonPropertyName("attachmentId")] long AttachmentId,
    [property: JsonPropertyName("request")] int Request = 1
) : IMessage
{
    [JsonIgnore]
    public string DuplicationId => string.Create(CultureInfo.InvariantCulture, $"{AttachmentId}-{Request}");

    [JsonIgnore]
    public string MessageGroupId => AttachmentId.ToString(CultureInfo.InvariantCulture);
}
