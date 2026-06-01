using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsClauseMapper
{
    internal static Clause? Map(SPSClauseType? source, MappingContext context)
    {
        if (source is null)
            return null;

        return new Clause
        {
            Identifier = source.ID?.Value,
            Content = source.Content.ForNeutralOrLanguage(context.LanguageCode)
        };
    }
}
