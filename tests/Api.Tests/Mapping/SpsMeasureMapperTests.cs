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

    [Fact]
    public void MapVolume_NullSource_ReturnsNull() => SpsMeasureMapper.MapVolume(null).Should().BeNull();

    [Fact]
    public void MapVolume_AllFields_MapCorrectly()
    {
        var source = new MeasureType
        {
            Value = 25.5m,
            unitCode = "LTR",
            unitCodeListVersionID = "rec20",
        };

        var result = SpsMeasureMapper.MapVolume(source)!;

        result.Content.Should().Be("25.5");
        result.UnitCode.Should().Be("LTR");
        result.UnitCodeListVersionId.Should().Be("rec20");
        result.Value.Should().BeNull();
    }
}
