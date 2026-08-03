using System.ComponentModel.DataAnnotations;

namespace TracesNT;

public record TracesNtConfig
{
    [Required]
    [Url]
    public string BaseUrl { get; init; } = "http://localhost:1080";

    /// <summary>
    /// The customs office this gateway speaks for, sent on every customs quantity-management call as
    /// the message header, the requester prefix and the body's competent office. The gateway
    /// is single-tenant, so one configured value serves all three.
    /// </summary>
    /// <remarks>
    /// Configuration is a provisional home. This may belong on the request instead: if the gateway
    /// ever fronts more than one customs office, a single deployment-wide value would attribute
    /// every reservation and clearance to the wrong office.
    /// </remarks>
    [Required]
    [RegularExpression(
        "^[A-Za-z0-9]{1,8}$",
        ErrorMessage = "CustomsOfficeReferenceNumber must be 1-8 alphanumeric characters."
    )]
    public string CustomsOfficeReferenceNumber { get; init; } = "";

    public Uri GetServiceUrl(string servicePath) => new($"{BaseUrl}/{servicePath}");
}
