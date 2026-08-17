#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record TradeProduct
{
    [JsonPropertyName("scientificName")]
    public string? ScientificName { get; init; }

    [JsonPropertyName("typeCode")]
    [Description("BSP/D23B canonical slot (`unece:typeCode`, `xsd:string`) for the trade product's type/form code. Drawn from the codelist named by the sibling `urlId`.")]
    public string? TypeCode { get; init; }

    [JsonPropertyName("urlId")]
    [Description("URL to the codelist this trade product's typeCode is drawn from.")]
    public string? UrlId { get; init; }

    [JsonPropertyName("designatedProductClassification")]
    public List<ApplicableClassification>? DesignatedProductClassification { get; init; }

    [JsonPropertyName("originCountry")]
    [Description("Per-product country of origin. Optional at core level; populated by profiles whose journeys carry origin per trade line rather than per consignment (TRACES CHED-PP's `OriginSPSCountry` is the round-trip target). Region of origin sits on `subordinateTradeCountrySubDivision` inside the `TradeCountry` shape and is carried with the country wherever it appears.")]
    public TradeCountry? OriginCountry { get; init; }
}
