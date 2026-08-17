using Refit;
using Trade.Gateway.Api.Contract.Certificate;

namespace Trade.Gateway.Api.Client.Clients;

public interface ITracesGatewayIntraClient
{
    [Get("/certificates/intras")]
    Task<ApiResponse<DefraUNVTDINTRASummaryProfile>> FindIntraUpdates(
        DateTimeOffset updatedFrom,
        DateTimeOffset updatedBefore,
        int pageSize,
        int offset,
        CancellationToken cancellationToken
    );

    [Get("/certificates/intras/{id}")]
    Task<ApiResponse<DefraUNVTDINTRAProfile>> GetIntraCertification(string id, CancellationToken cancellationToken);
}
