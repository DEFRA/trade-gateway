using TracesNT.WebServices;

namespace Api.Mapping;

internal static class SpsDateTimeMapper
{
    internal static DateTimeOffset? Map(DateTimeType? source) =>
        source?.Item switch
        {
            DateTime { Kind: DateTimeKind.Unspecified } dt => new DateTimeOffset(
                DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            ),
            DateTime dt => new DateTimeOffset(dt),
            DateTimeTypeDateTimeString { Value: null or "" } => null,
            DateTimeTypeDateTimeString s => DateTimeOffset.Parse(
                s.Value,
                System.Globalization.CultureInfo.InvariantCulture
            ),
            _ => null,
        };
}
