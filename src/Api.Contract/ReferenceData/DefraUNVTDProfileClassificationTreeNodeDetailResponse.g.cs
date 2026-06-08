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

    [JsonPropertyName("nodeId")]
    public string? NodeId { get; init; }

    [JsonPropertyName("node")]
    public DefraUNVTDProfileClassificationTreeNodeDetailResponseNode? Node { get; init; }

    [JsonPropertyName("attributes")]
    public List<NodeAttribute>? Attributes { get; init; }

    [JsonPropertyName("documentTypes")]
    public List<DocumentNodeAttribute>? DocumentTypes { get; init; }

    [JsonPropertyName("classificationSectionGroups")]
    public List<ClassificationSectionGroup>? ClassificationSectionGroups { get; init; }

    [JsonPropertyName("legislationAttributes")]
    public List<LegislationAttribute>? LegislationAttributes { get; init; }

    [JsonPropertyName("taxons")]
    public List<Taxon>? Taxons { get; init; }

    [JsonPropertyName("invasiveTaxons")]
    public List<Taxon>? InvasiveTaxons { get; init; }

    [JsonPropertyName("retrievedAt")]
    public DateTimeOffset? RetrievedAt { get; init; }
}

public partial record DefraUNVTDProfileClassificationTreeNodeDetailResponseNode
{
    [JsonPropertyName("cnCode")]
    public string? CnCode { get; init; }

    [JsonPropertyName("certificateModel")]
    [Description("Certificate model if applicable (for example lower-level ITACHS certificate models).")]
    public CertificateModelReference? CertificateModel { get; init; }

    [JsonPropertyName("selectable")]
    public required bool Selectable { get; init; }

    [JsonPropertyName("nodeType")]
    public string? NodeType { get; init; }

    [JsonPropertyName("label")]
    public string? Label { get; init; }
}
