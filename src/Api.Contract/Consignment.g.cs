#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract;
public partial record Consignment
{
    [JsonPropertyName("availabilityDueDateTime")]
    public DateTimeOffset? AvailabilityDueDateTime { get; init; }

    [JsonPropertyName("exportExitDateTime")]
    public DateTimeOffset? ExportExitDateTime { get; init; }

    [JsonPropertyName("consignorParty")]
    public TradeParty? ConsignorParty { get; init; }

    [JsonPropertyName("consigneeParty")]
    public TradeParty? ConsigneeParty { get; init; }

    [JsonPropertyName("despatchParty")]
    public TradeParty? DespatchParty { get; init; }

    [JsonPropertyName("customsTransitAgentParty")]
    public TradeParty? CustomsTransitAgentParty { get; init; }

    [JsonPropertyName("exportCountry")]
    [Description("Consignment route origin country (TRACES ExportSPSCountry).")]
    public TradeCountry? ExportCountry { get; init; }

    [JsonPropertyName("importCountry")]
    [Description("Consignment route destination/import country (TRACES ImportSPSCountry).")]
    public TradeCountry? ImportCountry { get; init; }

    [JsonPropertyName("reExportCountry")]
    [Description("Re-export countries on the consignment route (TRACES ReExportSPSCountry[]).")]
    public List<TradeCountry>? ReExportCountry { get; init; }

    [JsonPropertyName("transitCountry")]
    [Description("Transit countries on the consignment route (TRACES TransitSPSCountry[]).")]
    public List<TradeCountry>? TransitCountry { get; init; }

    [JsonPropertyName("unloadingBaseportLocation")]
    public List<LogisticsLocation>? UnloadingBaseportLocation { get; init; }

    [JsonPropertyName("includedConsignmentItem")]
    public List<ConsignmentItem>? IncludedConsignmentItem { get; init; }
}
