using Trade.Gateway.Api.Contract.ReferenceData;
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
            Name = source.Value,
            LanguageId = source.languageID,
        };

    internal static List<Taxon>? MapByNodeId(
        IEnumerable<AbstractNodeAttribute>? source,
        string nodeId
    ) => source?.OfType<TaxonNodeAttribute>().FirstOrDefault(a => a.id == nodeId)?.TaxonReference?.Select(Map).ToList().NullIfEmpty();
}
