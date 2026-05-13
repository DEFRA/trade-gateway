using Api.Models;

namespace Api.Endpoints;

public static class IntraEndpoints
{
    public static void UseIntraEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("intra/{id}", Get)
            .Produces<IntraCertificate>(200, "application/json");
    }

    private static async Task<IResult> Get(string id)
    {
        var intra = new IntraCertificate { Id = id };
        return Results.Ok(intra);
    }
}