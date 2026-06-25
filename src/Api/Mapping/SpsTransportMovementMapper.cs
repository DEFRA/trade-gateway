using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsTransportMovementMapper
{
    internal static LogisticsTransportMovement? Map(SPSTransportMovementType? source)
    {
        if (source is null)
            return null;

        return new LogisticsTransportMovement
        {
            Identifier = source.ID?.Value,
            ModeCode = source.ModeCode?.Value.XmlEnumCode(),
            UsedLogisticsTransportMeans = source.UsedSPSTransportMeans?.Name?.Value is { } name
                ? new LogisticsTransportMovementUsedLogisticsTransportMeans { Name = name }
                : null,
        };
    }

    /// <summary>
    /// Maps every carriage leg in the SPS array. The contract slot is a list with one entry per
    /// leg (the SPS profile collapses BSP's pre/main/on-carriage split into this single slot); an
    /// empty or absent array maps to <c>null</c>.
    /// </summary>
    internal static List<LogisticsTransportMovement>? MapList(SPSTransportMovementType[]? source) =>
        source?.Select(Map).OfType<LogisticsTransportMovement>().ToList().NullIfEmpty();
}
