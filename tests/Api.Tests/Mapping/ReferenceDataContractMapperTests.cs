using Api.Mapping;
using AwesomeAssertions;
using Defra.TradeGateway.Api.Contract.ReferenceData;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class ReferenceDataContractMapperTests
{
    [Fact]
    public void Map_ClassificationTrees_MapsSummaries()
    {
        ClassificationTreeDescription[] response =
            [
                new ClassificationTreeDescription { treeID = "intra_trade", Value = "EU Intra-trade" },
                new ClassificationTreeDescription { treeID = "cheda", Value = "CHED-A" },
            ];

        var result = response.Select(ClassificationTreeSummaryMapper.Map).ToList();

        result.Should()
            .BeEquivalentTo(
                new List<ClassificationTreeSummary>
                {
                    new() { TreeId = "intra_trade", TreeName = "EU Intra-trade" },
                    new() { TreeId = "cheda", TreeName = "CHED-A" },
                }
            );
    }

    [Fact]
    public void Map_ClassificationTrees_EmptyList_ReturnsNull()
    {
        Array.Empty<ClassificationTreeDescription>().Should().BeEmpty();
    }

    [Fact]
    public void Map_MetadataList_MapsResponse()
    {
        MetadataCodeType[] response =
            [
                new MetadataCodeType { Value = "A", mappedValue = "mapped-a", active = true },
                new MetadataCodeType { Value = "B", mappedValue = "mapped-b", active = false },
            ];

        var result = MetadataMapper.Map(response, "operatorActivityType");

        result.MetadataType.Should().Be("operatorActivityType");
        result.RetrievedAt.Should().NotBeNull();
        result.Items.Should()
            .BeEquivalentTo(
                new List<MetadataCode>
                {
                    new() { Value = "A", MappedValue = "mapped-a", Active = true },
                    new() { Value = "B", MappedValue = "mapped-b", Active = false },
                }
            );
    }

    [Fact]
    public void Map_MetadataList_EmptyList_ReturnsNullItems()
    {
        MetadataMapper.Map([], "operatorActivityType").Items.Should().BeEmpty();
    }
}
