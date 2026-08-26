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

        private enum QuantityManagementReservationMode
        {
            Release,
            Cancel,
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

        public async Task<ChedQuantityManagementOutcomeType?> Release(string chedId, string mrn, string languageCode)
        {
            // Correlates our logs with the MessageId the customs port echoes on responses and faults, which is
            // what DG SANTE ask for when a call is queried. 32 chars, inside the 1-48 token limit.
            var messageId = Guid.NewGuid().ToString("N");

            logger.LogInformation(
                "Customs Release for CHED {ChedId} as message {UpstreamMessageId}",
                chedId,
                messageId
            );

            return await ExecuteCustomsCall(
                chedId,
                messageId,
                $"releasing CHED quantity for MRN {mrn}",
                () =>
                    SendChedClearanceRequest(
                        chedId,
                        mrn,
                        languageCode,
                        messageId,
                        QuantityManagementReservationMode.Release
                    )
            );
        }

        public async Task<ChedQuantityManagementOutcomeType?> DeleteReservation(
            string chedId,
            string mrn,
            string languageCode
        )
        {
            // Correlates our logs with the MessageId the customs port echoes on responses and faults, which is
            // what DG SANTE ask for when a call is queried. 32 chars, inside the 1-48 token limit.
            var messageId = Guid.NewGuid().ToString("N");

            logger.LogInformation(
                "Customs Delete Reservation for CHED {ChedId} as message {UpstreamMessageId}",
                chedId,
                messageId
            );

            return await ExecuteCustomsCall(
                chedId,
                messageId,
                $"Deleting CHED reservation for MRN {mrn}",
                () =>
                    SendChedClearanceRequest(
                        chedId,
                        mrn,
                        languageCode,
                        messageId,
                        QuantityManagementReservationMode.Cancel
                    )
            );
        }

        public async Task<ChedQuantityManagementOutcomeType?> ReservationIntervention(
            string chedId,
            string mrn,
            ChedInterventionRequestType request,
            string languageCode
        )
        {
            // Correlates our logs with the MessageId the customs port echoes on responses and faults, which is
            // what DG SANTE ask for when a call is queried. 32 chars, inside the 1-48 token limit.
            var messageId = Guid.NewGuid().ToString("N");

            logger.LogInformation(
                "Customs Reservation Intervention for CHED {ChedId} as message {UpstreamMessageId}",
                chedId,
                messageId
            );

            return await ExecuteCustomsCall(
                chedId,
                messageId,
                $"CHED Reservation Intervention for MRN {mrn}",
                async () =>
                {
                    var response = await customsChedPort.chedInterventionRequestAsync(
                        new SecurityHeaderType(),
                        _credentials.WebServiceClientId,
                        languageCode.ToIso2AlphaLanguageCodeContentType(),
                        _customsOffice,
                        new CertexHeaderType { MessageId = messageId, UniqRequesterPrefix = _customsOffice },
                        request
                    );

                    return response?.ChedInterventionResponse1;
                }
            );
        }

        private async Task<ChedQuantityManagementOutcomeType?> SendChedClearanceRequest(
            string chedId,
            string mrn,
            string languageCode,
            string messageId,
            QuantityManagementReservationMode mode
        )
        {
            var response = await customsChedPort.chedClearanceRequestAsync(
                new SecurityHeaderType(),
                _credentials.WebServiceClientId,
                languageCode.ToIso2AlphaLanguageCodeContentType(),
                _customsOffice,
                new CertexHeaderType { MessageId = messageId, UniqRequesterPrefix = _customsOffice },
                new ChedClearanceRequestType
                {
                    CompetentCustomsOffice = new CompetentCustomsOfficeType { ReferenceNumber = _customsOffice },
                    SendingDate = DateTime.UtcNow,
                    CustomsDocumentReference = mrn,
                    ChedCertificateId = chedId,
                    GoodsClearanceInformation =
                        mode == QuantityManagementReservationMode.Release
                            ? GoodsClearanceInformationType.Item01
                            : GoodsClearanceInformationType.Item02,
                }
            );

            return response?.ChedClearanceResponse1;
        }

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

            return await ExecuteCustomsCall(
                chedId,
                messageId,
                $"CHED quantity request ({operation})",
                async () =>
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
                            CompetentCustomsOffice = new CompetentCustomsOfficeType
                            {
                                ReferenceNumber = _customsOffice,
                            },
                            QuantityManagementIndication = ToIndication(mode),
                            Language = languageCode,
                            CustomsDeclarationReferenceNumber = declarationReference,
                            CommodityDescriptionForChed = items,
                            PdfGenerationIndicationSpecified = false,
                            TransformationIndictionSpecified = false,
                        }
                    );

                    return response?.ProcessedChedInformationResponse1;
                }
            );
        }

        private static async Task<T?> ExecuteCustomsCall<T>(
            string chedId,
            string messageId,
            string operation,
            Func<Task<T?>> call
        )
        {
            try
            {
                return await call();
            }
            catch (FaultException<ExceptionWithUniqueInfoType> ex)
            {
                // The customs port has a single untyped fault, so an unknown CHED is
                // indistinguishable from a genuine upstream failure.
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
                throw new InvalidSoapException($"Traces SOAP bad request for {operation} {chedId}", ex);
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
