using Refit;
using Trade.Gateway.Api.Contract.Certificate;

namespace Trade.Gateway.Api.Client.Clients;

public interface ITracesGatewayDocomClient
{
    [Get("/certificates/docoms/{id}")]
    Task<ApiResponse<DefraUNVTDDOCOMProfile>> GetDocomCertification(string id, CancellationToken cancellationToken);
}
