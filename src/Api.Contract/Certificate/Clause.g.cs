#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record Clause
{
    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("urlId")]
    [Description("URL to the codelist this clause's identifier and content are drawn from. BSP D23B documentClauseType.urlId (UN01013211).")]
    public string? UrlId { get; init; }
}
