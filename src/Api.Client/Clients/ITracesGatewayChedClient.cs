using Refit;
using Trade.Gateway.Api.Contract.Certificate;
using Trade.Gateway.Api.Contract.Customs;

namespace Trade.Gateway.Api.Client.Clients;

public interface ITracesGatewayChedClient
{
    [Get("/certificates/cheds")]
    Task<ApiResponse<DefraUNVTDCHEDSummaryProfile>> FindChedUpdates(
        DateTimeOffset updatedFrom,
        DateTimeOffset updatedBefore,
        int pageSize,
        int offset,
        CancellationToken cancellationToken
    );

    [Get("/certificates/cheds/{id}")]
    Task<ApiResponse<DefraUNVTDCHEDProfile>> GetChedCertification(string id, CancellationToken cancellationToken);

    [Get("/customs/cheds/{id}/quantities")]
    Task<ApiResponse<ChedQuantityLedger>> GetChedQuantities(string id, CancellationToken cancellationToken);
}