using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.ReferenceData;

namespace Api.Mapping;

internal static class ClassificationTreeSummaryMapper
{
    internal static ClassificationTreeSummary Map(ClassificationTreeDescription source) =>
        new() { TreeId = source.treeID, TreeName = source.Value };
}
