using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsCargoMapper
{
    internal static CargoNature Map(SPSCargoType source) => new() { TypeCode = source.TypeCode?.Value.XmlEnumCode() };

    internal static List<CargoNature>? MapList(SPSCargoType[]? source) => source?.Select(Map).ToList().NullIfEmpty();
}
