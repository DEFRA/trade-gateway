using Api.Config;
using Microsoft.Extensions.Options;

namespace Api.Authorization;

/// <summary>
/// Fails startup fast on misconfiguration (ADR-0005): every alias granted permissions must
/// have a corresponding principal (<c>sub</c>) binding, and no principal may have an empty
/// permissions list.
/// </summary>
public class AuthorizationConfigValidator : IValidateOptions<AuthorizationConfig>
{
    public ValidateOptionsResult Validate(string? name, AuthorizationConfig options)
    {
        var failures = new List<string>();

        foreach (var (alias, grants) in options.Permissions)
        {
            if (!options.Principals.ContainsKey(alias))
                failures.Add($"Permission alias '{alias}' has no matching entry in Authorization:Principals.");

            if (grants.Count == 0)
                failures.Add($"Permission alias '{alias}' has an empty permissions list.");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
