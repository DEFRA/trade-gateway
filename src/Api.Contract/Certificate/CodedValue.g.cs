#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record CodedValue
{
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("urlId")]
    [Description("Codelist URI. EU TRACES: https://traces-codelists.ec.europa.eu/{listId}. Defra: https://codelists.tbc.defra.gov.uk/...")]
    public string? UrlId { get; init; }

    [JsonPropertyName("name")]
    [Description("Human-readable label for the coded value.")]
    public string? Name { get; init; }
}
