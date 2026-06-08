using TracesNT.WebServices;

namespace TracesNT.Services;

public interface IReferenceDataService
{
    Task<ClassificationSectionType[]?> GetClassificationSections(string languageCode);

    Task<ClassificationTreeNode[]?> GetClassificationTree(string treeId, string languageCode);

    Task<ClassificationTreeNodeDetail?> GetClassificationTreeNodeDetail(string treeId, string path,string languageCode);

    Task<MetadataCodeType[]?> GetMetadatas(string metaDataType, string languageCode);
}