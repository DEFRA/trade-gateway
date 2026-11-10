using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Utils;

public static class HealthEndpoints
{
    /// <summary>Tag for checks on external dependencies, which only <c>/health/all</c> runs.</summary>
    public const string Extended = "extended";

    /// <summary>
    /// <c>/health</c> is the platform's liveness check, so it leaves out the dependency checks: an AWS
    /// outage should not get healthy containers restarted. <c>/health/all</c> reports each dependency.
    /// </summary>
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = check => !check.Tags.Contains(Extended) })
            .AllowAnonymous();

        app.MapHealthChecks("/health/all", new HealthCheckOptions { ResponseWriter = WriteStatuses }).AllowAnonymous();
    }

    // Statuses only: this endpoint is anonymous, and failure details name queues, topics and accounts.
    // The health check service logs each failure with its exception.
    private static Task WriteStatuses(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(
            new
            {
                status = report.Status.ToString(),
                results = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString()),
            },
            cancellationToken: context.RequestAborted
        );
}
