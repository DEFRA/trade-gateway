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
        var (statusCode, title, detail) = exception switch
        {
            PermissionDeniedException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "Access to this resource is not permitted."
            ),
            InvalidSoapException => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An internal error occurred."
            ),
            TracesCommunicationException => (
                StatusCodes.Status502BadGateway,
                "Bad Gateway",
                "An error occurred communicating with an upstream service."
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred."
            ),
        };

        if (statusCode >= 500)
            logger.LogError(exception, "Unhandled exception resulted in {StatusCode}", statusCode);
        else
            logger.LogWarning(exception, "Request resulted in {StatusCode}", statusCode);

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails =
                {
                    Status = statusCode,
                    Title = title,
                    Detail = detail,
                },
            }
        );
    }
}
