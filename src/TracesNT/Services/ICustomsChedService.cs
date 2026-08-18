using TracesNT.WebServices;

namespace TracesNT.Services;

public interface ICustomsChedService
{
    /// <summary>
    /// Reads the customs quantity-management summary for a CHED without reserving anything
    /// (<c>QuantityManagementIndication = "0"</c>).
    /// </summary>
    /// <remarks>
    /// Takes no MRN: both read endpoints project the same whole-CHED response, and the declaration
    /// filter is applied when mapping.
    /// </remarks>
    /// <returns>
    /// The upstream response, or <c>null</c> if TracesNT answered with no body. The customs port has a single
    /// untyped fault, so an unknown CHED cannot be told from any other failure and raises
    /// <see cref="Exceptions.CustomsFaultException"/> rather than returning <c>null</c>.
    /// </returns>
    Task<ProcessedChedInformationResponseType?> GetChedQuantitySummary(string chedId, string languageCode);

    /// <summary>
    /// Reserves quantities against a declaration (<c>QuantityManagementIndication = "1"</c>). Mutates
    /// customs state and is not retried. States the declaration's whole position rather than adding to
    /// it, so calling twice replaces rather than accumulates.
    /// </summary>
    Task<ProcessedChedInformationResponseType?> ReserveChedQuantities(
        string chedId,
        string mrn,
        ConsignmentItemR6ForReservationType[] items,
        string languageCode
    );
}
