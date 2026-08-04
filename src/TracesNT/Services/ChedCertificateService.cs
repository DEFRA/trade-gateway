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
        IOptionsMonitor<TracesNtCredentials> credentials
    ) : IChedCertificateService
    {
        private readonly TracesNtCredentials _credentials = credentials.Get(TracesNtCredentialKeys.Default);

        public async Task<ChedCertificateType?> GetChedCertificate(string id, string languageCode)
        {
            try
            {
                var certificateResponse = await chedCertificatePort.getChedCertificateAsync(
                    new SecurityHeaderType(),
                    _credentials.WebServiceClientId,
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
            catch (FaultException<ChedCertificatePermissionDeniedExceptionType> ex)
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

        public async Task<FindChedCertificateResponse> FindChedCertificates(
            DateTime after,
            DateTime before,
            int offset,
            int pageSize,
            string languageCode
        )
        {
            var language = Enum.TryParse<ISO2AlphaLanguageCodeContentType>(languageCode, out var parsed)
                ? parsed
                : ISO2AlphaLanguageCodeContentType.en;

            try
            {
                var response = await chedCertificatePort.findChedCertificateAsync(
                    new SecurityHeaderType(),
                    _credentials.WebServiceClientId,
                    language,
                    [],
                    new FindChedCertificateRequestType
                    {
                        offset = offset,
                        pageSize = pageSize,
                        UpdateDateTimeRange = new DateTimeRange() { From = after, To = before },
                    }
                );

                return response;
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException("An error occurred calling the Traces web service", ex);
            }
        }
    }
}
