#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record ApplicableClassification
{
    [JsonPropertyName("systemId")]
    public string? SystemId { get; init; }

    [JsonPropertyName("systemName")]
    public string? SystemName { get; init; }

    [JsonPropertyName("classCode")]
    public CodedValue? ClassCode { get; init; }

    [JsonPropertyName("className")]
    public List<string>? ClassName { get; init; }
}
