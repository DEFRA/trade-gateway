#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.ReferenceData;
public partial record DefraUNVTDProfileClassificationSectionListResponse
{
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("service")]
    [Description("Optional service discriminator for classification-section responses when multiple TRACES services expose similarly named operations.")]
    public string? Service { get; init; }

    [JsonPropertyName("sections")]
    public List<ClassificationSection>? Sections { get; init; }

    [JsonPropertyName("retrievedAt")]
    public DateTimeOffset? RetrievedAt { get; init; }
}
