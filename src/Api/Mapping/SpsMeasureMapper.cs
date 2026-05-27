using TracesNT.WebServices;
using Trade.Gateway.Api.Contract;

namespace Api.Mapping;

internal static class SpsMeasureMapper
{
    internal static UneceWeightMeasure? Map(MeasureType? source)
    {
        if (source is null)
            return null;

        return new UneceWeightMeasure
        {
            Content = source.Value.ToString(),
            UnitCode = source.unitCode,
            UnitCodeListVersionId = source.unitCodeListVersionID,
        };
    }
}
