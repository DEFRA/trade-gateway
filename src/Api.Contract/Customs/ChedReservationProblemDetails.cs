using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

public sealed class ChedReservationProblemDetails
{
    public string? Title { get; init; }

    public string? Detail { get; init; }

    public int? Status { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; init; }

    public ReservationFailureReason? Reason =>
        Extensions is not null
        && Extensions.TryGetValue("reason", out var value)
        && value.ValueKind == JsonValueKind.String
        && Enum.TryParse<ReservationFailureReason>(value.GetString(), out var reason)
            ? reason
            : null;
}
