using Api.Mapping;
using AwesomeAssertions;
using Trade.Gateway.Api.Contract.Customs;

namespace Api.Tests.Mapping;

public class ChedReservationInterventionMapperTests
{
    private static ChedReservationInterventionRequest ValidRequest =>
        new()
        {
            CompetentCustomsOffice = new() { ReferenceNumber = "GB123456" },
            SendingDate = new DateTime(2026, 8, 26, 8, 0, 0, DateTimeKind.Utc),
            CustomsDocumentReference = "CUSTOMS-REF-123",
            TaricDocument = "TARIC-123",
            ChedCertificateId = "CHED-123",
            InterventionType = InterventionType.PhysicalCheck,
            ConsignmentItems =
            [
                new CustomsConsignmentItem
                {
                    GoodsItemNumber = 1,
                    CertificateLineNumber = 2,
                    ClassCode = "101000110",
                    NetWeightQuantity = 300m,
                    NetWeightUnitOfMeasure = UnitOfMeasureType.KGM,
                    NetVolumeQuantity = 10m,
                    NetVolumeUnitOfMeasure = UnitOfMeasureType.LTR,
                },
            ],
        };

    [Fact]
    public void MapsCompetentCustomsOffice()
    {
        var result = ValidRequest.ToChedInterventionRequestType();

        result.CompetentCustomsOffice.Should().NotBeNull();
        result.CompetentCustomsOffice.ReferenceNumber.Should().Be("GB123456");
    }

    [Fact]
    public void MapsSendingDate()
    {
        var result = ValidRequest.ToChedInterventionRequestType();

        result.SendingDate.Should().Be(ValidRequest.SendingDate);
    }

    [Fact]
    public void MapsCustomsDocumentReference()
    {
        var result = ValidRequest.ToChedInterventionRequestType();

        result.CustomsDocumentReference.Should().Be(ValidRequest.CustomsDocumentReference);
    }

    [Fact]
    public void MapsTaricDocument()
    {
        var result = ValidRequest.ToChedInterventionRequestType();

        result.TARICDocument.Should().Be(ValidRequest.TaricDocument);
    }

    [Fact]
    public void MapsChedCertificateId()
    {
        var result = ValidRequest.ToChedInterventionRequestType();

        result.ChedCertificateId.Should().Be(ValidRequest.ChedCertificateId);
    }

    [Theory]
    [InlineData(InterventionType.DocumentCheck, TracesNT.WebServices.InterventionMessageInformationType.Item01)]
    [InlineData(InterventionType.IdentityCheck, TracesNT.WebServices.InterventionMessageInformationType.Item02)]
    [InlineData(InterventionType.PhysicalCheck, TracesNT.WebServices.InterventionMessageInformationType.Item03)]
    public void MapsInterventionType(
        InterventionType source,
        TracesNT.WebServices.InterventionMessageInformationType expected
    )
    {
        var request = ValidRequest with { InterventionType = source };

        var result = request.ToChedInterventionRequestType();

        result.InterventionType.Should().Be(expected);
    }

    [Fact]
    public void MapsConsignmentItems()
    {
        var result = ValidRequest.ToChedInterventionRequestType();

        result.ConsignmentItem.Should().ContainSingle();

        var item = result.ConsignmentItem[0];

        item.GoodsItemNumber.Should().Be("1");
        item.CertificateLineNumber.Should().Be("2");
        item.ClassCode.Should().Be("101000110");
    }

    [Fact]
    public void MapsMultipleConsignmentItems()
    {
        var request = ValidRequest with
        {
            ConsignmentItems =
            [
                ValidRequest.ConsignmentItems[0],
                ValidRequest.ConsignmentItems[0] with
                {
                    GoodsItemNumber = 3,
                    CertificateLineNumber = 4,
                    ClassCode = "02000000",
                },
            ],
        };

        var result = request.ToChedInterventionRequestType();

        result.ConsignmentItem.Should().HaveCount(2);

        result.ConsignmentItem[0].GoodsItemNumber.Should().Be("1");
        result.ConsignmentItem[0].CertificateLineNumber.Should().Be("2");
        result.ConsignmentItem[0].ClassCode.Should().Be("101000110");

        result.ConsignmentItem[1].GoodsItemNumber.Should().Be("3");
        result.ConsignmentItem[1].CertificateLineNumber.Should().Be("4");
        result.ConsignmentItem[1].ClassCode.Should().Be("02000000");
    }

    [Fact]
    public void MapsNetWeightQuantity()
    {
        var result = ValidRequest.ToChedInterventionRequestType();

        var item = result.ConsignmentItem[0];

        item.NetWeightQuantity.Should().Be(300m);
        item.NetWeightQuantitySpecified.Should().BeTrue();
    }

    [Fact]
    public void MapsNetWeightUnitOfMeasure()
    {
        var result = ValidRequest.ToChedInterventionRequestType();

        var item = result.ConsignmentItem[0];

        item.NetWeightUnitOfMeasure.Should().Be(TracesNT.WebServices.UniversalUnitOfMeasureType.KGM);

        item.NetWeightUnitOfMeasureSpecified.Should().BeTrue();
    }

    [Fact]
    public void MapsNetVolumeQuantity()
    {
        var result = ValidRequest.ToChedInterventionRequestType();

        var item = result.ConsignmentItem[0];

        item.NetVolumeQuantity.Should().Be(10m);
        item.NetVolumeQuantitySpecified.Should().BeTrue();
    }

    [Fact]
    public void MapsNetVolumeUnitOfMeasure()
    {
        var result = ValidRequest.ToChedInterventionRequestType();

        var item = result.ConsignmentItem[0];

        item.NetVolumeUnitOfMeasure.Should().Be(TracesNT.WebServices.UniversalUnitOfMeasureType.LTR);

        item.NetVolumeUnitOfMeasureSpecified.Should().BeTrue();
    }

    [Fact]
    public void DoesNotSetWeightWhenWeightQuantityIsNotSpecified()
    {
        var request = ValidRequest with
        {
            ConsignmentItems =
            [
                ValidRequest.ConsignmentItems[0] with
                {
                    NetWeightQuantity = null,
                    NetWeightUnitOfMeasure = null,
                },
            ],
        };

        var result = request.ToChedInterventionRequestType();

        var item = result.ConsignmentItem[0];

        item.NetWeightQuantitySpecified.Should().BeFalse();
        item.NetWeightUnitOfMeasureSpecified.Should().BeFalse();
    }

    [Fact]
    public void DoesNotSetVolumeWhenVolumeQuantityIsNotSpecified()
    {
        var request = ValidRequest with
        {
            ConsignmentItems =
            [
                ValidRequest.ConsignmentItems[0] with
                {
                    NetVolumeQuantity = null,
                    NetVolumeUnitOfMeasure = null,
                },
            ],
        };

        var result = request.ToChedInterventionRequestType();

        var item = result.ConsignmentItem[0];

        item.NetVolumeQuantitySpecified.Should().BeFalse();
        item.NetVolumeUnitOfMeasureSpecified.Should().BeFalse();
    }

    [Fact]
    public void DoesNotSetWeightSpecifiedFlagWhenOnlyUnitOfMeasureIsPresent()
    {
        var request = ValidRequest with
        {
            ConsignmentItems =
            [
                ValidRequest.ConsignmentItems[0] with
                {
                    NetWeightQuantity = null,
                    NetWeightUnitOfMeasure = UnitOfMeasureType.KGM,
                },
            ],
        };

        var result = request.ToChedInterventionRequestType();

        var item = result.ConsignmentItem[0];

        item.NetWeightQuantitySpecified.Should().BeFalse();
        item.NetWeightUnitOfMeasureSpecified.Should().BeTrue();
    }

    [Fact]
    public void DoesNotSetVolumeSpecifiedFlagWhenOnlyUnitOfMeasureIsPresent()
    {
        var request = ValidRequest with
        {
            ConsignmentItems =
            [
                ValidRequest.ConsignmentItems[0] with
                {
                    NetVolumeQuantity = null,
                    NetVolumeUnitOfMeasure = UnitOfMeasureType.LTR,
                },
            ],
        };

        var result = request.ToChedInterventionRequestType();

        var item = result.ConsignmentItem[0];

        item.NetVolumeQuantitySpecified.Should().BeFalse();
        item.NetVolumeUnitOfMeasureSpecified.Should().BeTrue();
    }

    [Fact]
    public void DoesNotSetWeightUnitOfMeasureWhenItIsNotSpecified()
    {
        var request = ValidRequest with
        {
            ConsignmentItems = [ValidRequest.ConsignmentItems[0] with { NetWeightUnitOfMeasure = null }],
        };

        var result = request.ToChedInterventionRequestType();

        var item = result.ConsignmentItem[0];

        item.NetWeightQuantitySpecified.Should().BeTrue();
        item.NetWeightUnitOfMeasureSpecified.Should().BeFalse();
    }

    [Fact]
    public void DoesNotSetVolumeUnitOfMeasureWhenItIsNotSpecified()
    {
        var request = ValidRequest with
        {
            ConsignmentItems = [ValidRequest.ConsignmentItems[0] with { NetVolumeUnitOfMeasure = null }],
        };

        var result = request.ToChedInterventionRequestType();

        var item = result.ConsignmentItem[0];

        item.NetVolumeQuantitySpecified.Should().BeTrue();
        item.NetVolumeUnitOfMeasureSpecified.Should().BeFalse();
    }

    [Fact]
    public void MapsAllOptionalValuesWhenPresent()
    {
        var result = ValidRequest.ToChedInterventionRequestType();

        var item = result.ConsignmentItem[0];

        item.NetWeightQuantity.Should().Be(300m);
        item.NetWeightQuantitySpecified.Should().BeTrue();
        item.NetWeightUnitOfMeasure.Should().Be(TracesNT.WebServices.UniversalUnitOfMeasureType.KGM);
        item.NetWeightUnitOfMeasureSpecified.Should().BeTrue();

        item.NetVolumeQuantity.Should().Be(10m);
        item.NetVolumeQuantitySpecified.Should().BeTrue();
        item.NetVolumeUnitOfMeasure.Should().Be(TracesNT.WebServices.UniversalUnitOfMeasureType.LTR);
        item.NetVolumeUnitOfMeasureSpecified.Should().BeTrue();
    }

    [Theory]
    [InlineData(InterventionType.DocumentCheck, TracesNT.WebServices.InterventionMessageInformationType.Item01)]
    [InlineData(InterventionType.IdentityCheck, TracesNT.WebServices.InterventionMessageInformationType.Item02)]
    [InlineData(InterventionType.PhysicalCheck, TracesNT.WebServices.InterventionMessageInformationType.Item03)]
    public void MapsEverySupportedInterventionType(
        InterventionType interventionType,
        TracesNT.WebServices.InterventionMessageInformationType expected
    )
    {
        var request = ValidRequest with { InterventionType = interventionType };

        var result = request.ToChedInterventionRequestType();

        result.InterventionType.Should().Be(expected);
    }

    [Fact]
    public void ThrowsForAnUnsupportedInterventionType()
    {
        var request = ValidRequest with { InterventionType = (InterventionType)999 };

        var action = () => request.ToChedInterventionRequestType();

        action.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("source");
    }
}
