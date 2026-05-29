using System.Xml.Serialization;

namespace Api.Mapping;

internal static class SpsMapperExtensions
{
    internal static List<T>? NullIfEmpty<T>(this List<T>? list) => list is { Count: > 0 } ? list : null;

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
