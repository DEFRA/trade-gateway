using System.ComponentModel.DataAnnotations;

namespace TracesNT;

public record TracesNtConfig
{
    [Required]
    [Url]
    public string BaseUrl { get; init; } = "http://localhost:1080";

    public Uri GetServiceUrl(string servicePath) => new($"{BaseUrl}/{servicePath}");
}
