using Trade.Gateway.Api.Contract.Customs;

namespace Api.Mapping;

/// <summary>
/// Decodes the <c>ReservationFailureReason</c> code that TracesNT returns alongside a negative
/// <c>ReservationResult</c>. Only the decoded enum is published; the upstream value is free text on
/// the wire, so it is never echoed (ADR-0002 §4).
/// </summary>
internal static class ReservationFailureReasons
{
    private static readonly Dictionary<string, ReservationFailureReason> s_reasons = new()
    {
        ["01"] = ReservationFailureReason.BaseForExtract,
        ["02"] = ReservationFailureReason.PcaDocumentUsed,
        ["08"] = ReservationFailureReason.CountryOfDestinationMismatch,
        ["09"] = ReservationFailureReason.LicenceHolderMismatch,
        ["03"] = ReservationFailureReason.CnCodesMismatch,
        ["04"] = ReservationFailureReason.InappropriateStatus,
        ["05"] = ReservationFailureReason.QuantitiesInsufficient,
        ["06"] = ReservationFailureReason.WriteOffExists,
        ["07"] = ReservationFailureReason.LineNumbersMismatch,
        ["10"] = ReservationFailureReason.MeasurementUnitMismatch,
        ["11"] = ReservationFailureReason.QuantitiesCannotBeValidated,
    };

    /// <summary>
    /// The decoded reason, or <c>null</c> when there is none — the element arrives empty rather than
    /// absent, so an empty value is not a reason. Anything outside the table is
    /// <see cref="ReservationFailureReason.Unrecognised"/>.
    /// </summary>
    internal static ReservationFailureReason Decode(string? reason)
    {
        var code = reason?.Trim();

        return string.IsNullOrEmpty(code)
            ? ReservationFailureReason.Unrecognised
            : s_reasons.GetValueOrDefault(code, ReservationFailureReason.Unrecognised);
    }
}
