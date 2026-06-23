using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsLocationMapper
{
    internal static LogisticsLocation? Map(SPSLocationType? source, MappingContext context)
    {
        if (source is null)
            return null;

        return new LogisticsLocation
        {
            Identifier = source.ID?.Value,
            UrlId = source.ID?.schemeID.ToCodelistUri(),
            Name = source.Name.ForLanguage(context.LanguageCode),
        };
    }
}
