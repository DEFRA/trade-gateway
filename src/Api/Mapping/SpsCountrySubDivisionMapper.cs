using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsCountrySubDivisionMapper
{
    internal static TradeCountrySubDivision? Map(SPSCountrySubDivisionType? source, MappingContext context)
    {
        if (source?.FunctionTypeCode is null)
            return null;

        return new TradeCountrySubDivision
        {
            Identifier = source.ID?.Value,
            UrlId = source.ID?.schemeID.ToCodelistUri(),
            Name = source.Name.ForLanguage(context.LanguageCode),
            HierarchicalLevelCode = source.HierarchicalLevelCode is { } level
                ? new CodedValue { Value = level.Value, Name = level.name }
                : null,
            FunctionTypeCode = new TradeCountrySubDivisionFunctionTypeCode
            {
                Content = source.FunctionTypeCode.Value.XmlEnumCode(),
                Name = source.FunctionTypeCode.name,
            },
            ActivityAuthorizedParty = source
                .ActivityAuthorizedSPSParty?.Select(SpsPartyMapper.Map)
                .OfType<TradeParty>()
                .ToList()
                .NullIfEmpty(),
        };
    }

    internal static List<TradeCountrySubDivision>? MapList(
        SPSCountrySubDivisionType[]? source,
        MappingContext context
    ) => source?.Select(s => Map(s, context)).OfType<TradeCountrySubDivision>().ToList().NullIfEmpty();
}
