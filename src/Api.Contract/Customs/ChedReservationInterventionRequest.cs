using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

public record ChedReservationInterventionRequest
{
    [JsonPropertyName("taricDocument")]
    public required string TaricDocument { get; init; }

    [JsonPropertyName("consignmentItems")]
    public required CustomsConsignmentItem[] ConsignmentItems { get; init; }
}
