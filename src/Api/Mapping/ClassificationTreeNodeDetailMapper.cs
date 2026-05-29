using Api.Constants;
using Defra.TradeGateway.Api.Contract.ReferenceData;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class ClassificationTreeNodeDetailMapper
{
    internal static DefraUNVTDProfileClassificationTreeNodeDetailResponseResolvedProductClassification? MapResolvedProductClassification(
        ClassificationTreeNodeDetail? source
    )
    {
        if (source?.Item is not CodeType codeType)
            return null;

        return new DefraUNVTDProfileClassificationTreeNodeDetailResponseResolvedProductClassification
        {
            SystemId = codeType.listID,
            ClassCode = codeType.Value,
            ClassName = !string.IsNullOrWhiteSpace(source.Description.Value)
                ? [source.Description.Value]
                : null,
        };
    }
    internal static DefraUNVTDProfileClassificationTreeNodeDetailResponseNode? Map(
        ClassificationTreeNodeDetail? source
    )
    {
        if (source is null)
            return null;

        return new DefraUNVTDProfileClassificationTreeNodeDetailResponseNode
        {
            CnCode = (source.Item as CodeType)?.Value,
            ModelId = (source.Item as CertificateModelReference)?.modelId.ToString(),
            Selectable = source.allowedForSelection,
            NodeType = ClassificationTreeNodeTypeMapper.Map(source.type),
        };
    }

    internal static DefraUNVTDProfileClassificationTreeNodeDetailResponse Map(
        ClassificationTreeNodeDetail source,
        string treeId
    )
    {
        return new DefraUNVTDProfileClassificationTreeNodeDetailResponse
        {
            Source = ReferenceDataSource.Traces,
            TreeId = treeId,
            NodePath = source.path,
            Node = Map(source),
            Attributes = source.Attribute?.Select(NodeAttributeMapper.Map).ToList().NullIfEmpty(),
            ClassificationSections = source.Attribute
                ?.OfType<ClassificationSectionNodeAttribute>()
                .SelectMany(attribute => attribute.ClassificationSection ?? [])
                .Select(ClassificationSectionMapper.Map)
                .ToList()
                .NullIfEmpty(),
            Taxons = source.Attribute
                ?.OfType<TaxonNodeAttribute>()
                .SelectMany(attribute => attribute.TaxonReference ?? [])
                .Select(TaxonMapper.Map)
                .ToList()
                .NullIfEmpty(),
            ResolvedProductClassification = MapResolvedProductClassification(source),
            RetrievedAt = DateTimeOffset.UtcNow,
        };
    }

    internal static DefraUNVTDProfileClassificationTreeNodeDetailResponse Map(
        GetClassificationTreeNodeDetailResponse source,
        string treeId
    )
    {
        var node = source.GetClassificationTreeNodeDetailResponse1?.Node;

        return node is null
            ? new DefraUNVTDProfileClassificationTreeNodeDetailResponse
            {
                Source = ReferenceDataSource.Traces,
                TreeId = treeId,
                RetrievedAt = DateTimeOffset.UtcNow,
            }
            : Map(node, treeId);
    }
}
