using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TracesNT.Exceptions;
using TracesNT.Extensions;
using TracesNT.WebServices;

namespace TracesNT.Services
{
    public class CustomsChedService(
        CustomsCertexChedPortClient customsChedPort,
        ILogger<CustomsChedService> logger,
        IOptionsMonitor<TracesNtCredentials> credentials,
        IOptions<TracesNtConfig> config
    ) : ICustomsChedService
    {
        /// <summary>
        /// Read-only quantity management. <c>"1"</c> reserves quantities against a declaration, so
        /// this constant is the only thing keeping the read endpoints from mutating customs state.
        /// </summary>
        private const string ReadOnlyIndication = "0";

        private readonly TracesNtCredentials _credentials = credentials.Get(TracesNtCredentialKeys.Customs);
        private readonly string _customsOffice = config.Value.CustomsOfficeReferenceNumber;

        public async Task<ProcessedChedInformationResponseType?> GetChedQuantitySummary(
            string chedId,
            string languageCode
        )
        {
            // Correlates our logs with the MessageId the customs port echoes on responses and faults, which is
            // what DG SANTE ask for when a call is queried. 32 chars, inside the 1-48 token limit.
            var messageId = Guid.NewGuid().ToString("N");

            logger.LogInformation(
                "Requesting customs quantity summary for CHED {ChedId} as message {UpstreamMessageId}",
                chedId,
                messageId
            );

            try
            {
                var response = await customsChedPort.processedChedRequestAsync(
                    new SecurityHeaderType(),
                    _credentials.WebServiceClientId,
                    languageCode.ToIso2AlphaLanguageCodeContentType(),
                    _customsOffice,
                    new CertexHeaderType { MessageId = messageId, UniqRequesterPrefix = _customsOffice },
                    new ProcessedChedRequestType
                    {
                        SendingDate = DateTime.UtcNow,
                        ChedCertificateId = chedId,
                        CompetentCustomsOffice = new CompetentCustomsOfficeType { ReferenceNumber = _customsOffice },
                        QuantityManagementIndication = ReadOnlyIndication,
                        Language = languageCode,
                        // Required even though the schema says it is optional — TracesNT rejects a
                        // request without it. Sent empty: a value would narrow the response to one
                        // declaration, and it is the field a QMI=1 write reserves against.
                        CustomsDeclarationReferenceNumber = new CustomsDeclarationReferenceNumber4CoiChedR51InputType(),
                        // Left unset deliberately: PdfGenerationIndication would make every read ask
                        // TracesNT to render a PDF, and PushIndication would subscribe us to updates.
                        PdfGenerationIndicationSpecified = false,
                        TransformationIndictionSpecified = false,
                    }
                );

                return response?.ProcessedChedInformationResponse1;
            }
            catch (FaultException<ExceptionWithUniqueInfoType> ex)
            {
                // The customs port has a single untyped fault, so an unknown CHED is indistinguishable from a
                // genuine upstream failure. Both become 502 rather than guessing at a 404.
                throw new CustomsFaultException(
                    $"Customs port fault for CHED {chedId} (message {messageId})",
                    ex.Detail?.MessageId,
                    ex.Detail?.errorMessage,
                    ex
                );
            }
            catch (FaultException ex)
                when (ex.Code.IsSenderFault
                    && ex.Message.Contains("SAXException", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                throw new InvalidSoapException($"Traces SOAP bad request for CHED quantity summary {chedId}", ex);
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException("An error occurred calling the Traces web service", ex);
            }
        }
    }
}
