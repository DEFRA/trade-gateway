namespace TracesNT.Exceptions;

/// <summary>
/// A customs quantity-management operation returned its one and only fault,
/// <c>ExceptionWithUniqueInfoType</c>. The upstream <c>errorMessage</c> is deliberately carried on
/// this exception for logging and never surfaced in a response body (ADR-0002 §4).
/// </summary>
/// <remarks>
/// The customs port does not distinguish "no such CHED" from any other failure, so this maps to 502 rather
/// than 404 — 502 says "we do not know", 404 would assert "it does not exist".
/// </remarks>
public class CustomsFaultException(string message, string? messageId, string? upstreamError, Exception inner)
    : Exception(message, inner)
{
    /// <summary>The upstream <c>MessageId</c> echoed back on the fault, for DG SANTE correlation.</summary>
    public string? MessageId { get; } = messageId;

    /// <summary>The raw upstream error text. Log only — never place in a response.</summary>
    public string? UpstreamError { get; } = upstreamError;
}
