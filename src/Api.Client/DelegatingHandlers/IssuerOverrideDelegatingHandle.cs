namespace Trade.Gateway.Api.Client.DelegatingHandlers;

public class IssuerOverrideDelegatingHandle(string issuer) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        request.Headers.AcceptLanguage.Clear();

        request.Headers.Add("x-issuer-override", issuer);

        return base.SendAsync(request, cancellationToken);
    }
}