using Microsoft.Extensions.Options;

using System.ServiceModel;

using TracesNT.Exceptions;
using TracesNT.Extensions;
using TracesNT.WebServices;

namespace TracesNT.Services
{
    public class ReferenceDataService(ReferenceDataPortClient referenceDataPortClient, IOptions<TracesNtConfig> tracesOptions) : IReferenceDataService
    {
        public async Task<ClassificationSectionType[]?> GetClassificationSections(string languageCode)
        {
            try
            {
                var getClassificationSectionsResponse = await referenceDataPortClient.getClassificationSectionsAsync(
                    new SecurityHeaderType(),
                    tracesOptions.Value.WebServiceClientId,
                    languageCode.ToIso2AlphaLanguageCodeContentType(),
                    new GetClassificationSectionsRequestType { });

                return getClassificationSectionsResponse?.GetClassificationSectionsResponse1;
            }
            catch (FaultException ex) when (ex.Code.IsSenderFault &&
                                            ex.Message.Contains("SAXException",
                                                StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidSoapException("Traces SOAP bad request", ex);
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException("An error occurred calling the Traces web service", ex);
            }
        }

        public async Task<ClassificationTreeNode[]?> GetClassificationTree(string treeId, string languageCode)
        {
            try
            {
                var getClassificationTreeResponse = await referenceDataPortClient.getClassificationTreeAsync(
                    new SecurityHeaderType(),
                    tracesOptions.Value.WebServiceClientId,
                    languageCode.ToIso2AlphaLanguageCodeContentType(),
                    new GetClassificationTreeRequestType
                    {
                        TreeID = treeId
                    });

                return getClassificationTreeResponse?.GetClassificationTreeResponse1;
            }
            catch (FaultException ex) when (ex.Code.IsSenderFault &&
                                            ex.Message.Contains("SAXException",
                                                StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidSoapException("Traces SOAP bad request", ex);
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException("An error occurred calling the Traces web service", ex);
            }
        }

        public async Task<ClassificationTreeNodeDetail?> GetClassificationTreeNodeDetail(string treeId, string? path, string? cnCode, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(path) && string.IsNullOrWhiteSpace(cnCode))
            {
                throw new ArgumentException($"Either {nameof(path)} or {nameof(cnCode)} is required");
            }

            try
            {
                var getClassificationsTreesRequest = new GetClassificationTreeNodeDetailRequestType
                {
                    TreeID = treeId,
                    Item = string.IsNullOrWhiteSpace(path) ? new CodeType
                    {
                        Value = cnCode
                    } : path
                };

                var response = await referenceDataPortClient.getClassificationTreeNodeDetailAsync(
                    new SecurityHeaderType(),
                    tracesOptions.Value.WebServiceClientId,
                    languageCode.ToIso2AlphaLanguageCodeContentType(),
                    getClassificationsTreesRequest
                );

                return response?.GetClassificationTreeNodeDetailResponse1.Node;
            }
            catch (FaultException ex) when (ex.Code.IsSenderFault &&
                                            ex.Message.Contains("SAXException",
                                                StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidSoapException("Traces SOAP bad request", ex);
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException("An error occurred calling the Traces web service", ex);
            }
        }

        public async Task<MetadataCodeType[]?> GetMetadatas(string metaDataType, string languageCode)
        {
            try
            {
                var response = await referenceDataPortClient.getMetadatasAsync(
                    new SecurityHeaderType(),
                    tracesOptions.Value.WebServiceClientId,
                    languageCode.ToIso2AlphaLanguageCodeContentType(),
                    new GetMetadatasRequestType
                    {
                        MetadataType = metaDataType
                    }
                );

                return response?.GetMetadatasResponse1;
            }
            catch (FaultException ex) when (ex.Code.IsSenderFault &&
                                            ex.Message.Contains("SAXException",
                                                StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidSoapException("Traces SOAP bad request", ex);
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException("An error occurred calling the Traces web service", ex);
            }
        }
    }
}
