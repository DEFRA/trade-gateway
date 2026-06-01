using TracesNT.WebServices;

namespace Api.Mapping;

internal static class ClassificationTreeNodeTypeMapper
{
    internal static string Map(ClassificationTreeNodeType source) =>
        source switch
        {
            ClassificationTreeNodeType.nomenclature => "nomenclature",
            ClassificationTreeNodeType.label => "label",
            ClassificationTreeNodeType.taxon => "group",
            ClassificationTreeNodeType.certificate_model => "certificate",
            ClassificationTreeNodeType.no_commodity => "other",
            _ => "other",
        };
}
