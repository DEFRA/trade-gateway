using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

/// <summary>A customs write-off or amendment: the TARIC document it is raised against and the consignment items it applies to.</summary>
public record ChedReservationInterventionRequest
{
    /// <summary>TARIC document reference the intervention is raised against.</summary>
    /// <example>GB12345678901234567890</example>
    [JsonPropertyName("taricDocument")]
    public required string TaricDocument { get; init; }

    /// <summary>Consignment items the intervention applies to, each identifying a certificate line and the quantity written off.</summary>
    [JsonPropertyName("consignmentItems")]
    public required CustomsConsignmentItem[] ConsignmentItems { get; init; }
}
