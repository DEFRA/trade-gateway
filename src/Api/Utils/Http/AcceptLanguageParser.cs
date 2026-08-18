namespace Api.Utils.Http;

internal static class AcceptLanguageParser
{
    internal static string GetPrimaryLanguageCode(string? acceptLanguage)
    {
        var languageCode = acceptLanguage?.Split(',')[0].Split(';')[0].Split('-')[0].Trim();

        return string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;
    }
}
