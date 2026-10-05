using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsCountrySubDivisionMapper
{
    internal static TradeCountrySubDivision? Map(SPSCountrySubDivisionType? source)
    {
        if (source?.FunctionTypeCode is null)
            return null;

        return new TradeCountrySubDivision
        {
            Identifier = source.ID?.Value,
            UrlId = source.ID?.schemeID.ToCodelistUri(),
            FunctionTypeCode = new TradeCountrySubDivisionFunctionTypeCode
            {
                Content = source.FunctionTypeCode.Value.XmlEnumCode(),
            },
            ActivityAuthorizedParty = source
                .ActivityAuthorizedSPSParty?.Select(SpsPartyMapper.Map)
                .OfType<TradeParty>()
                .ToList()
                .NullIfEmpty(),
        };
    }

    internal static List<TradeCountrySubDivision>? MapList(SPSCountrySubDivisionType[]? source) =>
        source?.Select(Map).OfType<TradeCountrySubDivision>().ToList().NullIfEmpty();
}
