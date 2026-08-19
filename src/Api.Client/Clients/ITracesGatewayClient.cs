using Refit;

namespace Trade.Gateway.Api.Client.Clients;

public interface ITracesGatewayClient
    : ITracesGatewayIntraClient,
        ITracesGatewayChedClient,
        ITracesGatewayDocomClient,
        IReferenceDataClient
{
    [Get("/health")]
    Task<HttpResponseMessage> HealthCheck(CancellationToken cancellationToken);
}
