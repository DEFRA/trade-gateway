using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsProcessMapper
{
    internal static AppliedProcess Map(SPSProcessType source) =>
        new()
        {
            TypeCode = source.TypeCode?.Value.XmlEnumCode(),
            UrlId = source.TypeCode?.listID.ToCodelistUri(),
            OperatorParty = SpsPartyMapper.Map(source.OperatorSPSParty),
        };

    internal static List<AppliedProcess>? MapList(SPSProcessType[]? source) =>
        source?.Select(Map).ToList().NullIfEmpty();
}
