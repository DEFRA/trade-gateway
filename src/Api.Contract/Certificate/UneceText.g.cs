#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record UneceText
{
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("languageId")]
    public string? LanguageId { get; init; }
}
