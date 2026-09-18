using Api.Mapping;
using Api.Models;
using AwesomeAssertions;
using Trade.Gateway.Api.Contract.Customs;

namespace Api.Tests.Mapping;

public class ChedReservationInterventionMapperTests
{
    private static ChedReservationInterventionRequest ValidRequest =>
        new()
        {
            TaricDocument = "TARIC-123",
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

    [Theory]
    [InlineData(InterventionType.ForceWriteOff, TracesNT.WebServices.InterventionMessageInformationType.Item01)]
    [InlineData(InterventionType.AmendWriteOff, TracesNT.WebServices.InterventionMessageInformationType.Item02)]
    [InlineData(InterventionType.DeleteWriteOff, TracesNT.WebServices.InterventionMessageInformationType.Item03)]
    public void MapsInterventionType(
        InterventionType source,
        TracesNT.WebServices.InterventionMessageInformationType expected
    )
    {
        var result = source.ToCertexInterventionType();

        result.Should().Be(expected);
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

        var consignmentItems = request.ConsignmentItems.ToCertexConsignmentItems().ToArray();

        consignmentItems.Should().HaveCount(2);

        consignmentItems[0].GoodsItemNumber.Should().Be("1");
        consignmentItems[0].CertificateLineNumber.Should().Be("2");
        consignmentItems[0].ClassCode.Should().Be("101000110");

        consignmentItems[1].GoodsItemNumber.Should().Be("3");
        consignmentItems[1].CertificateLineNumber.Should().Be("4");
        consignmentItems[1].ClassCode.Should().Be("02000000");
    }

    [Fact]
    public void MapsNetWeightQuantity()
    {
        var firstConsignmentItem = ValidRequest.ConsignmentItems.ToCertexConsignmentItems().Single();

        firstConsignmentItem.NetWeightQuantity.Should().Be(300m);
        firstConsignmentItem.NetWeightQuantitySpecified.Should().BeTrue();
    }

    [Fact]
    public void MapsNetWeightUnitOfMeasure()
    {
        var firstConsignmentItem = ValidRequest.ConsignmentItems.ToCertexConsignmentItems().Single();

        firstConsignmentItem.NetWeightUnitOfMeasure.Should().Be(TracesNT.WebServices.UniversalUnitOfMeasureType.KGM);

        firstConsignmentItem.NetWeightUnitOfMeasureSpecified.Should().BeTrue();
    }

    [Fact]
    public void MapsNetVolumeQuantity()
    {
        var firstConsignmentItem = ValidRequest.ConsignmentItems.ToCertexConsignmentItems().Single();

        firstConsignmentItem.NetVolumeQuantity.Should().Be(10m);
        firstConsignmentItem.NetVolumeQuantitySpecified.Should().BeTrue();
    }

    [Fact]
    public void MapsNetVolumeUnitOfMeasure()
    {
        var firstConsignmentItem = ValidRequest.ConsignmentItems.ToCertexConsignmentItems().Single();

        firstConsignmentItem.NetVolumeUnitOfMeasure.Should().Be(TracesNT.WebServices.UniversalUnitOfMeasureType.LTR);

        firstConsignmentItem.NetVolumeUnitOfMeasureSpecified.Should().BeTrue();
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

        var firstConsignmentItem = request.ConsignmentItems.ToCertexConsignmentItems().Single();

        firstConsignmentItem.NetWeightQuantitySpecified.Should().BeFalse();
        firstConsignmentItem.NetWeightUnitOfMeasureSpecified.Should().BeFalse();
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

        var firstConsignmentItem = request.ConsignmentItems.ToCertexConsignmentItems().Single();

        firstConsignmentItem.NetVolumeQuantitySpecified.Should().BeFalse();
        firstConsignmentItem.NetVolumeUnitOfMeasureSpecified.Should().BeFalse();
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

        var firstConsignmentItem = request.ConsignmentItems.ToCertexConsignmentItems().Single();

        firstConsignmentItem.NetWeightQuantitySpecified.Should().BeFalse();
        firstConsignmentItem.NetWeightUnitOfMeasureSpecified.Should().BeTrue();
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

        var firstConsignmentItem = request.ConsignmentItems.ToCertexConsignmentItems().Single();

        firstConsignmentItem.NetVolumeQuantitySpecified.Should().BeFalse();
        firstConsignmentItem.NetVolumeUnitOfMeasureSpecified.Should().BeTrue();
    }

    [Fact]
    public void DoesNotSetWeightUnitOfMeasureWhenItIsNotSpecified()
    {
        var request = ValidRequest with
        {
            ConsignmentItems = [ValidRequest.ConsignmentItems[0] with { NetWeightUnitOfMeasure = null }],
        };

        var firstConsignmentItem = request.ConsignmentItems.ToCertexConsignmentItems().Single();

        firstConsignmentItem.NetWeightQuantitySpecified.Should().BeTrue();
        firstConsignmentItem.NetWeightUnitOfMeasureSpecified.Should().BeFalse();
    }

    [Fact]
    public void DoesNotSetVolumeUnitOfMeasureWhenItIsNotSpecified()
    {
        var request = ValidRequest with
        {
            ConsignmentItems = [ValidRequest.ConsignmentItems[0] with { NetVolumeUnitOfMeasure = null }],
        };

        var firstConsignmentItem = request.ConsignmentItems.ToCertexConsignmentItems().Single();

        firstConsignmentItem.NetVolumeQuantitySpecified.Should().BeTrue();
        firstConsignmentItem.NetVolumeUnitOfMeasureSpecified.Should().BeFalse();
    }

    [Fact]
    public void MapsAllOptionalValuesWhenPresent()
    {
        var firstConsignmentItem = ValidRequest.ConsignmentItems.ToCertexConsignmentItems().Single();

        firstConsignmentItem.NetWeightQuantity.Should().Be(300m);
        firstConsignmentItem.NetWeightQuantitySpecified.Should().BeTrue();
        firstConsignmentItem.NetWeightUnitOfMeasure.Should().Be(TracesNT.WebServices.UniversalUnitOfMeasureType.KGM);
        firstConsignmentItem.NetWeightUnitOfMeasureSpecified.Should().BeTrue();

        firstConsignmentItem.NetVolumeQuantity.Should().Be(10m);
        firstConsignmentItem.NetVolumeQuantitySpecified.Should().BeTrue();
        firstConsignmentItem.NetVolumeUnitOfMeasure.Should().Be(TracesNT.WebServices.UniversalUnitOfMeasureType.LTR);
        firstConsignmentItem.NetVolumeUnitOfMeasureSpecified.Should().BeTrue();
    }
}
