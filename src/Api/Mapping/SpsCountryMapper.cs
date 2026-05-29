using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsCountryMapper
{
    internal static TradeCountry? Map(SPSCountryType? source, MappingContext context)
    {
        if (source is null)
            return null;

        return new TradeCountry { Id = source.ID?.Value, Name = source.Name.ForLanguage(context.LanguageCode) };
    }

    internal static List<TradeCountry>? MapList(SPSCountryType[]? source, MappingContext context) =>
        source?.Select(s => Map(s, context)).OfType<TradeCountry>().ToList().NullIfEmpty();
}
