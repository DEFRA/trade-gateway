using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsDateTimeMapperTests
{
    [Fact]
    public void Map_NullSource_ReturnsNull() => SpsDateTimeMapper.Map(null).Should().BeNull();

    [Fact]
    public void Map_NullItem_ReturnsNull() => SpsDateTimeMapper.Map(new DateTimeType()).Should().BeNull();

    [Fact]
    public void Map_UnexpectedItemType_ReturnsNull() =>
        SpsDateTimeMapper.Map(new DateTimeType { Item = "2024-03-01" }).Should().BeNull();

    [Fact]
    public void Map_UnspecifiedKindDateTime_IsTreatedAsUtc()
    {
        var source = new DateTimeType { Item = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Unspecified) };

        var result = SpsDateTimeMapper.Map(source)!.Value;

        result.Offset.Should().Be(TimeSpan.Zero);
        result.UtcDateTime.Should().Be(new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Map_UtcDateTime_KeepsZeroOffset()
    {
        var source = new DateTimeType { Item = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc) };

        var result = SpsDateTimeMapper.Map(source)!.Value;

        result.Offset.Should().Be(TimeSpan.Zero);
        result.UtcDateTime.Should().Be(new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Map_LocalDateTime_UsesLocalOffset()
    {
        var local = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Local);

        var result = SpsDateTimeMapper.Map(new DateTimeType { Item = local })!.Value;

        result.Offset.Should().Be(TimeZoneInfo.Local.GetUtcOffset(local));
        result.UtcDateTime.Should().Be(local.ToUniversalTime());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Map_BlankDateTimeString_ReturnsNull(string? value) =>
        SpsDateTimeMapper
            .Map(new DateTimeType { Item = new DateTimeTypeDateTimeString { Value = value! } })
            .Should()
            .BeNull();

    [Fact]
    public void Map_DateTimeStringWithOffset_PreservesOffset()
    {
        var source = new DateTimeType { Item = new DateTimeTypeDateTimeString { Value = "2024-03-01T12:00:00+02:00" } };

        var result = SpsDateTimeMapper.Map(source)!.Value;

        result.Should().Be(new DateTimeOffset(2024, 3, 1, 12, 0, 0, TimeSpan.FromHours(2)));
    }

    [Fact]
    public void Map_DateTimeStringInUtc_ReturnsZeroOffset()
    {
        var source = new DateTimeType { Item = new DateTimeTypeDateTimeString { Value = "2024-03-01T12:00:00Z" } };

        var result = SpsDateTimeMapper.Map(source)!.Value;

        result.Should().Be(new DateTimeOffset(2024, 3, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Map_UnparseableDateTimeString_Throws()
    {
        var source = new DateTimeType { Item = new DateTimeTypeDateTimeString { Value = "not a date" } };

        var act = () => SpsDateTimeMapper.Map(source);

        act.Should().Throw<FormatException>();
    }
}
