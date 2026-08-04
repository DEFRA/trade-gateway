using System.ComponentModel.DataAnnotations;

namespace TracesNT;

/// <summary>
/// One TracesNT account's credentials. Bound as named options keyed by
/// <see cref="TracesNtCredentialKeys"/> — the customs port authenticates as a different account from
/// the CHED, EU-INTRA and reference-data ports, so credentials are per-service, not per-gateway.
/// </summary>
public record TracesNtCredentials
{
    /// <summary>WS-Security UsernameToken username.</summary>
    [Required]
    public string Username { get; init; } = "";

    /// <summary>Secret behind the WS-Security PasswordDigest. Never log this.</summary>
    [Required]
    public string AuthenticationKey { get; init; } = "";

    /// <summary>Sent as a SOAP header argument on every operation.</summary>
    [Required]
    public string WebServiceClientId { get; init; } = "";
}
