using Trade.Gateway.Api.Contract;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class SpsAddressMapper
{
    internal static TradeAddress? Map(SPSAddressType? source)
    {
        if (source is null) return null;

        return new TradeAddress
        {
            PostcodeCode = source.PostcodeCode?.Value,
            LineOne = source.LineOne?.Value,
            LineTwo = source.LineTwo?.Value,
            CityName = source.CityName?.Value,
            CountryId = source.CountryID?.Value,
            CountryName = source.CountryName?.Value,
            CountrySubDivisionName = source.CountrySubDivisionName?.Value
        };
    }
}
