using TracesNT.WebServices;

namespace Api.Mapping;

/// <summary>
/// The one definition of a unit of measure TracesNT will accept, shared by the validator that
/// rejects anything else and the mapper that projects it onto the generated request.
/// </summary>
internal static class UnitOfMeasureCode
{
    /// <summary>
    /// Returns <c>null</c> for an unrecognised code rather than falling back to the enum's first
    /// member, which is <c>TNE</c>. Case-sensitive: these are UN/ECE Recommendation 20 codes.
    /// </summary>
    internal static UniversalUnitOfMeasureType? Parse(string? unitOfMeasure) =>
        Enum.TryParse<UniversalUnitOfMeasureType>(unitOfMeasure, ignoreCase: false, out var parsed)
        && Enum.IsDefined(parsed)
            ? parsed
            : null;
}
