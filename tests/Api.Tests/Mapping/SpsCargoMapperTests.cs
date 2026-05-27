using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsCargoMapperTests
{
    [Fact]
    public void Map_TypeCode_MapsViaXmlEnumCode()
    {
        var source = new SPSCargoType
        {
            TypeCode = new CargoTypeClassificationCodeType { Value = CargoTypeClassificationCodeContentType.Item1 }
        };

        SpsCargoMapper.Map(source).TypeCode.Should().Be("1");
    }

    [Fact]
    public void Map_NullTypeCode_ReturnsNullTypeCode()
    {
        SpsCargoMapper.Map(new SPSCargoType()).TypeCode.Should().BeNull();
    }

    [Fact]
    public void MapList_NullSource_ReturnsNull() =>
        SpsCargoMapper.MapList(null).Should().BeNull();

    [Fact]
    public void MapList_EmptyArray_ReturnsNull() =>
        SpsCargoMapper.MapList([]).Should().BeNull();

    [Fact]
    public void MapList_MultipleEntries_MapsAll()
    {
        var source = new[]
        {
            new SPSCargoType { TypeCode = new CargoTypeClassificationCodeType { Value = CargoTypeClassificationCodeContentType.Item1 } },
            new SPSCargoType { TypeCode = new CargoTypeClassificationCodeType { Value = CargoTypeClassificationCodeContentType.Item2 } }
        };

        var result = SpsCargoMapper.MapList(source)!;

        result.Should().HaveCount(2);
        result[0].TypeCode.Should().Be("1");
        result[1].TypeCode.Should().Be("2");
    }
}
