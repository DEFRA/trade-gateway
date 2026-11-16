using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsTradeLineItemMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_SequenceNumeric_MapsFromDecimal()
    {
        var source = new SPSTradeLineItemType { SequenceNumeric = new NumericType { Value = 3m } };

        SpsTradeLineItemMapper.Map(source, Context).SequenceNumeric.Should().Be(3);
    }

    [Fact]
    public void Map_Description_FiltersToContextLanguage()
    {
        var source = new SPSTradeLineItemType
        {
            Description =
            [
                new TextType { languageID = "fr", Value = "Boeuf" },
                new TextType { languageID = "en", Value = "Beef" },
            ],
        };

        SpsTradeLineItemMapper.Map(source, Context).Description.Should().ContainSingle().Which.Should().Be("Beef");
    }

    [Fact]
    public void Map_Description_NoContextLanguageEntry_ReturnsNull()
    {
        var source = new SPSTradeLineItemType { Description = [new TextType { languageID = "fr", Value = "Boeuf" }] };

        SpsTradeLineItemMapper.Map(source, Context).Description.Should().BeNull();
    }

    [Fact]
    public void Map_ScientificName_FiltersToLatin()
    {
        var source = new SPSTradeLineItemType
        {
            ScientificName =
            [
                new TextType { languageID = "en", Value = "Donkey" },
                new TextType { languageID = "la", Value = "Equus asinus" },
            ],
        };

        SpsTradeLineItemMapper.Map(source, Context).ScientificName.Should().Be("Equus asinus");
    }

    [Fact]
    public void Map_ScientificName_NoLatinEntry_ReturnsNull()
    {
        var source = new SPSTradeLineItemType
        {
            ScientificName = [new TextType { languageID = "en", Value = "Donkey" }],
        };

        SpsTradeLineItemMapper.Map(source, Context).ScientificName.Should().BeNull();
    }

    [Fact]
    public void Map_NetAndGrossWeight_MapViaSpsMeasureMapper()
    {
        var source = new SPSTradeLineItemType
        {
            NetWeightMeasure = new MeasureType { Value = 100m, unitCode = "KGM" },
            GrossWeightMeasure = new MeasureType { Value = 110m, unitCode = "KGM" },
        };

        var result = SpsTradeLineItemMapper.Map(source, Context);

        result.NetWeight!.Content.Should().Be("100");
        result.GrossWeight!.Content.Should().Be("110");
    }

    [Fact]
    public void Map_NetVolume_MapsViaSpsMeasureMapper()
    {
        var source = new SPSTradeLineItemType
        {
            NetVolumeMeasure = new MeasureType { Value = 25.5m, unitCode = "LTR" },
        };

        var result = SpsTradeLineItemMapper.Map(source, Context);

        result.NetVolume!.Content.Should().Be("25.5");
        result.NetVolume.UnitCode.Should().Be("LTR");
        result.NetVolume.Value.Should().BeNull();
    }

    [Fact]
    public void Map_OriginCountry_MapsFromLineLevelCountry()
    {
        var source = new SPSTradeLineItemType
        {
            OriginSPSCountry = [new SPSCountryType { ID = new IDType { Value = "IE" } }],
        };

        SpsTradeLineItemMapper.Map(source, Context).OriginCountry!.Code?.Value.Should().Be("IE");
    }

    [Fact]
    public void Map_AppliedProcess_MapsList()
    {
        var source = new SPSTradeLineItemType
        {
            AppliedSPSProcess =
            [
                new SPSProcessType { TypeCode = new ProcessTypeCodeType { Value = ProcessTypeCodeContentType.Item3 } },
            ],
        };

        SpsTradeLineItemMapper
            .Map(source, Context)
            .AppliedProcess.Should()
            .ContainSingle()
            .Which.TypeCode.Should()
            .Be("3");
    }

    [Fact]
    public void Map_AdditionalInformationNote_MapsList()
    {
        var source = new SPSTradeLineItemType
        {
            AdditionalInformationSPSNote =
            [
                new SPSNoteType
                {
                    SubjectCode = new CodeType
                    {
                        listID = "ched_commodity_note_subject_code",
                        Value = "INDIVIDUAL_IDENTIFICATION_NUMBER",
                    },
                    Content = [new TextType { Value = "GB0987645768734" }],
                },
            ],
        };

        var note = SpsTradeLineItemMapper
            .Map(source, Context)
            .AdditionalInformationNote.Should()
            .ContainSingle()
            .Subject;

        note.SubjectCode!.Value.Should().Be("INDIVIDUAL_IDENTIFICATION_NUMBER");
        note.Content.Should().ContainSingle().Which.Should().Be("GB0987645768734");
    }

    [Fact]
    public void Map_NullProperties_ReturnNullFields()
    {
        var result = SpsTradeLineItemMapper.Map(new SPSTradeLineItemType(), Context);

        result.SequenceNumeric.Should().BeNull();
        result.Description.Should().BeNull();
        result.ScientificName.Should().BeNull();
        result.NetWeight.Should().BeNull();
        result.GrossWeight.Should().BeNull();
        result.NetVolume.Should().BeNull();
        result.ApplicableClassification.Should().BeNull();
        result.PhysicalReferencedLogisticsPackage.Should().BeNull();
        result.AdditionalInformationNote.Should().BeNull();
    }

    [Fact]
    public void MapList_NullSource_ReturnsNull() => SpsTradeLineItemMapper.MapList(null, Context).Should().BeNull();

    [Fact]
    public void MapList_EmptyArray_ReturnsNull() => SpsTradeLineItemMapper.MapList([], Context).Should().BeNull();
}
