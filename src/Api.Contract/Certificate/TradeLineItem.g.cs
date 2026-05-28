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
    public List<string>? ScientificName { get; init; }

    [JsonPropertyName("netWeight")]
    public UneceWeightMeasure? NetWeight { get; init; }

    [JsonPropertyName("grossWeight")]
    public UneceWeightMeasure? GrossWeight { get; init; }

    [JsonPropertyName("applicableProductClassification")]
    public List<ProductClassification>? ApplicableProductClassification { get; init; }

    [JsonPropertyName("physicalReferencedLogisticsPackage")]
    public List<LogisticsPackage>? PhysicalReferencedLogisticsPackage { get; init; }
}
