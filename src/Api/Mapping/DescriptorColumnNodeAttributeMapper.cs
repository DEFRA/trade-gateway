using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.ReferenceData;

namespace Api.Mapping;

internal static class DescriptorColumnNodeAttributeMapper
{
    internal static DescriptorColumnAttribute Map(DescriptorColumnNodeAttribute source) =>
        new()
        {
            Key = source.id,
            
            Description = source.Description.Value,
            DescriptorColumns = source.DescriptorColumnValue.Select(Map).ToList()
        };

    internal static DescriptorColumn Map(DescriptorColumnNodeAttributeValue source) =>
        new()
        {
            Label = source.Description.Value,
            Cardinality = source.Cardinality.XmlEnumCode(),
            ColumnType = source.columnType.XmlEnumCode(),
            Key = source.id
        };
}
