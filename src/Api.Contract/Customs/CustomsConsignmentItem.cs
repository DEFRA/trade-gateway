using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

public sealed record CustomsConsignmentItem
{
    [JsonPropertyName("goodsItemNumber")]
    public required int GoodsItemNumber { get; init; }

    [JsonPropertyName("certificateLineNumber")]
    public required int CertificateLineNumber { get; init; }

    [JsonPropertyName("classCode")]
    public required string ClassCode { get; init; }

    [JsonPropertyName("netWeightQuantity")]
    public decimal? NetWeightQuantity { get; init; }

    [JsonPropertyName("netWeightUnitOfMeasure")]
    public UnitOfMeasureType? NetWeightUnitOfMeasure { get; init; }

    [JsonPropertyName("netVolumeQuantity")]
    public decimal? NetVolumeQuantity { get; init; }

    [JsonPropertyName("netVolumeUnitOfMeasure")]
    public UnitOfMeasureType? NetVolumeUnitOfMeasure { get; init; }
}
