using FluentValidation;
using Trade.Gateway.Api.Contract.Customs;

namespace Api.Validation;

public sealed class ChedReservationInterventionRequestValidator : AbstractValidator<ChedReservationInterventionRequest>
{
    public ChedReservationInterventionRequestValidator()
    {
        RuleFor(x => x.CompetentCustomsOffice).NotNull();

        RuleFor(x => x.CompetentCustomsOffice.ReferenceNumber).NotEmpty().MaximumLength(50);

        RuleFor(x => x.SendingDate).NotEmpty();

        RuleFor(x => x.CustomsDocumentReference).NotEmpty().MaximumLength(100);

        RuleFor(x => x.TaricDocument).NotEmpty().MaximumLength(100);

        RuleFor(x => x.ChedCertificateId).NotEmpty().MaximumLength(50);

        RuleFor(x => x.InterventionType).IsInEnum();

        RuleFor(x => x.ConsignmentItems).NotEmpty().WithMessage("At least one item is required.");

        RuleForEach(x => x.ConsignmentItems)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.ClassCode).NotEmpty().MaximumLength(20);

                item.RuleFor(x => x.NetWeightQuantity).GreaterThan(0).When(x => x.NetWeightQuantity.HasValue);

                item.RuleFor(x => x.NetWeightUnitOfMeasure).IsInEnum().When(x => x.NetWeightUnitOfMeasure.HasValue);

                item.RuleFor(x => x.NetVolumeQuantity).GreaterThan(0).When(x => x.NetVolumeQuantity.HasValue);

                item.RuleFor(x => x.NetVolumeUnitOfMeasure).IsInEnum().When(x => x.NetVolumeUnitOfMeasure.HasValue);

                item.RuleFor(x => x)
                    .Must(x => x.NetWeightQuantity.HasValue == x.NetWeightUnitOfMeasure.HasValue)
                    .WithMessage(
                        "Net weight quantity and unit of measure must either both be specified or both be omitted."
                    );

                item.RuleFor(x => x)
                    .Must(x => x.NetVolumeQuantity.HasValue == x.NetVolumeUnitOfMeasure.HasValue)
                    .WithMessage(
                        "Net volume quantity and unit of measure must either both be specified or both be omitted."
                    );
            });
    }
}
