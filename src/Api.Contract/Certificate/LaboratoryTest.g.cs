#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record LaboratoryTest
{
    [JsonPropertyName("reference")]
    public string? Reference { get; init; }

    [JsonPropertyName("testDescriptor")]
    [Description("Test descriptor (TRACES `LaboratoryTestDescriptor`). Samples carry the identifier either numerically (`id`) or as text (`identifier`), so both slots are permitted.")]
    public LaboratoryTestTestDescriptor? TestDescriptor { get; init; }

    [JsonPropertyName("testMotivationCode")]
    public string? TestMotivationCode { get; init; }

    [JsonPropertyName("inspectorConclusionCode")]
    public string? InspectorConclusionCode { get; init; }

    [JsonPropertyName("analysis")]
    public LaboratoryTestAnalysis? Analysis { get; init; }
}

public partial record LaboratoryTestTestDescriptor
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("categoryCode")]
    public string? CategoryCode { get; init; }
}
