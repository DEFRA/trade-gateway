using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsNoteMapperTests
{
    [Fact]
    public void Map_MapsSubjectCodeValue()
    {
        var source = new SPSNoteType
        {
            SubjectCode = new CodeType { listID = "ched_note_subject_code", Value = "CHED_TYPE" },
            Content = [],
        };

        SpsNoteMapper.Map(source).SubjectCode!.Value.Should().Be("CHED_TYPE");
        SpsNoteMapper.Map(source).SubjectCode!.UrlId.Should().Be("ched_note_subject_code");
    }

    [Fact]
    public void Map_SubjectCode_IsNullWhenAbsent()
    {
        var result = SpsNoteMapper.Map(new SPSNoteType { Content = [] });

        result.Subject.Should().BeNull();
    }

    [Fact]
    public void Map_Content_MapsAllValues()
    {
        var source = new SPSNoteType
        {
            Content =
            [
                new TextType { languageID = "en", Value = "English value" },
                new TextType { languageID = "fr", Value = "Valeur française" },
            ],
        };

        SpsNoteMapper.Map(source).Content.Should().Equal("English value", "Valeur française");
    }

    [Fact]
    public void Map_Content_IsEmptyListWhenNoContent()
    {
        var source = new SPSNoteType { Content = [] };

        SpsNoteMapper.Map(source).Content.Should().BeEmpty();
    }

    [Fact]
    public void Map_ContentCodes_MapsAllEntriesWithListId()
    {
        var source = new SPSNoteType
        {
            Content = [],
            ContentCode =
            [
                new CodeType { listID = "refusal_reason", Value = "NON_APPROVED_ESTABLISHMENT" },
                new CodeType { listID = "refusal_reason_extent", Value = "PACKAGES" },
            ],
        };

        var result = SpsNoteMapper.Map(source).ContentCode;

        result.Should().HaveCount(2);
        result![0].UrlId.Should().Be("refusal_reason");
        result[0].Value.Should().Be("NON_APPROVED_ESTABLISHMENT");
        result[1].UrlId.Should().Be("refusal_reason_extent");
        result[1].Value.Should().Be("PACKAGES");
    }

    [Fact]
    public void Map_ContentCodes_IsNullWhenAbsent()
    {
        var source = new SPSNoteType { Content = [], ContentCode = null };

        SpsNoteMapper.Map(source).ContentCode.Should().BeNull();
    }

    [Fact]
    public void Map_ContentCodes_IsNullWhenEmpty()
    {
        var source = new SPSNoteType { Content = [], ContentCode = [] };

        SpsNoteMapper.Map(source).ContentCode.Should().BeNull();
    }

    [Fact]
    public void Map_Content_AndContentCodes_BothPreserved()
    {
        var source = new SPSNoteType
        {
            Content = [new TextType { languageID = "en", Value = "test establishment" }],
            ContentCode = [new CodeType { listID = "refusal_reason", Value = "NON_APPROVED_ESTABLISHMENT" }],
        };

        var result = SpsNoteMapper.Map(source);

        result.Content.Should().Equal("test establishment");
        result.ContentCode.Should().HaveCount(1);
        result.ContentCode![0].Value.Should().Be("NON_APPROVED_ESTABLISHMENT");
    }

    [Fact]
    public void MapList_NullSource_ReturnsEmptyList() => SpsNoteMapper.MapList(null).Should().BeEmpty();

    [Fact]
    public void MapList_EmptySource_ReturnsEmptyList() => SpsNoteMapper.MapList([]).Should().BeEmpty();

    [Fact]
    public void MapList_MapsEveryNoteInOrder()
    {
        SPSNoteType[] source =
        [
            new() { Subject = new TextType { Value = "first" } },
            new() { Subject = new TextType { Value = "second" } },
        ];

        SpsNoteMapper.MapList(source).Select(n => n.Subject).Should().Equal("first", "second");
    }
}
