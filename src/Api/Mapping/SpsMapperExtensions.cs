using System.Xml.Serialization;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class SpsMapperExtensions
{
    internal static List<T>? NullIfEmpty<T>(this List<T>? list) => list is { Count: > 0 } ? list : null;

    internal static string? ForLanguage(this TextType[]? source, string languageCode) =>
        (
            source?.FirstOrDefault(t => t.languageID == languageCode)
            ?? source?.FirstOrDefault(t => t.languageID is null)
        )?.Value;

    internal static List<string>? ForLanguageList(this TextType[]? source, string languageCode)
    {
        var byLanguage = source?.Where(t => t.languageID == languageCode).ToList();
        if (byLanguage is { Count: > 0 })
            return byLanguage.Select(t => t.Value).ToList();
        return source?.Where(t => t.languageID is null).Select(t => t.Value).ToList().NullIfEmpty();
    }

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
