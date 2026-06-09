using TracesNT.WebServices;

namespace TracesNT.Services;

public interface IEuIntraCertificateService
{
    Task<EuIntraCertificateType?> GetEuIntraCertificate(string id, string languageCode);

    Task<FindEuIntraCertificateResponse> FindEuIntraCertificates(
        DateTime after,
        DateTime before,
        int offset,
        int pageSize,
        string languageCode
    );
}
