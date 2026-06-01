using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class LegislationAttributeMapperTests
{
    [Fact]
    public void Map_MapsLegislationAttribute()
    {
        var source = new LegislationNodeAttribute
        {
            id = "LEGISLATION",
            Description = new TextType { Value = "Applicable legislation" },
            LegislationReference = new LegislationReference
            {
                legislationId = 123,
                CelexIdentifier = [new IDType { Value = "32020R0692" }],
                OriginCountry = [new IDType { Value = "GB" }],
                DestinationCountry = [new IDType { Value = "FR" }],
            },
        };

        var result = LegislationAttributeMapper.Map(source);

        result.Should()
            .BeEquivalentTo(
                new
                {
                    Key = "LEGISLATION",
                    Description = "Applicable legislation",
                    Legislation = new[]
                    {
                        new
                        {
                            LegislationId = 123,
                            CelexIdentifiers = new[] { "32020R0692" },
                            OriginCountries = new[] { "GB" },
                            DestinationCountries = new[] { "FR" },
                        },
                    },
                }
            );
    }
}
