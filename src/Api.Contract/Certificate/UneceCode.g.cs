#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Trade.Gateway.Api.Contract.Certificate;
public partial record UneceCode
{
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("listId")]
    public string? ListId { get; init; }

    [JsonPropertyName("listName")]
    public string? ListName { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("listAgencyId")]
    public string? ListAgencyId { get; init; }

    [JsonPropertyName("listAgencyName")]
    public string? ListAgencyName { get; init; }

    [JsonPropertyName("listVersionId")]
    public string? ListVersionId { get; init; }
}
