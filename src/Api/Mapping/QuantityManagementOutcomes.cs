namespace Api.Mapping;

/// <summary>
/// CERTEX quantity management outcome codes returned by clearance and intervention requests.
/// The schema types QuantityManagementOutcome as a bare [0-9]{2} pattern with no enumeration �
/// these values come from the CERTEX guidelines (sections 3.3.2 and 3.4), so an unrecognised code
/// is treated as an upstream problem rather than silently ignored.
/// </summary>
internal static class QuantityManagementOutcomes
{
    public const string Executed = "01";
    public const string RecordDoesNotExist = "02";
    public const string AlreadyConsumed = "03";
    public const string ExecutedWithStatusWarning = "04";
    public const string ActiveReservationExists = "05";

    public static bool IsSuccess(string? outcome) => outcome is Executed or ExecutedWithStatusWarning;

    public static string Describe(string? outcome) =>
        outcome switch
        {
            Executed => "Request successfully executed.",
            RecordDoesNotExist => "Request was not executed - no record exists for this MRN and CHED.",
            AlreadyConsumed => "Request was not executed - the reservation has been consumed.",
            ExecutedWithStatusWarning => "Request executed, but the CHED status changed during the clearance process. "
                + "The reserved quantities were still written off.",
            ActiveReservationExists => "Request was not executed - an active reservation exists for this MRN and CHED.",
            null => "TracesNT returned no quantity management outcome.",
            _ => $"Unrecognised quantity management outcome '{outcome}'.",
        };

    public static int ToStatusCode(string? outcome) =>
        outcome switch
        {
            Executed or ExecutedWithStatusWarning => StatusCodes.Status200OK,
            RecordDoesNotExist => StatusCodes.Status404NotFound,
            AlreadyConsumed or ActiveReservationExists => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status502BadGateway,
        };
}
