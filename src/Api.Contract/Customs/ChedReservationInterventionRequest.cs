using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

public record ChedReservationInterventionRequest
{
    [JsonPropertyName("competentCustomsOffice")]
    public required CompetentCustomsOffice CompetentCustomsOffice { get; init; }

    [JsonPropertyName("sendingDate")]
    public required DateTime SendingDate { get; init; }

    [JsonPropertyName("customsDocumentReference")]
    public required string CustomsDocumentReference { get; init; }

    [JsonPropertyName("taricDocument")]
    public required string TaricDocument { get; init; }

    [JsonPropertyName("chedCertificateId")]
    public required string ChedCertificateId { get; init; }

    [JsonPropertyName("consignmentItems")]
    public required CustomsConsignmentItem[] ConsignmentItems { get; init; }

    [JsonPropertyName("interventionType")]
    public required InterventionType InterventionType { get; init; }
}
