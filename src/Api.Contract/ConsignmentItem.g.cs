#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract;
public partial record ConsignmentItem
{
    [JsonPropertyName("natureIdCargo")]
    public List<CargoNature>? NatureIdCargo { get; init; }

    [JsonPropertyName("includedTradeLineItem")]
    public List<TradeLineItem>? IncludedTradeLineItem { get; init; }
}
