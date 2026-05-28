using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ServiceModel;
using TracesNT.Exceptions;
using TracesNT.WebServices;

namespace TracesNT.Services
{
    public class ReferenceDataService(ReferenceDataPortClient referenceDataPortClient, IOptions<TracesNtConfig> tracesOptions) : IReferenceDataService
    {
        public async Task<GetClassificationSectionsResponse> GetClassificationSections()
        {
            try
            {
                var getClassificationSectionsResponse = await referenceDataPortClient.getClassificationSectionsAsync(
                    new SecurityHeaderType(),
                    tracesOptions.Value.WebServiceClientId,
                    ISO2AlphaLanguageCodeContentType.EN,
                    new GetClassificationSectionsRequestType { });

                return getClassificationSectionsResponse ?? new GetClassificationSectionsResponse();
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

        public async Task<GetClassificationTreesResponse> GetClassificationTrees()
        {
            try
            {
                var getClassificationTreesResponse = await referenceDataPortClient.getClassificationTreesAsync(
                    new SecurityHeaderType(),
                    tracesOptions.Value.WebServiceClientId,
                    ISO2AlphaLanguageCodeContentType.EN,
                    new GetClassificationTreesRequestType { });

                return getClassificationTreesResponse ?? new GetClassificationTreesResponse();
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

        public async Task<GetClassificationTreeResponse> GetClassificationTree(string treeId)
        {
            try
            {
                var getClassificationTreeResponse = await referenceDataPortClient.getClassificationTreeAsync(
                    new SecurityHeaderType(),
                    tracesOptions.Value.WebServiceClientId,
                    ISO2AlphaLanguageCodeContentType.EN,
                    new GetClassificationTreeRequestType
                    {
                        TreeID = treeId
                    });

                return getClassificationTreeResponse ?? new GetClassificationTreeResponse();
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

        public async Task<GetClassificationTreeNodeDetailResponse> GetClassificationTreeNodeDetail(string treeId, string? path, string? cnCode)
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
                    Item = (path as object) ?? new CodeType
                    {
                        Value = cnCode
                    }
                };

                return await referenceDataPortClient.getClassificationTreeNodeDetailAsync(
                    new SecurityHeaderType(),
                    tracesOptions.Value.WebServiceClientId,
                    ISO2AlphaLanguageCodeContentType.EN,
                    getClassificationsTreesRequest
                );
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
