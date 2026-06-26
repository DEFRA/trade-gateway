using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization;

/// <summary>
/// Authorisation handler that enforces fine-grained resource/action permissions for an
/// already-authenticated principal (ADR-0005). Denies (does not succeed) → 403 Forbidden.
/// </summary>
public class ResourcePermissionHandler(
    IHttpContextAccessor httpContextAccessor,
    IResourceAuthorizer authorizer)
    : AuthorizationHandler<ResourcePermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourcePermissionRequirement requirement)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null)
            return Task.CompletedTask;

        // The JWT bearer handler may surface "sub" under its mapped NameIdentifier claim type.
        var sub = context.User.FindFirst("sub")?.Value
                  ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var path = http.Request.Path.Value ?? "/";
        if (authorizer.IsAuthorized(sub, path, http.Request.Method))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
