using Defra.TradeGateway.Api.Contract.ReferenceData;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class TaxonMapper
{
    internal static Taxon Map(TaxonReference source) =>
        new()
        {
            TaxonId = checked((int)source.taxonId),
            EppoCode = source.eppoCode,
            FaoCode = source.faoCode,
            Name = GetName(source),
            LanguageId = source.languageID,
        };

    internal static string GetName(TaxonReference source) => source.Value;
}
