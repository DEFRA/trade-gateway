namespace Trade.Gateway.Api.Client.DelegatingHandlers;

public class TracingDelegatingHandler(Func<string> traceIdAccessor) : DelegatingHandler
{
    private  const string TraceKey = "x-cdp-request-id";
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        request.Headers.Add(TraceKey, traceIdAccessor());
        return await base.SendAsync(request, cancellationToken);
    }
}
