namespace Api.Authorization;

public interface IResourceAuthorizer
{
    /// <summary>
    /// Returns true if the principal identified by <paramref name="sub"/> is permitted to
    /// perform the action implied by <paramref name="httpMethod"/> on <paramref name="path"/>.
    /// </summary>
    bool IsAuthorized(string? sub, string path, string httpMethod);
}
