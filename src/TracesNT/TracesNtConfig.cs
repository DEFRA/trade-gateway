using System.ComponentModel.DataAnnotations;

namespace TracesNT;

public record TracesNtConfig
{
    [Required]
    [Url]
    public string BaseUrl { get; init; } = "http://localhost:1080";

    [Required]
    public string Username { get; init; } = "";

    [Required]
    public string AuthenticationKey { get; init; } = "";

    [Required]
    public string WebServiceClientId { get; init; } = "";

    public Uri GetServiceUrl(string servicePath) => new($"{BaseUrl}/{servicePath}");
}
