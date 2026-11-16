#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record TradeLineItem
{
    [JsonPropertyName("sequenceNumeric")]
    public int? SequenceNumeric { get; init; }

    [JsonPropertyName("description")]
    public List<string>? Description { get; init; }

    [JsonPropertyName("scientificName")]
    [Description("The species name for the commodity in Latin")]
    public string? ScientificName { get; init; }

    [JsonPropertyName("netWeight")]
    public UneceWeightMeasure? NetWeight { get; init; }

    [JsonPropertyName("grossWeight")]
    public UneceWeightMeasure? GrossWeight { get; init; }

    [JsonPropertyName("netVolume")]
    [Description("Net volume of the goods on this line, with unitCode carrying the UN/ECE Rec 20 unit (e.g. H87 piece, LTR litre). Aliased to `unece:netVolumeMeasure` in defra-unvtd-core-v1.context.jsonld.")]
    public UneceMeasure? NetVolume { get; init; }

    [JsonPropertyName("originCountry")]
    [Description("Country of origin of the goods on this line (TRACES line-level `OriginSPSCountry`, `unece:originCountry`). Line-level origin, distinct from the consignment-level `originCountry`, for consignments whose lines originate in different countries.")]
    public TradeCountry? OriginCountry { get; init; }

    [JsonPropertyName("appliedProcess")]
    [Description("Processes applied to the goods on this line (TRACES `AppliedSPSProcess`, `unece:appliedProcess`).")]
    public List<AppliedProcess>? AppliedProcess { get; init; }

    [JsonPropertyName("applicableClassification")]
    public List<ApplicableClassification>? ApplicableClassification { get; init; }

    [JsonPropertyName("physicalReferencedLogisticsPackage")]
    public List<LogisticsPackage>? PhysicalReferencedLogisticsPackage { get; init; }

    [JsonPropertyName("specifiedTradeProduct")]
    [Description("The trade product on this line. BSP-canonical structural slot; profile schemas may narrow shape and cardinality.")]
    public List<TradeProduct>? SpecifiedTradeProduct { get; init; }

    [JsonPropertyName("specifiedLineTradeDelivery")]
    [Description("Delivery aspect of this line (line-level quantities).")]
    public List<LineTradeDelivery>? SpecifiedLineTradeDelivery { get; init; }

    [JsonPropertyName("additionalInformationNote")]
    [Description("Per-line annotation notes. Each entry carries a coded subject and one or more content values; consumers narrow the permitted subject codes in their profile.")]
    public List<IncludedNote>? AdditionalInformationNote { get; init; }
}
