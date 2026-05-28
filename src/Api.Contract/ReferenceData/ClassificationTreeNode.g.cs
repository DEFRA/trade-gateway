#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Defra.TradeGateway.Api.Contract.ReferenceData;
public partial record ClassificationTreeNode
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("nodeType")]
    public required string NodeType { get; init; }

    [JsonPropertyName("selectable")]
    public required bool Selectable { get; init; }

    [JsonPropertyName("cnCode")]
    public string? CnCode { get; init; }

    [JsonPropertyName("children")]
    public List<ClassificationTreeNode>? Children { get; init; }
}
