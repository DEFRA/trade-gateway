using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsAddressMapperTests
{
    [Fact]
    public void Map_NullSource_ReturnsNull() =>
        SpsAddressMapper.Map(null).Should().BeNull();

    [Fact]
    public void Map_AllFields_MapCorrectly()
    {
        var source = new SPSAddressType
        {
            PostcodeCode = new CodeType { Value = "BT1" },
            LineOne = new TextType { Value = "Line 1" },
            LineTwo = new TextType { Value = "Line 2" },
            CityName = new TextType { Value = "Belfast" },
            CountryID = new IDType { Value = "XI" },
            CountryName = new TextType { Value = "United Kingdom (Northern Ireland)" },
            CountrySubDivisionName = new TextType { Value = "County Antrim" }
        };

        var result = SpsAddressMapper.Map(source)!;

        result.PostcodeCode.Should().Be("BT1");
        result.LineOne.Should().Be("Line 1");
        result.LineTwo.Should().Be("Line 2");
        result.CityName.Should().Be("Belfast");
        result.CountryId.Should().Be("XI");
        result.CountryName.Should().Be("United Kingdom (Northern Ireland)");
        result.CountrySubDivisionName.Should().Be("County Antrim");
    }

    [Fact]
    public void Map_NullProperties_ReturnNullFields()
    {
        var result = SpsAddressMapper.Map(new SPSAddressType())!;

        result.PostcodeCode.Should().BeNull();
        result.LineOne.Should().BeNull();
        result.CountryId.Should().BeNull();
    }
}
