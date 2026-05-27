using TracesNT.WebServices;

namespace Api.Mapping;

internal static class SpsDateTimeMapper
{
    internal static string? Map(DateTimeType? source) =>
        source?.Item switch
        {
            DateTime dt => dt.ToString("O"),
            DateTimeTypeDateTimeString s => s.Value,
            _ => null
        };
}
