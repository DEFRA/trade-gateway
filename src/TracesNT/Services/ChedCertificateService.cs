using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TracesNT.Exceptions;
using TracesNT.Extensions;
using TracesNT.WebServices;

namespace TracesNT.Services
{
    public class ChedCertificateService(
        ChedCertificatePortClient chedCertificatePort,
        ILogger<ChedCertificateService> logger,
        IOptions<TracesNtConfig> tracesOptions
    ) : IChedCertificateService
    {
        public async Task<ChedCertificateType?> GetChedCertificate(string id, string languageCode)
        {
            try
            {
                var certificateResponse = await chedCertificatePort.getChedCertificateAsync(
                    new SecurityHeaderType(),
                    tracesOptions.Value.WebServiceClientId,
                    languageCode.ToIso2AlphaLanguageCodeContentType(),
                    [],
                    new GetChedCertificateRequestType { ID = id }
                );

                return certificateResponse?.GetChedCertificateResponse1;
            }
            catch (FaultException<ChedCertificateNotFoundExceptionType> ex)
            {
                logger.LogWarning(ex, "CHED certificate not found {Id}", id);
                return null;
            }
            catch (FaultException<EuIntraCertificatePermissionDeniedExceptionType> ex)
            {
                logger.LogWarning(ex, "Permission denied for CHED certificate {Id}", id);
                throw new PermissionDeniedException(id, ex);
            }
            catch (FaultException ex)
                when (ex.Code.IsSenderFault
                    && ex.Message.Contains("SAXException", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                throw new InvalidSoapException($"Traces SOAP bad request for Ched certificate id {id}", ex);
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException("An error occurred calling the Traces web service", ex);
            }
        }
    }
}
