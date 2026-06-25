#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record IncludedNote
{
    [JsonPropertyName("type")]
    [Description("JSON-LD @type for unece:Note when using unece-context-D23B.jsonld (type aliases to @type).")]
    [ConstValue("Note")]
    public string Type { get; init; } = "Note";

    [JsonPropertyName("identifier")]
    [Description("unece:identifier — xsd:string in unece-context-D23B.jsonld.")]
    public string? Identifier { get; init; }

    [JsonPropertyName("name")]
    [Description("unece:name — xsd:string in unece-context-D23B.jsonld.")]
    public string? Name { get; init; }

    [JsonPropertyName("subject")]
    [Description("unece:subject — xsd:string in unece-context-D23B.jsonld.")]
    public string? Subject { get; init; }

    [JsonPropertyName("creationDateTime")]
    [Description("unece:creationDateTime — xsd:string in unece-context-D23B.jsonld.")]
    public string? CreationDateTime { get; init; }

    [JsonPropertyName("noteSubjectCode")]
    [Description("Legacy subject-code slot - flat string only. Retained for the existing INTRA/CHED samples; new code should use subjectCode below.")]
    public string? NoteSubjectCode { get; init; }

    [JsonPropertyName("subjectCode")]
    [Description("Subject of the note as a coded value. urlId on the CodedValue names the codelist the code is drawn from.")]
    public CodedValue? SubjectCode { get; init; }

    [JsonPropertyName("content")]
    [Description("unece:content profile extension for deterministic typed contracts and lossless TRACES XML round-tripping: always an array of strings.")]
    public List<string>? Content { get; init; }

    [JsonPropertyName("contentCode")]
    [Description("unece:contentCode profile extension for deterministic typed contracts and lossless TRACES XML round-tripping: always an array of CodedValue objects (value + optional urlId and name).")]
    public List<CodedValue>? ContentCode { get; init; }
}
