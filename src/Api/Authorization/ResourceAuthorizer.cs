using Api.Config;
using Microsoft.Extensions.Options;

namespace Api.Authorization;

/// <summary>
/// Evaluates fine-grained, per-principal resource/action permissions (ADR-0005).
/// Config is keyed by human-readable alias; this resolves it to a lookup keyed by the
/// canonical runtime identifier (the JWT <c>sub</c> claim) once at construction.
/// </summary>
public class ResourceAuthorizer : IResourceAuthorizer
{
    private const string Read = "READ";
    private const string Write = "WRITE";

    private readonly Dictionary<string, List<PermissionGrant>> _permissionsBySub;

    public ResourceAuthorizer(IOptions<AuthorizationConfig> options)
    {
        var config = options.Value;
        _permissionsBySub = new Dictionary<string, List<PermissionGrant>>(StringComparer.Ordinal);

        foreach (var (alias, grants) in config.Permissions)
        {
            if (config.Principals.TryGetValue(alias, out var sub))
                _permissionsBySub[sub] = grants;
        }
    }

    public bool IsAuthorized(string? sub, string path, string httpMethod)
    {
        var action = ResolveAction(httpMethod);
        if (action is null || sub is null)
            return false;

        if (!_permissionsBySub.TryGetValue(sub, out var grants))
            return false;

        return grants.Any(grant =>
            grant.Actions.Contains(action, StringComparer.OrdinalIgnoreCase)
            && PathPattern.Matches(grant.Resource, path)
        );
    }

    private static string? ResolveAction(string httpMethod) =>
        httpMethod.ToUpperInvariant() switch
        {
            "GET" => Read,
            "POST" or "PUT" => Write,
            _ => null,
        };
}
