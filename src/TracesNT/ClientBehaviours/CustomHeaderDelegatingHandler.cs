namespace TracesNT.ClientBehaviours;

public class CustomHeaderDelegatingHandler : DelegatingHandler
{
    private readonly string? _xApiKey;
    
    public CustomHeaderDelegatingHandler(HttpMessageHandler handler, string? xApiKey)
    {
        InnerHandler = handler;
        _xApiKey = xApiKey;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_xApiKey))
            request.Headers.Add("x-api-key", _xApiKey);
        
        return base.SendAsync(request, cancellationToken);
    }
}