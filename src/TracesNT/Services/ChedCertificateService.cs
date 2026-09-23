using System.Net.Mail;
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
        CertificateAttachmentsPortClient certificateAttachmentsPort,
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

        public async Task<CertificateAttachmentType?> GetChedCertificateAttachment(
            string id,
            long attachmentId,
            string filename
        )
        {
            try
            {
                var certificateResponse = await certificateAttachmentsPort.getCertificateAttachmentAsync(
                    new SecurityHeaderType(),
                    _credentials.WebServiceClientId,
                    new GetCertificateAttachmentRequestType()
                    {
                        ItemElementName = ItemChoiceType1.ChedCertificateReference,
                        Item = id,
                        DocumentId = attachmentId,
                        FileName = filename,
                    }
                );

                return certificateResponse?.GetCertificateAttachmentResponse1;
            }
            catch (FaultException<CertificateAttachmentNotFoundExceptionType> ex)
            {
                logger.LogWarning(ex, "CHED certificate attachment not found {Id} - {AttachmentId}", id, attachmentId);
                return null;
            }
            catch (FaultException<PermissionDeniedExceptionType> ex)
            {
                logger.LogWarning(
                    ex,
                    "Permission denied for CHED certificate attachment {Id} - {AttachmentId}",
                    id,
                    attachmentId
                );
                throw new PermissionDeniedException(id, ex);
            }
            catch (FaultException ex)
                when (ex.Code.IsSenderFault
                    && ex.Message.Contains("SAXException", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                throw new InvalidSoapException(
                    $"Traces SOAP bad request for Ched certificate attachment id {id} - {attachmentId}",
                    ex
                );
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
