using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsNoteMapper
{
    internal static IncludedNote Map(SPSNoteType source) =>
        new()
        {
            Subject = source.Subject?.Value,
            SubjectCode =
                source.SubjectCode != null
                    ? new CodedValue { UrlId = source.SubjectCode.listID, Value = source.SubjectCode.Value }
                    : null,
            Content = source.Content?.Select(c => c.Value).ToList(),
            ContentCode = source.ContentCode is { Length: > 0 }
                ? source.ContentCode.Select(c => new CodedValue { UrlId = c.listID, Value = c.Value }).ToList()
                : null,
        };

    /// <summary>
    /// Maps every note in the SPS array. Unlike the other list mappers this keeps an empty list
    /// rather than collapsing to null, so a note collection is always present on the contract.
    /// </summary>
    internal static List<IncludedNote> MapList(SPSNoteType[]? source) => source?.Select(Map).ToList() ?? [];
}
