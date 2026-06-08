namespace Api.Endpoints;

public static class AuthTestEndpoints
{
    public static void UseAuthTestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("auth-test", () => Results.Ok()).RequireAuthorization("ApiAccess").ExcludeFromDescription();
    }
}
