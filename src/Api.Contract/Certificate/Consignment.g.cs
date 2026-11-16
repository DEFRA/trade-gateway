#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
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

    [JsonPropertyName("deliveryParty")]
    [Description("The immediate delivery party — the party to whom the consignment is delivered. Distinct from final destination (Defra pre-notification adds finalDestinationLocation for the permanent post-movements destination at profile level).")]
    public TradeParty? DeliveryParty { get; init; }

    [JsonPropertyName("carrier")]
    [Description("The carrier party (the transporter). Operational TRACES populates both an activity-type discriminator (operator_activity_type) and a section-code discriminator (classification_section_code) on partyTypeCode.")]
    public TradeParty? Carrier { get; init; }

    [JsonPropertyName("customsTransitAgentParty")]
    public TradeParty? CustomsTransitAgentParty { get; init; }

    [JsonPropertyName("originCountry")]
    [Description("The country the consignment originated from - where the goods were produced. For consignments where the country of production differs from the country of export, this slot carries the country of production and `exportCountry` carries the country of export; for the common case where they coincide, only `originCountry` need be populated.")]
    public TradeCountry? OriginCountry { get; init; }

    [JsonPropertyName("exportCountry")]
    [Description("The country the consignment was exported from (TRACES `ExportSPSCountry`). For consignments where the country of production differs from the country of export, this slot carries the country of export and `originCountry` carries the country of production.")]
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

    [JsonPropertyName("loadingBaseportLocation")]
    [Description("Port of exit / loading baseport (TRACES `LoadingBaseportSPSLocation`, `unece:loadingBaseportLocation`) — where the consignment is loaded for despatch.")]
    public LogisticsLocation? LoadingBaseportLocation { get; init; }

    [JsonPropertyName("unloadingBaseportLocation")]
    [Description("Port of entry / unloading baseport. For Defra import pre-notifications the identifier carries un_locode.")]
    public LogisticsLocation? UnloadingBaseportLocation { get; init; }

    [JsonPropertyName("examinationEvent")]
    [Description("Examination (inspection) events on the consignment (TRACES `ExaminationSPSEvent`, `unece:examinationEvent`).")]
    public List<ExaminationEvent>? ExaminationEvent { get; init; }

    [JsonPropertyName("utilizedLogisticsTransportEquipment")]
    [Description("Transport equipment carrying the consignment, with any seals affixed to it (TRACES `UtilizedSPSTransportEquipment`). Aliased to `unece:utilizedTransportEquipment` in defra-unvtd-core-v1.context.jsonld.")]
    public List<LogisticsTransportEquipment>? UtilizedLogisticsTransportEquipment { get; init; }

    [JsonPropertyName("mainCarriageLogisticsTransportMovement")]
    [Description("Transport movement(s) for the main carriage leg(s). An array of one entry per carriage leg; consumers identify a specific leg by matching its arrivalEvent.occurrenceLogisticsLocation.identifier against a location anchor (for GB imports, unloadingBaseportLocation.identifier), never positionally. The SPS profile collapses BSP's three-way pre/main/on carriage split into this slot.")]
    public List<LogisticsTransportMovement>? MainCarriageLogisticsTransportMovement { get; init; }

    [JsonPropertyName("transitTradeCountry")]
    [Description("Transit countries between the consignment's origin and destination.")]
    public List<TradeCountry>? TransitTradeCountry { get; init; }

    [JsonPropertyName("includedConsignmentItem")]
    public List<ConsignmentItem>? IncludedConsignmentItem { get; init; }
}
