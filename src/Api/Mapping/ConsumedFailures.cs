using Trade.Gateway.Api.Contract.Customs;

namespace Api.Mapping;

/// <summary>
/// Decodes the <c>QuantityManagementOutcome</c> code that TracesNT returns when a release is not
/// executed. Only the decoded enum is published; the upstream value is never echoed (ADR-0002 §4).
/// </summary>
internal static class ConsumedFailures
{
    private static readonly Dictionary<string, ConsumedFailure> s_failures = new()
    {
        ["02"] = ConsumedFailure.RecordDoesNotExist,
        ["03"] = ConsumedFailure.ReservationConsumed,
        ["05"] = ConsumedFailure.ActiveReservationExists,
        ["06"] = ConsumedFailure.WriteOffExists,
        ["07"] = ConsumedFailure.WriteOffCannotBePerformed,
    };

    /// <summary>
    /// The decoded failure. An empty value, a success code, or anything outside the table is
    /// <see cref="ConsumedFailure.Unrecognised"/>.
    /// </summary>
    internal static ConsumedFailure Decode(string? outcome)
    {
        var code = outcome?.Trim();

        return string.IsNullOrEmpty(code)
            ? ConsumedFailure.Unrecognised
            : s_failures.GetValueOrDefault(code, ConsumedFailure.Unrecognised);
    }
}
