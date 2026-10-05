using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsExaminationEventMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void MapList_NullSource_ReturnsNull() =>
        SpsExaminationEventMapper.MapList(null, Context).Should().BeNull();

    [Fact]
    public void MapList_Location_MapsOccurrence()
    {
        var source = new SPSEventType
        {
            OccurrenceSPSLocation = new SPSLocationType
            {
                ID = new IDType { Value = "GBDVR1", schemeID = "un_locode" },
                Name = [new TextType { Value = "Dover", languageID = "en" }],
            },
        };

        var result = SpsExaminationEventMapper.MapList(source, Context)!.Single();

        result.OccurrenceLogisticsLocation!.Identifier.Should().Be("GBDVR1");
        result
            .OccurrenceLogisticsLocation.UrlId.Should()
            .Be("https://traces-codelists.ec.europa.eu/un_locode");
        result.OccurrenceLogisticsLocation.Name.Should().Be("Dover");
    }

    [Fact]
    public void MapList_EmptyLocation_ReturnsNull()
    {
        var source = new SPSEventType { OccurrenceSPSLocation = new SPSLocationType() };

        SpsExaminationEventMapper.MapList(source, Context).Should().BeNull();
    }
}
