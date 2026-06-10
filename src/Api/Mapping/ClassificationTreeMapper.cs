using Api.Constants;
using Api.Extensions;
using Defra.TradeGateway.Api.Contract.ReferenceData;

using TracesNT.WebServices;
using CertificateModelReference = TracesNT.WebServices.CertificateModelReference;
using ContractClassificationTreeNode = Defra.TradeGateway.Api.Contract.ReferenceData.ClassificationTreeNode;
using SoapClassificationTreeNode = TracesNT.WebServices.ClassificationTreeNode;

namespace Api.Mapping;

internal static class ClassificationTreeMapper
{
    internal static DefraUNVTDProfileClassificationTreeResponse Map(
        SoapClassificationTreeNode[] source,
        string treeId
    )
    {
        return new DefraUNVTDProfileClassificationTreeResponse
        {
            Source = ReferenceDataSource.Traces,
            TreeId = treeId,
            Nodes = source.Select(Map).ToList(),
            RetrievedAt = DateTimeOffset.UtcNow,
        };
    }

    internal static ContractClassificationTreeNode Map(SoapClassificationTreeNode source)
    {
        return new ContractClassificationTreeNode
        {
            NodeId = source.path.ToNodeId(),
            Path = source.path,
            Label = source.Description.Value,
            NodeType = ClassificationTreeNodeTypeMapper.Map(source.type),
            Selectable = source.allowedForSelection,
            CnCode = (source.Item as CodeType)?.Value,
            Children = source.Node?.Select(Map).ToList().NullIfEmpty(),
            Certificate = CertificateModelReferenceMapper.Map(source.Item as CertificateModelReference)
        };
    }
}
