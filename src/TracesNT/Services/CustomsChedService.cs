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
        private enum QuantityManagementMode
        {
            /// <summary>Reads quantities without reserving.</summary>
            ReadOnly,

            /// <summary>Reserves quantities against a declaration. Mutates customs state.</summary>
            Reserve,
        }

        private readonly TracesNtCredentials _credentials = credentials.Get(TracesNtCredentialKeys.Customs);
        private readonly string _customsOffice = config.Value.CustomsOfficeReferenceNumber;

        public Task<ProcessedChedInformationResponseType?> GetChedQuantitySummary(string chedId, string languageCode) =>
            SendProcessedChedRequest(
                chedId,
                languageCode,
                mode: QuantityManagementMode.ReadOnly,
                declarationReference: EmptyDeclarationReference(),
                items: null
            );

        public Task<ProcessedChedInformationResponseType?> ReserveChedQuantities(
            string chedId,
            string mrn,
            ConsignmentItemR6ForReservationType[] items,
            string languageCode
        ) =>
            SendProcessedChedRequest(
                chedId,
                languageCode,
                mode: QuantityManagementMode.Reserve,
                declarationReference: DeclarationReferenceFor(mrn),
                items: items
            );

        private async Task<ProcessedChedInformationResponseType?> SendProcessedChedRequest(
            string chedId,
            string languageCode,
            QuantityManagementMode mode,
            CustomsDeclarationReferenceNumber4CoiChedR51InputType declarationReference,
            ConsignmentItemR6ForReservationType[]? items
        )
        {
            // Correlates our logs with the MessageId the customs port echoes on responses and faults, which is
            // what DG SANTE ask for when a call is queried. 32 chars, inside the 1-48 token limit.
            var messageId = Guid.NewGuid().ToString("N");
            var operation = ToOperationName(mode);

            logger.LogInformation(
                "Customs {Operation} for CHED {ChedId} as message {UpstreamMessageId}",
                operation,
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
                        QuantityManagementIndication = ToIndication(mode),
                        Language = languageCode,
                        CustomsDeclarationReferenceNumber = declarationReference,
                        CommodityDescriptionForChed = items,
                        // Left unset deliberately: PdfGenerationIndication would make every call ask
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
                throw new InvalidSoapException($"Traces SOAP bad request for CHED quantity request {chedId}", ex);
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException("An error occurred calling the Traces web service", ex);
            }
        }

        /// <summary>
        /// Required even though the schema says it is optional — TracesNT rejects a request without
        /// it — but left empty, since a value would narrow the response to one declaration.
        /// </summary>
        private static CustomsDeclarationReferenceNumber4CoiChedR51InputType EmptyDeclarationReference() => new();

        /// <summary>
        /// Sets the <c>MRN</c> discriminator explicitly: <see cref="ItemChoiceType1"/> defaults to
        /// <c>LRN</c>, which would reserve against a different declaration carrying the same reference.
        /// </summary>
        private static CustomsDeclarationReferenceNumber4CoiChedR51InputType DeclarationReferenceFor(string mrn) =>
            new() { Item = mrn, ItemElementName = ItemChoiceType1.MRN };

        private static string ToIndication(QuantityManagementMode mode) =>
            mode switch
            {
                QuantityManagementMode.ReadOnly => "0",
                QuantityManagementMode.Reserve => "1",
                _ => throw new ArgumentOutOfRangeException(nameof(mode)),
            };

        private static string ToOperationName(QuantityManagementMode mode) =>
            mode switch
            {
                QuantityManagementMode.ReadOnly => "quantity summary read",
                QuantityManagementMode.Reserve => "quantity reservation",
                _ => throw new ArgumentOutOfRangeException(nameof(mode)),
            };
    }
}
