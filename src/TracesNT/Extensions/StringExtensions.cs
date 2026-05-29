using TracesNT.WebServices;

namespace TracesNT.Extensions
{
    public static class StringExtensions
    {
        internal static ISO2AlphaLanguageCodeContentType ToIso2AlphaLanguageCodeContentType(this string languageCode)
        {
            return Enum.TryParse<ISO2AlphaLanguageCodeContentType>(languageCode, out var parsed)
                ? parsed
                : ISO2AlphaLanguageCodeContentType.en;
        }
    }
}
