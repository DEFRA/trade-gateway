using Api.Validation;
using AwesomeAssertions;
using Trade.Gateway.Api.Contract.Certificate;
using Trade.Gateway.Api.Contract.Customs;

namespace Api.Tests.Validation;

public class ChedReservationInterventionRequestValidatorTests
{
    private static readonly ChedReservationInterventionRequestValidator Validator = new();

    private static ChedReservationInterventionRequest ValidRequest =>
        new()
        {
            CompetentCustomsOffice = new() { ReferenceNumber = "GB123456" },
            SendingDate = DateTime.UtcNow,
            CustomsDocumentReference = "CUSTOMS-REF-123",
            TaricDocument = "TARIC-123",
            ChedCertificateId = "CHED-123",
            InterventionType = InterventionType.PhysicalCheck,
            ConsignmentItems = [ValidItem],
        };

    private static CustomsConsignmentItem ValidItem =>
        new()
        {
            GoodsItemNumber = 1,
            ClassCode = "101000110",
            NetWeightQuantity = 300m,
            NetWeightUnitOfMeasure = UnitOfMeasureType.ASV,
            CertificateLineNumber = 1,
        };

    [Fact]
    public void AcceptsACompleteRequest()
    {
        var result = Validator.Validate(ValidRequest);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void RejectsAnEmptyCompetentCustomsOfficeReferenceNumber(string referenceNumber)
    {
        var request = ValidRequest with
        {
            CompetentCustomsOffice = ValidRequest.CompetentCustomsOffice! with { ReferenceNumber = referenceNumber },
        };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("CompetentCustomsOffice.ReferenceNumber");
    }

    [Fact]
    public void RejectsACompetentCustomsOfficeReferenceNumberLongerThan50Characters()
    {
        var request = ValidRequest with
        {
            CompetentCustomsOffice = ValidRequest.CompetentCustomsOffice! with
            {
                ReferenceNumber = new string('A', 51),
            },
        };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("CompetentCustomsOffice.ReferenceNumber");
    }

    [Fact]
    public void RejectsAnEmptySendingDate()
    {
        var request = ValidRequest with { SendingDate = default };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("SendingDate");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void RejectsAnEmptyCustomsDocumentReference(string value)
    {
        var request = ValidRequest with { CustomsDocumentReference = value };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("CustomsDocumentReference");
    }

    [Fact]
    public void RejectsACustomsDocumentReferenceLongerThan100Characters()
    {
        var request = ValidRequest with { CustomsDocumentReference = new string('A', 101) };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("CustomsDocumentReference");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void RejectsAnEmptyTaricDocument(string value)
    {
        var request = ValidRequest with { TaricDocument = value };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("TaricDocument");
    }

    [Fact]
    public void RejectsATaricDocumentLongerThan100Characters()
    {
        var request = ValidRequest with { TaricDocument = new string('A', 101) };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("TaricDocument");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void RejectsAnEmptyChedCertificateId(string value)
    {
        var request = ValidRequest with { ChedCertificateId = value };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ChedCertificateId");
    }

    [Fact]
    public void RejectsAChedCertificateIdLongerThan50Characters()
    {
        var request = ValidRequest with { ChedCertificateId = new string('A', 51) };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ChedCertificateId");
    }

    [Fact]
    public void RejectsAnUndefinedInterventionType()
    {
        var request = ValidRequest with { InterventionType = (InterventionType)999 };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("InterventionType");
    }

    [Fact]
    public void RejectsAnEmptyConsignmentItemsArray()
    {
        var request = ValidRequest with { ConsignmentItems = [] };

        var result = Validator.Validate(request);

        result.IsValid.Should().BeFalse();

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ConsignmentItems");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void RejectsAnEmptyClassCode(string classCode)
    {
        var request = ValidRequest with { ConsignmentItems = [ValidItem with { ClassCode = classCode }] };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ConsignmentItems[0].ClassCode");
    }

    [Fact]
    public void RejectsAClassCodeLongerThan20Characters()
    {
        var request = ValidRequest with { ConsignmentItems = [ValidItem with { ClassCode = new string('A', 21) }] };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ConsignmentItems[0].ClassCode");
    }

    [Fact]
    public void RejectsANegativeWeight()
    {
        var request = ValidRequest with { ConsignmentItems = [ValidItem with { NetWeightQuantity = -1m }] };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ConsignmentItems[0].NetWeightQuantity");
    }

    [Fact]
    public void RejectsAZeroWeight()
    {
        var request = ValidRequest with { ConsignmentItems = [ValidItem with { NetWeightQuantity = 0m }] };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ConsignmentItems[0].NetWeightQuantity");
    }

    [Fact]
    public void AllowsWeightToBeOmitted()
    {
        var request = ValidRequest with
        {
            ConsignmentItems = [ValidItem with { NetWeightQuantity = null, NetWeightUnitOfMeasure = null }],
        };

        var result = Validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RejectsAWeightWithNoUnitOfMeasure()
    {
        var request = ValidRequest with { ConsignmentItems = [ValidItem with { NetWeightUnitOfMeasure = null }] };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ConsignmentItems[0]");
    }

    [Fact]
    public void RejectsAUnitOfMeasureWithNoWeight()
    {
        var request = ValidRequest with { ConsignmentItems = [ValidItem with { NetWeightQuantity = null }] };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ConsignmentItems[0]");
    }

    [Fact]
    public void AllowsVolumeToBeOmitted()
    {
        var request = ValidRequest with
        {
            ConsignmentItems = [ValidItem with { NetVolumeQuantity = null, NetVolumeUnitOfMeasure = null }],
        };

        var result = Validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RejectsANegativeVolume()
    {
        var request = ValidRequest with
        {
            ConsignmentItems =
            [
                ValidItem with
                {
                    NetVolumeQuantity = -1m,
                    NetVolumeUnitOfMeasure = UnitOfMeasureType.ASV,
                },
            ],
        };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ConsignmentItems[0].NetVolumeQuantity");
    }

    [Fact]
    public void RejectsZeroVolume()
    {
        var request = ValidRequest with
        {
            ConsignmentItems =
            [
                ValidItem with
                {
                    NetVolumeQuantity = 0m,
                    NetVolumeUnitOfMeasure = UnitOfMeasureType.ASV,
                },
            ],
        };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ConsignmentItems[0].NetVolumeQuantity");
    }

    [Fact]
    public void RejectsAVolumeWithNoUnitOfMeasure()
    {
        var request = ValidRequest with
        {
            ConsignmentItems = [ValidItem with { NetVolumeQuantity = 100m, NetVolumeUnitOfMeasure = null }],
        };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ConsignmentItems[0]");
    }

    [Fact]
    public void RejectsAUnitOfMeasureWithNoVolume()
    {
        var request = ValidRequest with
        {
            ConsignmentItems =
            [
                ValidItem with
                {
                    NetVolumeQuantity = null,
                    NetVolumeUnitOfMeasure = UnitOfMeasureType.ASV,
                },
            ],
        };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ConsignmentItems[0]");
    }

    [Fact]
    public void ReportsAFailureAgainstTheIndexOfTheItemItBelongsTo()
    {
        var request = ValidRequest with { ConsignmentItems = [ValidItem, ValidItem with { ClassCode = null! }] };

        var result = Validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("ConsignmentItems[1].ClassCode");
    }
}
