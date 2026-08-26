using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

public sealed record CompetentCustomsOffice
{
    [JsonPropertyName("referenceNumber")]
    public required string ReferenceNumber { get; init; }
}
