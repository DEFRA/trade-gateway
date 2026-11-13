#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record AppliedProcess
{
    [JsonPropertyName("typeCode")]
    public string? TypeCode { get; init; }

    [JsonPropertyName("urlId")]
    [Description("URL to the codelist this process typeCode is drawn from.")]
    public string? UrlId { get; init; }

    [JsonPropertyName("operatorParty")]
    public TradeParty? OperatorParty { get; init; }
}
