#nullable enable
using System.ComponentModel;
using System.Text.Json.Serialization;
using Api.Contract;

namespace Trade.Gateway.Api.Contract.Customs;

/// <summary>
/// The whole quantity-management position of one CHED: what remains available, and what customs
/// declarations hold against it.
/// </summary>
/// <remarks>
/// Carries no CHED id and no timestamp. The caller supplied the id in the request URL, and the
/// response's <c>Date</c> header states when it was read; echoing either back would restate the
/// request rather than tell the caller anything. A caller persisting this payload as evidence must
/// record both itself — the gateway never caches, so every response is read fresh from TracesNT.
/// </remarks>
[MediaType("application/vnd.defra.trade.ched-quantities.v1+json")]
public record ChedQuantityLedger
{
    [JsonPropertyName("available")]
    [Description("Quantities not yet reserved or consumed by any declaration.")]
    public required AvailableCommodityQuantity[] Available { get; init; }

    [JsonPropertyName("allocations")]
    public QuantityAllocations? Allocations { get; init; }
}
