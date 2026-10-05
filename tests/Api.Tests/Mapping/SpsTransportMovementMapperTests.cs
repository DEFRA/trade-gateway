using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsTransportMovementMapperTests
{
    [Fact]
    public void Map_AllFields_MapFromCorrectSourceFields()
    {
        var source = new SPSTransportMovementType
        {
            ID = new IDType { Value = "VESSEL-123" },
            ModeCode = new TransportModeCodeType { Value = TransportModeCodeContentType.Item1 },
            UsedSPSTransportMeans = new SPSTransportMeansType { Name = new TextType { Value = "MV Example" } },
        };

        var result = SpsTransportMovementMapper.Map(source);

        result!.Identifier.Should().Be("VESSEL-123");
        result.ModeCode.Should().Be(1);
    }

    [Fact]
    public void Map_NoMovementId_FallsBackToTransportMeansName()
    {
        var source = new SPSTransportMovementType
        {
            UsedSPSTransportMeans = new SPSTransportMeansType { Name = new TextType { Value = "MV Example" } },
        };

        SpsTransportMovementMapper.Map(source)!.Identifier.Should().Be("MV Example");
    }

    [Fact]
    public void Map_Null_ReturnsNull()
    {
        SpsTransportMovementMapper.Map(null).Should().BeNull();
    }

    [Fact]
    public void Map_EmptyMovement_ReturnsNullTargetFields()
    {
        var result = SpsTransportMovementMapper.Map(new SPSTransportMovementType());

        result.Should().NotBeNull();
        result!.Identifier.Should().BeNull();
        result.ModeCode.Should().BeNull();
    }

    [Fact]
    public void Map_NonNumericModeCode_ReturnsNullModeCode()
    {
        var source = new SPSTransportMovementType
        {
            ModeCode = new TransportModeCodeType { Value = TransportModeCodeContentType.Item1, name = "Maritime" },
        };

        var result = SpsTransportMovementMapper.Map(source);

        // XmlEnumCode reads [XmlEnum("1")] -> "1" -> parses to 1
        result!.ModeCode.Should().Be(1);
    }

    [Fact]
    public void MapList_MapsEveryLeg()
    {
        SPSTransportMovementType[] source =
        [
            new() { ID = new IDType { Value = "FIRST" } },
            new() { ID = new IDType { Value = "SECOND" } },
        ];

        var result = SpsTransportMovementMapper.MapList(source);

        result!.Select(m => m.Identifier).Should().Equal("FIRST", "SECOND");
    }

    [Fact]
    public void MapList_NullOrEmpty_ReturnsNull()
    {
        SpsTransportMovementMapper.MapList(null).Should().BeNull();
        SpsTransportMovementMapper.MapList([]).Should().BeNull();
    }
}
