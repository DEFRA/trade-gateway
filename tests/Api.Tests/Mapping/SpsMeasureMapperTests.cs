using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsMeasureMapperTests
{
    [Fact]
    public void Map_NullSource_ReturnsNull() => SpsMeasureMapper.Map(null).Should().BeNull();

    [Fact]
    public void Map_AllFields_MapCorrectly()
    {
        var source = new MeasureType
        {
            Value = 12.5m,
            unitCode = "KGM",
            unitCodeListVersionID = "rec20",
        };

        var result = SpsMeasureMapper.Map(source)!;

        result.Content.Should().Be("12.5");
        result.UnitCode.Should().Be("KGM");
        result.UnitCodeListVersionId.Should().Be("rec20");
        result.Value.Should().BeNull();
    }
}
