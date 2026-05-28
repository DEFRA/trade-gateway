using TracesNT.WebServices;

namespace TracesNT.Services;

public interface IReferenceDataService
{
    Task<GetClassificationSectionsResponse> GetClassificationSections();
    
    Task<GetClassificationTreesResponse> GetClassificationTrees();

    Task<GetClassificationTreeResponse> GetClassificationTree(string treeId);

    Task<GetClassificationTreeNodeDetailResponse> GetClassificationTreeNodeDetail(string treeId, string? path,
        string? cnCode);
}