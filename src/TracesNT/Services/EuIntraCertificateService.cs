using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TracesNT.Exceptions;
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
            var language = Enum.TryParse<ISO2AlphaLanguageCodeContentType>(languageCode, out var parsed)
                ? parsed
                : ISO2AlphaLanguageCodeContentType.en;

            try
            {
                var certificateResponse = await euIntraCertificatePort.getEuIntraCertificateAsync(
                    new SecurityHeaderType(),
                    tracesOptions.Value.WebServiceClientId,
                    language,
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
    }
}
