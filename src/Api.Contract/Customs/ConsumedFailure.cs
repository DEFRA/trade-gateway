using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

/// <summary>
/// Why TracesNT did not execute a release of a CHED reservation. Decoded from the upstream
/// <c>QuantityManagementOutcome</c> code, which itself is never published.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ConsumedFailure>))]
public enum ConsumedFailure
{
    [Description("An outcome was given, but it is not one the gateway recognises.")]
    Unrecognised,

    [Description("Request was not executed - MRN/PCA Document ID record does not exist.")]
    RecordDoesNotExist,

    [Description("Request was not executed - reservation has been consumed.")]
    ReservationConsumed,

    [Description("Request was not executed - active reservation for this MRN/PCA Document ID exists.")]
    ActiveReservationExists,

    [Description("Request was not executed - write-off for this MRN/PCA Document ID exists.")]
    WriteOffExists,

    [Description("Request was not executed - write-off cannot be performed.")]
    WriteOffCannotBePerformed,
}
