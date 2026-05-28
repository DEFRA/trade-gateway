#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Defra.TradeGateway.Api.Contract.ReferenceData;
public partial record DefraUNVTDProfileClassificationTreeNodeDetailResponse
{
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("treeId")]
    public string? TreeId { get; init; }

    [JsonPropertyName("nodePath")]
    public string? NodePath { get; init; }

    [JsonPropertyName("node")]
    public DefraUNVTDProfileClassificationTreeNodeDetailResponseNode? Node { get; init; }

    [JsonPropertyName("attributes")]
    public List<NodeAttribute>? Attributes { get; init; }

    [JsonPropertyName("classificationSections")]
    public List<ClassificationSection>? ClassificationSections { get; init; }

    [JsonPropertyName("taxons")]
    public List<Taxon>? Taxons { get; init; }

    [JsonPropertyName("resolvedProductClassification")]
    public DefraUNVTDProfileClassificationTreeNodeDetailResponseResolvedProductClassification? ResolvedProductClassification { get; init; }

    [JsonPropertyName("retrievedAt")]
    public DateTimeOffset? RetrievedAt { get; init; }
}

public partial record DefraUNVTDProfileClassificationTreeNodeDetailResponseNode
{
    [JsonPropertyName("cnCode")]
    public string? CnCode { get; init; }

    [JsonPropertyName("modelId")]
    [Description("Source model identifier for node-detail payloads that do not carry a CN code (for example lower-level ITACHS certificate models).")]
    public string? ModelId { get; init; }

    [JsonPropertyName("selectable")]
    public required bool Selectable { get; init; }

    [JsonPropertyName("nodeType")]
    public string? NodeType { get; init; }
}

public partial record DefraUNVTDProfileClassificationTreeNodeDetailResponseResolvedProductClassification
{
    [JsonPropertyName("systemId")]
    public string? SystemId { get; init; }

    [JsonPropertyName("classCode")]
    public string? ClassCode { get; init; }

    [JsonPropertyName("className")]
    public List<string>? ClassName { get; init; }
}
