using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsAuthenticationMapper
{
    internal static Authentication? MapByCode(
        IEnumerable<SPSAuthenticationType>? source,
        string typeCode,
        MappingContext context
    ) => Map(source?.FirstOrDefault(a => a.TypeCode?.Value.XmlEnumCode() == typeCode), context);

    internal static Authentication? Map(SPSAuthenticationType? source, MappingContext context)
    {
        if (source is null)
            return null;

        return new Authentication
        {
            TypeCode = source.TypeCode?.Value.XmlEnumCode(),
            GovernmentActionTypeCode = source.TypeCode?.name,
            ActualDateTime = SpsDateTimeMapper.Map(source.ActualDateTime),
            Provider = SpsPartyMapper.Map(source.ProviderSPSParty),
            IncludedClause = source
                .IncludedSPSClause?.Select(c => SpsClauseMapper.Map(c, context))
                .OfType<Clause>()
                .ToList()
                .NullIfEmpty(),
        };
    }
}
