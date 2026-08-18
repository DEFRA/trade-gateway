using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Api.Validation;
using AwesomeAssertions;
using Trade.Gateway.Api.Contract.Customs;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Api.Tests.Validation;

public class ChedReservationRequestValidatorTests
{
    private static readonly ChedReservationRequestValidator Validator = new();

    private static ReservationCommodityItem ValidItem =>
        new()
        {
            GoodsItemNumber = 1,
            CertificateLineNumber = 1,
            ClassCode = "101000110",
            NetWeightQuantity = 300m,
            NetWeightUnitOfMeasure = "KGM",
        };

    [Fact]
    public void EveryRequiredPropertyOnTheContractHasARuleEnforcingIt()
    {
        var required = typeof(ReservationCommodityItem)
            .GetProperties()
            .Where(property => property.GetCustomAttribute<RequiredAttribute>() is not null)
            .ToArray();

        required.Should().NotBeEmpty("a guard that finds nothing to guard is worthless");

        foreach (var property in required)
        {
            var result = Validate(WithoutValue(ValidItem, property));

            RejectedFields(result)
                .Should()
                .Contain(
                    $"Items[0].{property.Name}",
                    $"[Required] on {property.Name} would otherwise reach TracesNT and come back a 500"
                );
        }
    }

    [Fact]
    public void AcceptsACompleteItem()
    {
        var result = Validate(ValidItem);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RejectsAnEmptyItemsArray()
    {
        var request = new ChedReservationRequest { Items = [] };

        var result = Validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void RejectsAClassCodeThatIsOnlyWhitespace(string classCode)
    {
        var item = ValidItem with { ClassCode = classCode };

        var result = Validate(item);

        RejectedFields(result).Should().Contain("Items[0].ClassCode");
    }

    [Fact]
    public void RejectsAnItemWithNeitherAWeightNorAVolume()
    {
        var item = ValidItem with { NetWeightQuantity = null, NetWeightUnitOfMeasure = null };

        var result = Validate(item);

        var error = result.Errors.Should().ContainSingle().Subject;
        error.PropertyName.Should().Be("Items[0]");
        error.ErrorMessage.Should().Contain("netWeightQuantity").And.Contain("netVolumeQuantity");
    }

    [Fact]
    public void RejectsAQuantityWithNoUnitOfMeasure()
    {
        var item = ValidItem with { NetWeightUnitOfMeasure = null };

        var result = Validate(item);

        RejectedFields(result).Should().Contain("Items[0].NetWeightUnitOfMeasure");
    }

    [Theory]
    [InlineData("KILOS")]
    [InlineData("kgm")]
    public void RejectsAnUnrecognisedUnitOfMeasureRatherThanDefaultingToTonnes(string unitOfMeasure)
    {
        var item = ValidItem with { NetWeightUnitOfMeasure = unitOfMeasure };

        var result = Validate(item);

        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Contain(unitOfMeasure);
    }

    [Fact]
    public void RequiresAUnitOfMeasureOnlyForTheQuantityThatWasSupplied()
    {
        var item = ValidItem with { NetVolumeUnitOfMeasure = null };

        var result = Validate(item);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RejectsANegativeQuantity()
    {
        var item = ValidItem with { NetWeightQuantity = -1m };

        var result = Validate(item);

        RejectedFields(result).Should().Contain("Items[0].NetWeightQuantity");
    }

    [Fact]
    public void ReportsAFailureAgainstTheIndexOfTheItemItBelongsTo()
    {
        var request = new ChedReservationRequest { Items = [ValidItem, ValidItem with { ClassCode = null }] };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Items[1].ClassCode");
    }

    private static ValidationResult Validate(ReservationCommodityItem item) =>
        Validator.Validate(new ChedReservationRequest { Items = [item] });

    private static string[] RejectedFields(ValidationResult result) =>
        [.. result.Errors.Select(failure => failure.PropertyName)];

    private static ReservationCommodityItem WithoutValue(ReservationCommodityItem item, PropertyInfo property)
    {
        var clone = item with { };
        property.SetValue(clone, null);
        return clone;
    }
}
