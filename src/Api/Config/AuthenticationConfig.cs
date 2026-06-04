using System.ComponentModel.DataAnnotations;

namespace Api.Config;

public class AuthenticationConfig
{
    [Required]
    public required string Authority { get; init; }

    [Required]
    public required string Scope { get; init; }
}
