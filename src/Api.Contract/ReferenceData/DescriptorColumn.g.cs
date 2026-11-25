#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.ReferenceData;
public partial record DescriptorColumn
{
    [JsonPropertyName("key")]
    [Description("Identifier for this descriptor column")]
    public required string Key { get; init; }

    [JsonPropertyName("label")]
    [Description("Human readable description")]
    public required string Label { get; init; }

    [JsonPropertyName("columnType")]
    public required string ColumnType { get; init; }

    [JsonPropertyName("cardinality")]
    [Description("Cardinality: OPTIONAL (0..1), MANDATORY (1..1), OPTIONAL_MULTIPLE (0..n), MANDATORY_MULTIPLE (1..n)")]
    public required string Cardinality { get; init; }
}
