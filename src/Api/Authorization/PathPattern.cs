namespace Api.Authorization;

/// <summary>
/// Matches a request path against a permission resource pattern (ADR-0005).
/// <list type="bullet">
/// <item><c>*</c> matches exactly one path segment (no <c>/</c>).</item>
/// <item><c>**</c> matches any suffix (zero or more segments); only meaningful as the final token.</item>
/// <item>A pattern with no wildcards is an exact literal match.</item>
/// </list>
/// Matching is case-insensitive and trailing slashes are normalised.
/// </summary>
public static class PathPattern
{
    private static readonly char[] Separator = ['/'];

    public static bool Matches(string pattern, string path)
    {
        var patternSegments = Split(pattern);
        var pathSegments = Split(path);

        for (var i = 0; i < patternSegments.Length; i++)
        {
            var segment = patternSegments[i];

            // "**" consumes all remaining path segments (including none).
            if (segment == "**")
                return true;

            // Pattern still has segments but the path has run out.
            if (i >= pathSegments.Length)
                return false;

            // "*" matches any single segment; otherwise require a literal match.
            if (segment != "*" && !string.Equals(segment, pathSegments[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Matched only if the path had no extra trailing segments.
        return patternSegments.Length == pathSegments.Length;
    }

    private static string[] Split(string value) => value.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
}
