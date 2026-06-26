#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record LineTradeDelivery
{
    [JsonPropertyName("productUnitQuantity")]
    public LineTradeDeliveryProductUnitQuantity? ProductUnitQuantity { get; init; }
}

public partial record LineTradeDeliveryProductUnitQuantity
{
    [JsonPropertyName("content")]
    public required decimal Content { get; init; }

    [JsonPropertyName("unitCode")]
    public string? UnitCode { get; init; }
}
