#nullable enable
using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Certificate;

#pragma warning disable S101
public record DefraUNVTDCHEDSummaryProfileItem
#pragma warning restore S101
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("origin")]
    public required string Origin { get; init; }

    [JsonPropertyName("created")]
    public required DateTimeOffset Created { get; init; }

    [JsonPropertyName("updated")]
    public required DateTimeOffset Updated { get; init; }
}
