using Refit;
using Trade.Gateway.Api.Contract.ReferenceData;

namespace Trade.Gateway.Api.Client.Clients;

public interface IReferenceDataClient
{
    [Get("/reference-data/classifications/sections")]
    Task<ApiResponse<DefraUNVTDProfileClassificationSectionListResponse>> GetClassificationSections(
        CancellationToken cancellationToken
    );

    [Get("/reference-data/classifications/trees/{treeId}")]
    Task<ApiResponse<DefraUNVTDProfileClassificationTreeResponse>> GetClassificationTree(string treeId,
        CancellationToken cancellationToken
    );

    [Get("/reference-data/classifications/trees/{treeId}/nodes/{nodeId}")]
    Task<ApiResponse<DefraUNVTDProfileClassificationTreeNodeDetailResponse>> GetClassificationTreeNodeDetail(string treeId, string nodeId,
        CancellationToken cancellationToken
    );

    [Get("/reference-data/metadata/{metadataType}")]
    Task<ApiResponse<DefraUNVTDProfileMetadataListResponse>> GetMetadatas(string metadataType,
        CancellationToken cancellationToken
    );
}