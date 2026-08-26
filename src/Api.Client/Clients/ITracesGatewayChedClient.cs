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

    [Put("/customs/cheds/{id}/declarations/{mrn}/reservation")]
    Task<ApiResponse<ChedDeclarationReservation>> PutChedReservation(
        string id,
        string mrn,
        [Body] ChedReservationRequest request,
        CancellationToken cancellationToken
    );

    [Put("/customs/cheds/{id}/declarations/{mrn}/reservation/release")]
    Task<HttpResponseMessage> ReleaseChedReservation(string id, string mrn, CancellationToken cancellationToken);

    [Delete("/customs/cheds/{id}/declarations/{mrn}/reservation")]
    Task<HttpResponseMessage> DeleteChedReservation(string id, string mrn, CancellationToken cancellationToken);

    [Put("/customs/cheds/{id}/declarations/{mrn}/reservation/intervene")]
    Task<HttpResponseMessage> ChedReservationIntervention(
        string id,
        string mrn,
        [Body] ChedReservationInterventionRequest request,
        CancellationToken cancellationToken
    );
}
