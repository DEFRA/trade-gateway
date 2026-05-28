#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Defra.TradeGateway.Api.Contract.ReferenceData;
public partial record Taxon
{
    [JsonPropertyName("taxonId")]
    public required int TaxonId { get; init; }

    [JsonPropertyName("eppoCode")]
    public string? EppoCode { get; init; }

    [JsonPropertyName("faoCode")]
    public string? FaoCode { get; init; }

    [JsonPropertyName("names")]
    public required List<string> Names { get; init; }
}
