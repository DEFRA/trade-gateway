using System.Text.Json.Serialization;
using Api.Contract;

namespace Trade.Gateway.Api.Contract.Certificate;

[MediaType("application/vnd.defra.trade.intra-list.v1+json")]
#pragma warning disable S101
public record DefraUNVTDINTRASummaryProfile
#pragma warning restore S101
{
    [JsonPropertyName("items")]
    public required DefraUNVTDINTRASummaryProfileItem[] Items { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }
}

#pragma warning disable S101
public record DefraUNVTDINTRASummaryProfileItem
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
