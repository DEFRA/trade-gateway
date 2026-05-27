using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsConsignmentItemMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_NatureIdCargo_MapsFromNatureIdentificationSPSCargo()
    {
        var source = new SPSConsignmentItemType
        {
            NatureIdentificationSPSCargo =
            [
                new SPSCargoType
                {
                    TypeCode = new CargoTypeClassificationCodeType
                    {
                        Value = CargoTypeClassificationCodeContentType.Item1,
                    },
                },
            ],
        };

        SpsConsignmentItemMapper
            .Map(source, Context)
            .NatureIdCargo.Should()
            .ContainSingle()
            .Which.TypeCode.Should()
            .Be("1");
    }

    [Fact]
    public void Map_IncludedTradeLineItem_MapsFromIncludedSPSTradeLineItem()
    {
        var source = new SPSConsignmentItemType
        {
            IncludedSPSTradeLineItem =
            [
                new SPSTradeLineItemType { SequenceNumeric = new NumericType { Value = 1m } },
                new SPSTradeLineItemType { SequenceNumeric = new NumericType { Value = 2m } },
            ],
        };

        var result = SpsConsignmentItemMapper.Map(source, Context);

        result.IncludedTradeLineItem.Should().HaveCount(2);
        result.IncludedTradeLineItem![0].SequenceNumeric.Should().Be(1);
        result.IncludedTradeLineItem![1].SequenceNumeric.Should().Be(2);
    }

    [Fact]
    public void Map_NullProperties_ReturnNullFields()
    {
        var result = SpsConsignmentItemMapper.Map(new SPSConsignmentItemType(), Context);

        result.NatureIdCargo.Should().BeNull();
        result.IncludedTradeLineItem.Should().BeNull();
    }

    [Fact]
    public void MapList_NullSource_ReturnsNull() => SpsConsignmentItemMapper.MapList(null, Context).Should().BeNull();

    [Fact]
    public void MapList_EmptyArray_ReturnsNull() => SpsConsignmentItemMapper.MapList([], Context).Should().BeNull();
}
