using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization;

/// <summary>
/// Marker requirement that triggers fine-grained, per-principal resource/action
/// authorisation downstream of the <c>ApiAccess</c> policy (ADR-0005).
/// </summary>
public class ResourcePermissionRequirement : IAuthorizationRequirement;
