using TracesNT.WebServices;

namespace Api.Mapping;

internal static class TextTypeExtensions
{
    /// <summary>
    /// Returns the <c>Value</c> of the first entry whose <c>languageID</c> matches
    /// <paramref name="languageCode"/>, falling back to the first entry with a
    /// <see langword="null"/> <c>languageID</c>. Returns <see langword="null"/> if neither
    /// is found.
    /// </summary>
    internal static string? ForLanguage(this TextType[]? source, string languageCode) =>
        (
            source?.FirstOrDefault(t => t.languageID == languageCode)
            ?? source?.FirstOrDefault(t => t.languageID is null)
        )?.Value;

    /// <summary>
    /// Returns the <c>Value</c> of all entries whose <c>languageID</c> matches
    /// <paramref name="languageCode"/>. If none are found, falls back to all entries with a
    /// <see langword="null"/> <c>languageID</c>. Returns <see langword="null"/> if neither
    /// set exists.
    /// </summary>
    internal static List<string>? ForLanguageList(this TextType[]? source, string languageCode)
    {
        var byLanguage = source?.Where(t => t.languageID == languageCode).ToList();
        if (byLanguage is { Count: > 0 })
            return byLanguage.Select(t => t.Value).ToList();
        return source?.Where(t => t.languageID is null).Select(t => t.Value).ToList().NullIfEmpty();
    }

    /// <summary>
    /// Returns the <c>Value</c> of the first matching entry using a three-stage priority:
    /// <list type="number">
    ///   <item><description>First entry with a <see langword="null"/> <c>languageID</c> (language-neutral canonical code).</description></item>
    ///   <item><description>First entry whose <c>languageID</c> matches <paramref name="languageCode"/>.</description></item>
    ///   <item><description>First entry regardless of language.</description></item>
    /// </list>
    /// Use this where un-tagged entries carry a canonical code and tagged entries carry translations.
    /// </summary>
    internal static string? ForNeutralOrLanguage(this TextType[]? source, string languageCode) =>
        (
            source?.FirstOrDefault(t => t.languageID is null)
            ?? source?.FirstOrDefault(t => t.languageID == languageCode)
            ?? source?.FirstOrDefault()
        )?.Value;
}
