using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

public sealed class ChedReservationProblemDetails
{
    public string? Title { get; init; }

    public string? Detail { get; init; }

    public int? Status { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? Extensions { get; init; }

    public ReservationFailureReason? Reason
    {
        get
        {
            if (Extensions is null || !Extensions.TryGetValue("reason", out var value))
            {
                return null;
            }

            var reasonString = value switch
            {
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),

                string str => str,

                _ => null,
            };

            if (Enum.TryParse<ReservationFailureReason>(reasonString, out var reason))
            {
                return reason;
            }

            return null;
        }
    }
}
