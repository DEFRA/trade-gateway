using TracesNT.WebServices;

namespace Api.Mapping;

internal static class SpsDateTimeMapper
{
    internal static DateTimeOffset? Map(DateTimeType? source) =>
        source?.Item switch
        {
            DateTime dt => Map(dt),
            DateTimeTypeDateTimeString { Value: null or "" } => null,
            DateTimeTypeDateTimeString s => DateTimeOffset.Parse(
                s.Value,
                System.Globalization.CultureInfo.InvariantCulture
            ),
            _ => null,
        };

    /// <summary>
    /// Maps a bare xs:dateTime. Most SPS elements wrap their timestamps in a
    /// <see cref="DateTimeType"/>, but some (the DOCOM follow-up records) do not.
    /// An offset-less value is read as UTC.
    /// </summary>
    internal static DateTimeOffset Map(DateTime source) =>
        new(source.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(source, DateTimeKind.Utc) : source);

    /// <summary>
    /// Maps a bare xs:dateTime paired with the <c>Specified</c> flag XmlSerializer generates for an
    /// optional element, which is how an absent timestamp is signalled.
    /// </summary>
    internal static DateTimeOffset? Map(DateTime source, bool specified) => specified ? Map(source) : null;
}
