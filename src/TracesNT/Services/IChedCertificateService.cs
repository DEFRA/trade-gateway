using TracesNT.WebServices;

namespace TracesNT.Services;

public interface IChedCertificateService
{
    Task<ChedCertificateType?> GetChedCertificate(string id, string languageCode);
}
