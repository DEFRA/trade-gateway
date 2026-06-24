#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.ReferenceData;

public partial record Taxon
{
    [JsonPropertyName("taxonId")]
    public required int TaxonId { get; init; }

    [JsonPropertyName("eppoCode")]
    public string? EppoCode { get; init; }

    [JsonPropertyName("faoCode")]
    public string? FaoCode { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("languageId")]
    public string? LanguageId { get; init; }
}
