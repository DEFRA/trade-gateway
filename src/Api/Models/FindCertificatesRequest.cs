using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Api.Models;

public class FindCertificatesRequest : IValidatableObject
{
    [FromQuery(Name = "pageSize")]
    [System.ComponentModel.Description("The number of records to return")]
    public int PageSize { get; set; } = 10;

    [FromQuery(Name = "offset")]
    [System.ComponentModel.Description("Number of records to offset")]
    [Range(1, int.MaxValue, ErrorMessage = "offset must be greater than 0")]
    public int Offset { get; set; } = 0;

    [FromQuery(Name = "updatedFrom")]
    [System.ComponentModel.Description("Start of the range")]
    [Required]
    public DateTime? UpdatedFrom { get; set; }

    [FromQuery(Name = "updatedBefore")]
    [System.ComponentModel.Description("End of the range")]
    [Required]
    public DateTime? UpdatedBefore { get; set; }

    [System.ComponentModel.Description("End of the range")]
    [Required]
    [FromHeader(Name = "Accept-Language")]
    public string? AcceptLanguage { get; set; } = "en";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UpdatedFrom.GetValueOrDefault().Kind != DateTimeKind.Utc)
        {
            yield return new ValidationResult("After date must be UTC.", [nameof(UpdatedFrom)]);
        }

        if (UpdatedBefore.GetValueOrDefault().Kind != DateTimeKind.Utc)
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
