using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Mapping;

internal static class SpsNoteMapper
{
    internal static IncludedNote Map(SPSNoteType source) =>
        new()
        {
            Subject = source.Subject?.Value,
            SubjectCode = source.SubjectCode != null ? new CodedValue { UrlId = source.SubjectCode.listID, Value = source.SubjectCode.Value } : null,
            Content = source.Content?.Select(c => c.Value).ToList(),
            ContentCode = source.ContentCode is { Length: > 0 }
                ? source.ContentCode.Select(c => new CodedValue { UrlId = c.listID, Value = c.Value }).ToList()
                : null,
        };
}
