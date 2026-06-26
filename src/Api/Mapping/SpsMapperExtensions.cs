using System.Xml.Serialization;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsMapperExtensions
{
#pragma warning disable S1075
    const string CodelistBaseUri = "https://traces-codelists.ec.europa.eu/{0}";
#pragma warning restore S1075

    internal static List<T>? NullIfEmpty<T>(this List<T>? list) => list is { Count: > 0 } ? list : null;

    /// <summary>
    /// Wraps a code string in a <see cref="CodedValue"/>, returning null when the value is null or empty.
    /// </summary>
    internal static CodedValue? ToCodedValue(this string? value, string uri) =>
        string.IsNullOrEmpty(value) ? null : new CodedValue { Value = value, UrlId = uri };

    /// <summary>
    /// Builds a TRACES codelist URI from a list/scheme identifier, returning null when it is null or empty.
    /// </summary>
    internal static string? ToCodelistUri(this string? listId) =>
        string.IsNullOrEmpty(listId) ? null : string.Format(CodelistBaseUri, listId);

    /// <summary>
    /// Gets the XML enum code for a given enum value, using the [XmlEnum(Name="...")] attribute if present.
    /// Enum members generated from XSD carry [XmlEnum(Name="...")] rather than their C# identifier,
    /// so we read the attribute to get the wire value (e.g. "1", "SM") instead of Enum value (e.g. "Item1").
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    internal static string XmlEnumCode<T>(this T value)
        where T : struct, Enum
    {
        var memberName = value.ToString();
        if (
            typeof(T).GetField(memberName)?.GetCustomAttributes(typeof(XmlEnumAttribute), false)
            is [XmlEnumAttribute attr, ..]
        )
            return attr.Name ?? memberName;
        return memberName;
    }
}
