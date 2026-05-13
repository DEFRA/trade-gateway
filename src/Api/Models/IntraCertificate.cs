using System.Text.Json.Serialization;

namespace Api.Models;

public class IntraCertificate
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
}