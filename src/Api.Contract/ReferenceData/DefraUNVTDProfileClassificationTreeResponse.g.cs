#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.ReferenceData;
public partial record DefraUNVTDProfileClassificationTreeResponse
{
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("treeId")]
    public string? TreeId { get; init; }

    [JsonPropertyName("nodes")]
    public List<ClassificationTreeNode>? Nodes { get; init; }

    [JsonPropertyName("retrievedAt")]
    public DateTimeOffset? RetrievedAt { get; init; }
}
