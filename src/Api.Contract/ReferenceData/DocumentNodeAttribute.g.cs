#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.ReferenceData;
public partial record DocumentNodeAttribute
{
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("documentLinkTypes")]
    public List<DocumentNodeAttributeValue>? DocumentLinkTypes { get; init; }
}
