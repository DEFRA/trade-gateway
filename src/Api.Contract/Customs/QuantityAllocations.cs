#nullable enable
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

/// <summary>
/// What has been reserved and consumed against the CHED, across all customs declarations.
/// </summary>
public record QuantityAllocations
{
    [JsonPropertyName("reserved")]
    [Description("Quantities held for a declaration but not yet written off.")]
    public required AllocatedCommodityQuantity[] Reserved { get; init; }

    [JsonPropertyName("consumed")]
    [Description("Quantities written off against a declaration when goods were cleared.")]
    public required AllocatedCommodityQuantity[] Consumed { get; init; }
}
