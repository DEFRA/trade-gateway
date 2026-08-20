using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TracesNT.Exceptions;
using TracesNT.Extensions;
using TracesNT.WebServices;

namespace TracesNT.Services
{
    public class DocomCertificateService(
        DocomCertificateRetrievalPortClient docomCertificatePort,
        ILogger<DocomCertificateService> logger,
        IOptionsMonitor<TracesNtCredentials> credentials
    ) : IDocomCertificateService
    {
        private readonly TracesNtCredentials _credentials = credentials.Get(TracesNtCredentialKeys.Default);

        public async Task<DocomCertificateType?> GetDocomCertificate(string id, string languageCode)
        {
            try
            {
                var certificateResponse = await docomCertificatePort.getDocomCertificateAsync(
                    new SecurityHeaderType(),
                    _credentials.WebServiceClientId,
                    languageCode.ToIso2AlphaLanguageCodeContentType(),
                    [],
                    new GetDocomCertificateRequestType { ID = id }
                );

                return certificateResponse?.GetDocomCertificateResponse1;
            }
            catch (FaultException<DocomCertificateNotFoundExceptionType> ex)
            {
                logger.LogWarning(ex, "DOCOM certificate not found {Id}", id);
                return null;
            }
            catch (FaultException<DocomCertificatePermissionDeniedExceptionType> ex)
            {
                logger.LogWarning(ex, "Permission denied for DOCOM certificate {Id}", id);
                throw new PermissionDeniedException(id, ex);
            }
            catch (FaultException ex)
                when (ex.Code.IsSenderFault
                    && ex.Message.Contains("SAXException", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                throw new InvalidSoapException($"Traces SOAP bad request for Docom certificate id {id}", ex);
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException("An error occurred calling the Traces web service", ex);
            }
        }
    }
}
