#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record LogisticsSeal
{
    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("urlId")]
    [Description("URL to the scheme this seal identifier is drawn from (e.g. seal_number).")]
    public string? UrlId { get; init; }
}
