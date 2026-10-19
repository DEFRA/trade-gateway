using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Api.Models;

public class FindTreeNodesRequest : IValidatableObject
{
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

        if ((UpdatedBefore - UpdatedFrom) > TimeSpan.FromDays(60))
        {
            yield return new ValidationResult(
                "The gap between updatedFrom and updatedBefore must be less than 60 days.",
                [nameof(UpdatedFrom), nameof(UpdatedBefore)]
            );
        }
    }
}
