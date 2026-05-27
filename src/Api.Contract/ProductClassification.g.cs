#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract;
public partial record ProductClassification
{
    [JsonPropertyName("systemId")]
    public string? SystemId { get; init; }

    [JsonPropertyName("systemName")]
    public string? SystemName { get; init; }

    [JsonPropertyName("classCode")]
    public string? ClassCode { get; init; }

    [JsonPropertyName("className")]
    public List<string>? ClassName { get; init; }
}
