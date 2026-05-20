using TracesNT.WebServices;

namespace TracesNT.Services;

public interface IEuIntraCertificateService
{
    Task<EuIntraCertificateType?> GetEuIntraCertificate(string id);
}