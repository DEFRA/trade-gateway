#nullable enable
using System.Text.Json.Serialization;
using Api.Contract;

namespace Trade.Gateway.Api.Contract.Certificate;


[MediaType("application/vnd.defra.trade.ched.summary.v1+json")]
#pragma warning disable S101
public record DefraUNVTDCHEDSummaryProfile
#pragma warning restore S101
{
    [JsonPropertyName("items")]
    public required DefraUNVTDCHEDSummaryProfileItem[] Items { get; init; }

    [JsonPropertyName("offset")]
    public required int Offset { get; init; }

    [JsonPropertyName("pageSize")]
    public required int PageSize { get; init; }

    [JsonPropertyName("hasMore")]
    public required bool HasMore { get; init; }
}
