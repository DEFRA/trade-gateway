namespace Api.Mapping;

/// <summary>
/// Decodes the <c>ReservationFailureReason</c> that TracesNT returns alongside a negative
/// <c>ReservationResult</c>.
/// </summary>
internal static class ReservationFailureReasons
{
    private const string Unrecognised = "Unrecognised reservation failure reason";

    private static readonly Dictionary<string, string> Reasons = new()
    {
        ["01"] = "Base for extract",
        ["02"] = "PCA document used",
        ["03"] = "CN codes mismatch",
        ["04"] = "Inappropriate status",
        ["05"] = "Quantities insufficient",
        ["06"] = "Write-off for this MRN and PCA document ID exists",
        ["07"] = "Line numbers mismatch",
        ["08"] = "Country of destination mismatch",
        ["09"] = "Licence holder mismatch",
        ["10"] = "Measurement unit mismatch",
        ["11"] = "Quantities cannot be validated",
    };

    /// <summary>
    /// The decoded reason, or <c>null</c> when there is none — the element arrives empty rather than
    /// absent, so an empty value is not a reason.
    /// </summary>
    internal static ReservationFailureReason? Decode(string? reason)
    {
        var code = reason?.Trim();

        if (string.IsNullOrEmpty(code))
            return null;

        return Reasons.TryGetValue(code, out var description)
            ? new ReservationFailureReason(code, description)
            : new ReservationFailureReason(null, Unrecognised);
    }
}

internal readonly record struct ReservationFailureReason(string? Code, string Description);
