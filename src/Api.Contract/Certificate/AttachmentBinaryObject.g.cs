#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record AttachmentBinaryObject
{
    [JsonPropertyName("format")]
    public string? Format { get; init; }

    [JsonPropertyName("mimeCode")]
    public string? MimeCode { get; init; }

    [JsonPropertyName("uri")]
    public string? Uri { get; init; }
}
