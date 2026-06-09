using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class ClassificationTreeSummaryMapperTests
{
    [Fact]
    public void Map_MapsSummary()
    {
        var source = new ClassificationTreeDescription { treeID = "intra_trade", Value = "EU Intra-trade" };

        var result = ClassificationTreeSummaryMapper.Map(source);

        result.Should().BeEquivalentTo(new { TreeId = "intra_trade", TreeName = "EU Intra-trade" });
    }
}
