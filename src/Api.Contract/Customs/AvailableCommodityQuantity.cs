#nullable enable
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

/// <summary>
/// Quantity still available on the CHED for a commodity — what has not yet been reserved or
/// consumed by any customs declaration.
/// </summary>
public record AvailableCommodityQuantity
{
    [JsonPropertyName("commodityCode")]
    public CommodityCode? CommodityCode { get; init; }

    [JsonPropertyName("certificateLineNumber")]
    [Description("The CHED line this quantity belongs to.")]
    public int? CertificateLineNumber { get; init; }

    [JsonPropertyName("unitOfMeasure")]
    [Description(
        "UN/ECE Recommendation 20 unit code, for example KGM or TNE. Absent when TracesNT did not "
            + "state one — never assume a default."
    )]
    public string? UnitOfMeasure { get; init; }

    [JsonPropertyName("quantity")]
    [Description("The available amount, expressed in unitOfMeasure.")]
    public required decimal Quantity { get; init; }
}
