using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class TaxonMapperTests
{
    [Fact]
    public void Map_MapsTaxon()
    {
        var source = new TaxonReference
        {
            taxonId = 123,
            eppoCode = "EQCAB",
            faoCode = "HOR",
            Value = "Equus caballus",
            languageID = "la",
        };

        var result = TaxonMapper.Map(source);

        result.Should()
            .BeEquivalentTo(
                new
                {
                    TaxonId = 123,
                    EppoCode = "EQCAB",
                    FaoCode = "HOR",
                    Name = "Equus caballus",
                    LanguageId = "la",
                }
            );
    }
}
