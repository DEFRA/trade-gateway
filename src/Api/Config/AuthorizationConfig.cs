namespace Api.Config;

public class AuthorizationConfig
{
    /// <summary>Alias → JWT <c>sub</c> claim value. Environment-specific.</summary>
    public Dictionary<string, string> Principals { get; init; } = new();

    /// <summary>Alias → resource/action grants. Environment-agnostic.</summary>
    public Dictionary<string, List<PermissionGrant>> Permissions { get; init; } = new();
}

public class PermissionGrant
{
    public required List<string> Actions { get; init; }

    public required string Resource { get; init; }
}
