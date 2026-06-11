#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.ReferenceData;

public partial record MetadataCode
{
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("mappedValue")]
    public string? MappedValue { get; init; }

    [JsonPropertyName("active")]
    public bool? Active { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}
