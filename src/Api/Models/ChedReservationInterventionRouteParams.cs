using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Api.Models
{
    public record ChedReservationInterventionRouteParams : IValidatableObject
    {
        [FromRoute(Name = "chedId")]
        [MaxLength(50)]
        [Required]
        public string? ChedCertificateId { get; set; }

        [FromRoute(Name = "mrn")]
        [MaxLength(100)]
        [Required]
        public string? CustomsDocumentReference { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ChedCertificateId))
                yield return new ValidationResult("Ched certificate id is required.", new[] { nameof(ChedCertificateId) });

            if (string.IsNullOrWhiteSpace(CustomsDocumentReference))
                yield return new ValidationResult("MRN (customs document reference) is required.", new[] { nameof(CustomsDocumentReference) });
        }
    }
}
