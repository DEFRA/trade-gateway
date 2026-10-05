using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsProcessMapperTests
{
    [Fact]
    public void Map_NullList_ReturnsNull() => SpsProcessMapper.MapList(null).Should().BeNull();

    [Fact]
    public void Map_EmptyList_ReturnsNull() => SpsProcessMapper.MapList([]).Should().BeNull();

    [Fact]
    public void Map_Process_MapsTypeCodeAndOperator()
    {
        var source = new SPSProcessType
        {
            TypeCode = new ProcessTypeCodeType { Value = ProcessTypeCodeContentType.Item3, name = "Treatment" },
            OperatorSPSParty = new SPSPartyType { ID = new IDType { Value = "OPERATOR" } },
        };

        var result = SpsProcessMapper.MapList([source])!.Single();

        result.TypeCode.Should().Be("3");
        result.UrlId.Should().Be("https://traces-codelists.ec.europa.eu/7187");
        result.OperatorParty!.Identifier.Should().Be("OPERATOR");
    }

    [Fact]
    public void Map_NoTypeCode_LeavesCodeAndUrlNull()
    {
        var result = SpsProcessMapper.MapList([new SPSProcessType()])!.Single();

        result.TypeCode.Should().BeNull();
        result.UrlId.Should().BeNull();
        result.OperatorParty.Should().BeNull();
    }
}
