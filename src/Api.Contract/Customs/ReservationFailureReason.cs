using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Trade.Gateway.Api.Contract.Customs;

/// <summary>
/// Why TracesNT refused a reservation. Decoded from the upstream <c>ReservationFailureReason</c>
/// code, which itself is never published.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReservationFailureReason>))]
public enum ReservationFailureReason
{
    [Description("A reason was given, but it is not one the gateway recognises.")]
    Unrecognised,

    [Description("Base for extract.")]
    BaseForExtract,

    [Description("PCA document used.")]
    PcaDocumentUsed,

    [Description("Country of destination mismatch.")]
    CountryOfDestinationMismatch,

    [Description("Licence holder mismatch.")]
    LicenceHolderMismatch,

    [Description("CN codes mismatch.")]
    CnCodesMismatch,

    [Description("Inappropriate status.")]
    InappropriateStatus,

    [Description("Quantities insufficient.")]
    QuantitiesInsufficient,

    [Description("A write-off for this MRN and PCA document ID already exists.")]
    WriteOffExists,

    [Description("Line numbers mismatch.")]
    LineNumbersMismatch,

    [Description("Measurement unit mismatch.")]
    MeasurementUnitMismatch,

    [Description("Quantities cannot be validated.")]
    QuantitiesCannotBeValidated,
}
