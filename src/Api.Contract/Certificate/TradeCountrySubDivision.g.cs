#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record TradeCountrySubDivision
{
    [JsonPropertyName("identifier")]
    public required string Identifier { get; init; }

    [JsonPropertyName("urlId")]
    [Description("URL to the codelist this region identifier is drawn from.")]
    public string? UrlId { get; init; }

    [JsonPropertyName("functionTypeCode")]
    public required TradeCountrySubDivisionFunctionTypeCode FunctionTypeCode { get; init; }
}

public partial record TradeCountrySubDivisionFunctionTypeCode
{
    [JsonPropertyName("content")]
    [ConstValue("106")]
    public string Content { get; init; } = "106";
}
