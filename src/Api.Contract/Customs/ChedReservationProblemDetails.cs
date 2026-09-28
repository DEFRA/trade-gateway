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

    // Decoded from Extensions["reason"]. System.Text.Json still claims an ignored property's JSON
    // name, so it is renamed off "reason" — otherwise a case-insensitive (web) reader swallows the
    // member here and it never reaches Extensions.
    [JsonIgnore]
    [JsonPropertyName("$computedReason")]
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
