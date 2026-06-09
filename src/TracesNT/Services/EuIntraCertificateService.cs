using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TracesNT.Exceptions;
using TracesNT.Extensions;
using TracesNT.WebServices;

namespace TracesNT.Services
{
    public class EuIntraCertificateService(
        EuIntraCertificatePortClient euIntraCertificatePort,
        ILogger<EuIntraCertificateService> logger,
        IOptions<TracesNtConfig> tracesOptions
    ) : IEuIntraCertificateService
    {
        public async Task<EuIntraCertificateType?> GetEuIntraCertificate(string id, string languageCode)
        {
            try
            {
                var certificateResponse = await euIntraCertificatePort.getEuIntraCertificateAsync(
                    new SecurityHeaderType(),
                    tracesOptions.Value.WebServiceClientId,
                    languageCode.ToIso2AlphaLanguageCodeContentType(),
                    [],
                    new GetEuIntraCertificateRequestType { ID = id }
                );

                return certificateResponse?.GetEuIntraCertificateResponse1;
            }
            catch (FaultException<EuIntraCertificateNotFoundExceptionType> ex)
            {
                logger.LogWarning(ex, "Certificate not found {Id}", id);
                return null;
            }
            catch (FaultException<EuIntraCertificatePermissionDeniedExceptionType> ex)
            {
                logger.LogWarning(ex, "Permission denied for certificate {Id}", id);
                throw new PermissionDeniedException(id, ex);
            }
            catch (FaultException ex)
                when (ex.Code.IsSenderFault
                    && ex.Message.Contains("SAXException", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                throw new InvalidSoapException($"Traces SOAP bad request for Intra certificate id {id}", ex);
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException("An error occurred calling the Traces web service", ex);
            }
        }

        public async Task<FindEuIntraCertificateResponse> FindEuIntraCertificates(
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
                var response = await euIntraCertificatePort.findEuIntraCertificateAsync(
                    new SecurityHeaderType(),
                    tracesOptions.Value.WebServiceClientId,
                    language,
                    [],
                    new FindEuIntraCertificateRequestType()
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
