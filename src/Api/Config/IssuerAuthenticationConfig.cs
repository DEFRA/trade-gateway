using System.ComponentModel.DataAnnotations;

namespace Api.Config;

public class IssuerAuthenticationConfig
{
    [Required]
    public required string Authority { get; init; }

    public string? Scope { get; init; }

    public string? Audience { get; init; }
}
