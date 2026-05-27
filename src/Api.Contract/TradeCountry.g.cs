#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract;
public partial record TradeCountry
{
    [JsonPropertyName("id")]
    [Description("Country identifier/code value (ISO 3166-1 alpha-2 style lexical token).")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    [Description("Country name.")]
    public string? Name { get; init; }
}
