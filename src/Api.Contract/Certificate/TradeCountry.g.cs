#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record TradeCountry
{
    [JsonPropertyName("code")]
    [Description("Country code; ISO 3166-1 alpha-2 expected. ISO 3166-1 is well-known so urlId is usually omitted, but a sibling urlId can be added when a non-ISO scheme is in use.")]
    public CodedValue? Code { get; init; }

    [JsonPropertyName("subordinateTradeCountrySubDivision")]
    public TradeCountrySubDivision? SubordinateTradeCountrySubDivision { get; init; }
}
