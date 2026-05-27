using Trade.Gateway.Api.Contract;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class SpsPackageMapper
{
    internal static LogisticsPackage Map(SPSPackageType source) => new()
    {
        LevelCode = int.TryParse(source.LevelCode?.Value, out var level) ? level : null,
        TypeCode = source.TypeCode?.Value.XmlEnumCode(),
        ItemQuantity = source.ItemQuantity is { } q ? (int)q.Value : null
    };

    internal static List<LogisticsPackage>? MapList(SPSPackageType[]? source) =>
        source?.Select(Map).ToList().NullIfEmpty();
}
