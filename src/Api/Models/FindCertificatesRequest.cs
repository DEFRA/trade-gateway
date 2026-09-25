using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Api.Models;

public class FindCertificatesRequest : IValidatableObject
{
    [FromQuery(Name = "pageSize")]
    [Description("Number of summary records to return per page, between 10 and 200.")]
    [DefaultValue(10)]
    [Range(10, 200, ErrorMessage = "pageSize must be between 10 and 200")]
    public int? PageSize { get; set; } = 10;

    [FromQuery(Name = "offset")]
    [Description("Number of records to skip before this page.")]
    [DefaultValue(0)]
    [Range(0, 9990, ErrorMessage = "offset must be equal to or greater than 0")]
    public int? Offset { get; set; } = 0;

    [FromQuery(Name = "updatedFrom")]
    [Description("Inclusive start of the update window, as a UTC timestamp.")]
    [Required]
    public DateTimeOffset? UpdatedFrom { get; set; }

    [FromQuery(Name = "updatedBefore")]
    [Description("Exclusive end of the update window, as a UTC timestamp.")]
    [Required]
    public DateTimeOffset? UpdatedBefore { get; set; }

    [Description("Preferred language for returned names, as a BCP 47 language tag. Defaults to en.")]
    [DefaultValue("en")]
    [FromHeader(Name = "Accept-Language")]
    public string? AcceptLanguage { get; set; } = "en";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        PageSize ??= 10;
        Offset ??= 0;
        AcceptLanguage ??= "en";

        if (UpdatedFrom?.Offset != TimeSpan.Zero)
        {
            yield return new ValidationResult("UpdatedFrom date must be UTC.", [nameof(UpdatedFrom)]);
        }

        if (UpdatedBefore?.Offset != TimeSpan.Zero)
        {
            yield return new ValidationResult("UpdatedBefore date must be UTC.", [nameof(UpdatedBefore)]);
        }

        if (UpdatedBefore < UpdatedFrom)
        {
            yield return new ValidationResult(
                "UpdatedBefore must be greater than or equal to UpdatedFrom.",
                [nameof(UpdatedFrom)]
            );
        }
    }
}
