#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.ReferenceData;

public partial record ClassificationSection
{
    [JsonPropertyName("classCode")]
    public required string ClassCode { get; init; }

    [JsonPropertyName("chapter")]
    public string? Chapter { get; init; }

    [JsonPropertyName("lms")]
    public required bool Lms { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("active")]
    public bool? Active { get; init; }

    [JsonPropertyName("scopes")]
    public required List<string> Scopes { get; init; }

    [JsonPropertyName("operatorActivities")]
    public List<string>? OperatorActivities { get; init; }
}
