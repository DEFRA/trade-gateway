using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TracesNT.Exceptions;
using TracesNT.Extensions;
using TracesNT.WebServices;

namespace TracesNT.Services
{
    public class ReferenceDataService(
        ReferenceDataPortClient referenceDataPortClient,
        IOptionsMonitor<TracesNtCredentials> credentials,
        ILogger<ReferenceDataService> logger
    ) : IReferenceDataService
    {
        private readonly TracesNtCredentials _credentials = credentials.Get(TracesNtCredentialKeys.Default);

        public async Task<ClassificationSectionType[]?> GetClassificationSections(string languageCode)
        {
            try
            {
                var getClassificationSectionsResponse = await referenceDataPortClient.getClassificationSectionsAsync(
                    new SecurityHeaderType(),
                    _credentials.WebServiceClientId,
                    languageCode.ToIso2AlphaLanguageCodeContentType(),
                    new GetClassificationSectionsRequestType { }
                );

                return getClassificationSectionsResponse?.GetClassificationSectionsResponse1;
            }
            catch (FaultException ex) when (ex.Code.IsSenderFault)
            {
                throw new InvalidSoapException(
                    $"An invalid soap request was returned from getClassificationSections for languageCode '{languageCode}'",
                    ex
                );
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException(
                    $"An error occurred calling the Traces web service getClassificationSections for languageCode '{languageCode}'",
                    ex
                );
            }
        }

        public async Task<ClassificationTreeNode[]?> GetClassificationTree(string treeId, string languageCode)
        {
            try
            {
                var getClassificationTreeResponse = await referenceDataPortClient.getClassificationTreeAsync(
                    new SecurityHeaderType(),
                    _credentials.WebServiceClientId,
                    languageCode.ToIso2AlphaLanguageCodeContentType(),
                    new GetClassificationTreeRequestType { TreeID = treeId }
                );

                return getClassificationTreeResponse?.GetClassificationTreeResponse1;
            }
            catch (FaultException ex)
                when (ex.Code.IsSenderFault
                    && ex.Message.Equals("Invalid classification tree ID", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                logger.LogWarning(ex, "Classification tree not found for treeId '{TreeId}'", treeId);
                return null;
            }
            catch (FaultException ex) when (ex.Code.IsSenderFault)
            {
                throw new InvalidSoapException(
                    $"An invalid soap request was returned from getClassificationTree for treeId '{treeId}' and languageCode '{languageCode}'",
                    ex
                );
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException(
                    $"An error occurred calling the Traces web service getClassificationTree for treeId '{treeId}' and languageCode '{languageCode}'",
                    ex
                );
            }
        }

        public async Task<ClassificationTreeNodeDetail?> GetClassificationTreeNodeDetail(
            string treeId,
            string path,
            string languageCode
        )
        {
            try
            {
                var getClassificationsTreesRequest = new GetClassificationTreeNodeDetailRequestType
                {
                    TreeID = treeId,
                    Item = path,
                };

                var response = await referenceDataPortClient.getClassificationTreeNodeDetailAsync(
                    new SecurityHeaderType(),
                    _credentials.WebServiceClientId,
                    languageCode.ToIso2AlphaLanguageCodeContentType(),
                    getClassificationsTreesRequest
                );

                return response?.GetClassificationTreeNodeDetailResponse1.Node;
            }
            catch (FaultException<NodeNotFoundExceptionType> ex)
            {
                logger.LogWarning(
                    ex,
                    "Node not found for treeId '{TreeId}' and path '{Path}' for languageCode '{LanguageCode}'",
                    treeId,
                    path,
                    languageCode
                );
                return null;
            }
            catch (FaultException ex) when (ex.Code.IsSenderFault)
            {
                throw new InvalidSoapException(
                    $"Traces SOAP bad request calling getClassificationTreeNodeDetail for treeId '{treeId}' and path '{path}' for languageCode '{languageCode}'",
                    ex
                );
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException(
                    $"An error occurred calling the Traces web service getClassificationTreeNodeDetail for treeId '{treeId}' and path '{path}' for languageCode '{languageCode}'",
                    ex
                );
            }
        }

        public async Task<MetadataCodeType[]?> GetMetadatas(string metaDataType, string languageCode)
        {
            try
            {
                var response = await referenceDataPortClient.getMetadatasAsync(
                    new SecurityHeaderType(),
                    _credentials.WebServiceClientId,
                    languageCode.ToIso2AlphaLanguageCodeContentType(),
                    new GetMetadatasRequestType { MetadataType = metaDataType }
                );

                return response?.GetMetadatasResponse1;
            }
            catch (FaultException ex) when (ex.Code.IsSenderFault)
            {
                throw new InvalidSoapException(
                    $"Traces SOAP bad request calling getMetadatas for metadata type '{metaDataType}' and languageCode '{languageCode}'",
                    ex
                );
            }
            catch (Exception ex)
            {
                throw new TracesCommunicationException(
                    $"An error occurred calling the Traces web service getMetadatas for metadata type {metaDataType} and languageCode {languageCode}",
                    ex
                );
            }
        }
    }
}
