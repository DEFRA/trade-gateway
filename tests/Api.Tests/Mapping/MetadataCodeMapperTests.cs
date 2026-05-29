using Api.Mapping;
using AwesomeAssertions;
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
        };

        var result = MetadataCodeMapper.Map(source);

        result.Should()
            .BeEquivalentTo(
                new
                {
                    Value = "A",
                    MappedValue = "mapped-a",
                    Active = false,
                }
            );
    }
}
