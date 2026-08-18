#nullable enable
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

/// <summary>
/// The quantities a customs declaration is asking to hold against a CHED. Reserving replaces
/// whatever the declaration previously held — it is a statement of its whole position, not an
/// increment.
/// </summary>
public record ChedReservationRequest
{
    [JsonPropertyName("items")]
    [Description("The consignment items to reserve. At least one is required.")]
    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required to reserve against a CHED.")]
    public required ReservationCommodityItem[] Items { get; init; }
}
