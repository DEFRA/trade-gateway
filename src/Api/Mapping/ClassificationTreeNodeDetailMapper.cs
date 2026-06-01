using Api.Constants;
using Defra.TradeGateway.Api.Contract.ReferenceData;
using SoapCertificateModelReference = TracesNT.WebServices.CertificateModelReference;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class ClassificationTreeNodeDetailMapper
{
    internal static DefraUNVTDProfileClassificationTreeNodeDetailResponseNode? Map(
        ClassificationTreeNodeDetail? source
    )
    {
        if (source is null)
            return null;

        return new DefraUNVTDProfileClassificationTreeNodeDetailResponseNode
        {
            CnCode = (source.Item as CodeType)?.Value,
            ModelId = (source.Item as SoapCertificateModelReference)?.modelId.ToString(),
            Selectable = source.allowedForSelection,
            NodeType = ClassificationTreeNodeTypeMapper.Map(source.type),
            Label = source.Description.Value
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
            Attributes = source.Attribute
                ?.Where(attribute => attribute is not LegislationNodeAttribute)
                .Select(NodeAttributeMapper.Map)
                .ToList()
                .NullIfEmpty(),
            ClassificationSections = source.Attribute
                ?.OfType<ClassificationSectionNodeAttribute>()
                .SelectMany(attribute => attribute.ClassificationSection ?? [])
                .Select(ClassificationSectionMapper.Map)
                .ToList()
                .NullIfEmpty(),
            LegislationAttributes = source.Attribute
                ?.OfType<LegislationNodeAttribute>()
                .Select(LegislationAttributeMapper.Map)
                .ToList()
                .NullIfEmpty(),
            Taxons = source.Attribute
                ?.OfType<TaxonNodeAttribute>()
                .SelectMany(attribute => attribute.TaxonReference ?? [])
                .Select(TaxonMapper.Map)
                .ToList()
                .NullIfEmpty(),
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
