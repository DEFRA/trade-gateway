#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record LogisticsTransportEquipment
{
    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("urlId")]
    [Description("URL to the scheme this equipment identifier is drawn from (e.g. container_number).")]
    public string? UrlId { get; init; }

    [JsonPropertyName("affixedLogisticsSeal")]
    public List<LogisticsSeal>? AffixedLogisticsSeal { get; init; }
}
