using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsPackageMapperTests
{
    [Fact]
    public void Map_AllFields_MapCorrectly()
    {
        var source = new SPSPackageType
        {
            LevelCode = new CodeType { Value = "2" },
            TypeCode = new PackageTypeCodeType { Value = PackageTypeCodeContentType.Item43 },
            ItemQuantity = new QuantityType { Value = 10m },
        };

        var result = SpsPackageMapper.Map(source);

        result.LevelCode.Should().Be(2);
        result.TypeCode.Should().Be("43");
        result.ItemQuantity.Should().Be(10);
    }

    [Fact]
    public void Map_NonNumericLevelCode_ReturnsNullLevelCode()
    {
        var source = new SPSPackageType { LevelCode = new CodeType { Value = "X" } };

        SpsPackageMapper.Map(source).LevelCode.Should().BeNull();
    }

    [Fact]
    public void Map_NullProperties_ReturnNullFields()
    {
        var result = SpsPackageMapper.Map(new SPSPackageType());

        result.LevelCode.Should().BeNull();
        result.TypeCode.Should().BeNull();
        result.ItemQuantity.Should().BeNull();
    }

    [Fact]
    public void MapList_NullSource_ReturnsNull() => SpsPackageMapper.MapList(null).Should().BeNull();

    [Fact]
    public void MapList_EmptyArray_ReturnsNull() => SpsPackageMapper.MapList([]).Should().BeNull();
}
