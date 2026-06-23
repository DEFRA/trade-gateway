using Api.Constants;
using Trade.Gateway.Api.Contract.ReferenceData;
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
            CertificateModel = CertificateModelReferenceMapper.Map(source.Item as SoapCertificateModelReference),
            Selectable = source.allowedForSelection,
            NodeType = ClassificationTreeNodeTypeMapper.Map(source.type),
            Label = source.Description.Value
        };
    }

    internal static DefraUNVTDProfileClassificationTreeNodeDetailResponse Map(
        ClassificationTreeNodeDetail source,
        string treeId,
        string nodeId
    )
    {
        return new DefraUNVTDProfileClassificationTreeNodeDetailResponse
        {
            Source = ReferenceDataSource.Traces,
            TreeId = treeId,
            NodeId = nodeId,
            NodePath = source.path,
            Node = Map(source),
            Attributes = source.Attribute
                ?.Where(attribute => attribute is not LegislationNodeAttribute)
                .Where(attribute => attribute is not TaxonNodeAttribute)
                .Where(attribute => attribute is not ClassificationSectionNodeAttribute)
                .Where(attribute => attribute is not SelectableDocumentLinkNodeAttribute)
                .Select(NodeAttributeMapper.Map)
                .ToList()
                .NullIfEmpty(),
            ClassificationSectionGroups = source.Attribute
                ?.OfType<ClassificationSectionNodeAttribute>()
                .Select(ClassificationSectionNodeAttributeMapper.Map)
                .ToList()
                .NullIfEmpty(),
            DocumentTypes = source.Attribute
                ?.OfType<SelectableDocumentLinkNodeAttribute>()
                .Select(DocumentNodeAttributeMapper.Map)
                .ToList()
                .NullIfEmpty(),
            LegislationAttributes = source.Attribute
                ?.OfType<LegislationNodeAttribute>()
                .Select(LegislationAttributeMapper.Map)
                .ToList()
                .NullIfEmpty(),
            Taxons = TaxonMapper.MapByNodeId(source.Attribute, AttributeNodeId.TaxonPossibleValues),
            InvasiveTaxons = TaxonMapper.MapByNodeId(source.Attribute, AttributeNodeId.InvasiveTaxonPossibleValues),
            RetrievedAt = DateTimeOffset.UtcNow,
        };
    }
}
