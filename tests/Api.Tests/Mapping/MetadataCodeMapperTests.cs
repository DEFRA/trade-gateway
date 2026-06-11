using Api.Mapping;
using AwesomeAssertions;
using Trade.Gateway.Api.Contract.ReferenceData;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class MetadataCodeMapperTests
{
    [Fact]
    public void Map_MapsMetadataCode()
    {
        var source = new MetadataCodeType
        {
            Value = "A",
            mappedValue = "mapped-a",
            active = false,
            name = "Test Metadata Code",
        };

        var result = MetadataCodeMapper.Map(source);

        result.Should()
            .BeEquivalentTo(
                new MetadataCode
                {
                    Value = "A",
                    MappedValue = "mapped-a",
                    Active = false,
                    DisplayName = "Test Metadata Code"
                }
            );
    }
}
