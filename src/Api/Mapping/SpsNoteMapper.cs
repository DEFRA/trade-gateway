using Trade.Gateway.Api.Contract;
using TracesNT.WebServices;

namespace Api.Mapping;

internal static class SpsNoteMapper
{
    internal static IncludedNote Map(SPSNoteType source) => new()
    {
        Subject = source.SubjectCode?.Value,
        Content = source.Content?.Select(c => c.Value).ToList(),
        ContentCode = source.ContentCode is { Length: > 0 }
            ? source.ContentCode.Select(c => new UneceCode { ListId = c.listID, Value = c.Value }).ToList()
            : null
    };

    
}
