using System.ComponentModel.DataAnnotations;

namespace Api.Config;

public class AuthenticationConfig
{
    [Required]
    public required IssuerAuthenticationConfig Cognito { get; init; }

    [Required]
    public required IssuerAuthenticationConfig Sts { get; init; }
}
