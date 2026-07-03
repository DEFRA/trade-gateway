#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.ReferenceData;
public partial record DefraUNVTDProfileMetadataListResponse
{
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("metadataType")]
    public string? MetadataType { get; init; }

    [JsonPropertyName("items")]
    public List<MetadataCode>? Items { get; init; }

    [JsonPropertyName("retrievedAt")]
    public DateTimeOffset? RetrievedAt { get; init; }
}
