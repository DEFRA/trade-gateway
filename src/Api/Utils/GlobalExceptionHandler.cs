using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TracesNT.Exceptions;

namespace Api.Utils;

public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger
) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var problemDetails = exception switch
        {
            PermissionDeniedException => Forbidden(),
            InvalidSoapException => InternalServerError("An internal error occurred."),
            TracesCommunicationException => BadGateway(),
            CustomsFaultException => BadGateway(),
            BadHttpRequestException => BadRequest(exception),
            _ => InternalServerError("An unexpected error occurred."),
        };

        var statusCode = problemDetails.Status!.Value;

        if (statusCode >= 500)
            logger.LogError(exception, "Unhandled exception resulted in {StatusCode}", statusCode);
        else
            logger.LogWarning(exception, "Request resulted in {StatusCode}", statusCode);

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problemDetails }
        );
    }

    private static ProblemDetails BadRequest(Exception ex) =>
        new() { Status = StatusCodes.Status400BadRequest, Detail = ex.Message };

    private static ProblemDetails Forbidden() =>
        new() { Status = StatusCodes.Status403Forbidden, Detail = "Access to this resource is not permitted." };

    private static ProblemDetails BadGateway() =>
        new()
        {
            Status = StatusCodes.Status502BadGateway,
            Detail = "An error occurred communicating with an upstream service.",
        };

    private static ProblemDetails InternalServerError(string detail) =>
        new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = detail,
        };
}
