#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Defra.TradeGateway.Api.Contract.ReferenceData;
public partial record LegislationAttribute
{
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("legislation")]
    public required List<LegislationReference> Legislation { get; init; }
}
