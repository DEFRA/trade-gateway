using TracesNT.WebServices;

namespace TracesNT.Services;

public interface IChedCertificateService
{
    Task<ChedCertificateType?> GetChedCertificate(string id, string languageCode);

    Task<CertificateAttachmentType?> GetChedCertificateAttachment(string id, long attachmentId, string filename);

    Task<FindChedCertificateResponse> FindChedCertificates(
        DateTime after,
        DateTime before,
        int offset,
        int pageSize,
        string languageCode
    );
}
