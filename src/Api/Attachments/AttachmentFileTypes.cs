namespace Api.Attachments;

/// <summary>A file type CHED attachments may be, configured in <c>ChedAttachments:AllowedFileTypes</c>.</summary>
public sealed class AllowedFileType
{
    /// <summary>e.g. <c>application/pdf</c>. Also what the file is declared and served as.</summary>
    public string MimeType { get; set; } = null!;

    /// <summary>
    /// e.g. <c>.pdf</c>. Only used when TRACES gives no specific content type
    /// (none, or <c>application/octet-stream</c>), to recognise the file by its name.
    /// </summary>
    public List<string> Extensions { get; set; } = [];

    /// <summary>
    /// Other types the scanner may report for this kind of content, besides <see cref="MimeType"/>.
    /// e.g. legacy Word and Excel files are detected as the generic <c>application/x-cfb</c>.
    /// </summary>
    public List<string> DetectedTypes { get; set; } = [];
}

/// <summary>Decides whether an attachment's type is on the allowed list.</summary>
public static class AttachmentFileTypes
{
    private static readonly string[] s_unspecifiedTypes = ["application/octet-stream", "binary/octet-stream"];

    /// <summary>
    /// The allowed MIME type for a file, or null if it is not allowed. TRACES' content type decides;
    /// the file name's extension only counts when TRACES gives no specific type.
    /// </summary>
    public static string? Resolve(IReadOnlyList<AllowedFileType> allowed, string? contentType, string? fileName)
    {
        var mimeType = Normalise(contentType);

        if (mimeType is not null && !s_unspecifiedTypes.Contains(mimeType))
            return allowed.FirstOrDefault(t => Normalise(t.MimeType) == mimeType)?.MimeType;

        var extension = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        if (extension.Length < 2)
            return null;

        return allowed
            .FirstOrDefault(t => t.Extensions.Any(e => "." + e.TrimStart('.').ToLowerInvariant() == extension))
            ?.MimeType;
    }

    /// <summary>
    /// Whether the scanner's detected type fits the type the file was declared as: its MIME type or
    /// one of its <see cref="AllowedFileType.DetectedTypes"/>. False if the declared type is no longer
    /// on the list.
    /// </summary>
    public static bool MatchesContent(
        IReadOnlyList<AllowedFileType> allowed,
        string? declaredType,
        string? detectedType
    )
    {
        var declared = Normalise(declaredType);
        var detected = Normalise(detectedType);
        if (declared is null || detected is null)
            return false;

        var type = allowed.FirstOrDefault(t => Normalise(t.MimeType) == declared);
        return type is not null && (declared == detected || type.DetectedTypes.Any(d => Normalise(d) == detected));
    }

    public static IReadOnlyList<string> MimeTypes(IReadOnlyList<AllowedFileType> allowed) =>
        allowed.Select(t => t.MimeType).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>"application/pdf" without parameters such as <c>; charset=...</c>, lower case; null if blank.</summary>
    private static string? Normalise(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return null;

        var semicolon = mimeType.IndexOf(';');
        return (semicolon >= 0 ? mimeType[..semicolon] : mimeType).Trim().ToLowerInvariant();
    }
}
