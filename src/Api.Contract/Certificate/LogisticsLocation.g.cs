#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record LogisticsLocation
{
    [JsonPropertyName("identifier")]
    [Description("Location identifier as a bare string (e.g. CPH19876, GBDVR1, GBDVR). Matches the BSP D23B canonical shape where idType metadata is disabled. The sibling urlId names the register or codelist the identifier is drawn from (cph_number, bcp_reference, un_locode).")]
    public string? Identifier { get; init; }

    [JsonPropertyName("urlId")]
    [Description("URL identifier for the codelist / register this location's identifier is drawn from.")]
    public string? UrlId { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("typeCode")]
    [Description("unece:typeCode is xsd:string in unece-context-D23B.jsonld.")]
    public string? TypeCode { get; init; }

    [JsonPropertyName("postalAddress")]
    [Description("Optional postal address for the location. Additive — needed for port-of-entry and inspection-point records that carry both a codelist-tagged identifier and a human-readable address.")]
    public TradeAddress? PostalAddress { get; init; }
}
