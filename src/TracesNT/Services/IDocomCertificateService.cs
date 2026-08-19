using TracesNT.WebServices;

namespace TracesNT.Services;

public interface IDocomCertificateService
{
    Task<DocomCertificateType?> GetDocomCertificate(string id, string languageCode);
}
