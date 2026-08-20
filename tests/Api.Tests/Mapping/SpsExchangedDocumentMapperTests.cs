using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsExchangedDocumentMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_Identifier_MapsFromId()
    {
        var source = MinimalDocument();
        source.ID = new IDType { Value = "INTRA.EU.FR.2026.0000041" };

        SpsExchangedDocumentMapper.Map(source, Context).Identifier.Should().Be("INTRA.EU.FR.2026.0000041");
    }

    [Fact]
    public void Map_DocumentTypeCode_ExtractsXmlEnumCode()
    {
        var source = MinimalDocument();
        source.TypeCode = new DocumentCodeType { Value = DocumentNameCodeContentType.Item856 };

        SpsExchangedDocumentMapper.Map(source, Context).DocumentTypeCode.Should().Be("856");
    }

    [Fact]
    public void Map_Name_PrefersContextLanguage()
    {
        var source = MinimalDocument();
        source.Name =
        [
            new TextType { languageID = "fr", Value = "Nom" },
            new TextType { languageID = "en", Value = "Certificate Name" },
        ];

        SpsExchangedDocumentMapper.Map(source, Context).Name.Should().Be("Certificate Name");
    }

    [Fact]
    public void Map_Name_FallsBackToNullLanguageId()
    {
        var source = MinimalDocument();
        source.Name = [new TextType { Value = "Certificate Name" }, new TextType { Value = "Other" }];

        SpsExchangedDocumentMapper.Map(source, Context).Name.Should().Be("Certificate Name");
    }

    [Fact]
    public void Map_Authentications_MapToSlotsByTypeCode()
    {
        var source = MinimalDocument();
        source.SignatorySPSAuthentication =
        [
            new SPSAuthenticationType
            {
                TypeCode = new GovernmentActionCodeType
                {
                    Value = GovernmentActionCodeContentType.Item4,
                    name = "First",
                },
            },
            new SPSAuthenticationType
            {
                TypeCode = new GovernmentActionCodeType
                {
                    Value = GovernmentActionCodeContentType.Item1,
                    name = "Second",
                },
            },
            new SPSAuthenticationType
            {
                TypeCode = new GovernmentActionCodeType
                {
                    Value = GovernmentActionCodeContentType.Item8,
                    name = "Third",
                },
            },
        ];

        var result = SpsExchangedDocumentMapper.Map(source, Context);

        result.FirstSignatoryAuthentication!.GovernmentActionTypeCode.Should().Be("First");
        result.SecondSignatoryAuthentication!.GovernmentActionTypeCode.Should().Be("Second");
        result.ThirdSignatoryAuthentication!.GovernmentActionTypeCode.Should().Be("Third");
    }

    [Fact]
    public void Map_AuthenticationWithUnmappedTypeCode_OtherSlotsNull()
    {
        var source = MinimalDocument();
        source.SignatorySPSAuthentication =
        [
            new SPSAuthenticationType
            {
                TypeCode = new GovernmentActionCodeType
                {
                    Value = GovernmentActionCodeContentType.Item4,
                    name = "First",
                },
            },
        ];

        var result = SpsExchangedDocumentMapper.Map(source, Context);

        result.FirstSignatoryAuthentication!.GovernmentActionTypeCode.Should().Be("First");
        result.SecondSignatoryAuthentication.Should().BeNull();
        result.ThirdSignatoryAuthentication.Should().BeNull();
    }

    [Fact]
    public void Map_NullNotes_ReturnsEmptyIncludedNote()
    {
        SpsExchangedDocumentMapper.Map(MinimalDocument(), Context).IncludedNote.Should().BeEmpty();
    }

    [Fact]
    public void Map_EmptyNotes_ReturnsEmptyIncludedNote()
    {
        var source = MinimalDocument();
        source.IncludedSPSNote = [];

        SpsExchangedDocumentMapper.Map(source, Context).IncludedNote.Should().BeEmpty();
    }

    private static SPSExchangedDocumentType MinimalDocument() =>
        new()
        {
            ID = new IDType { Value = "DOC-1" },
            TypeCode = new DocumentCodeType { Value = DocumentNameCodeContentType.Item856 },
        };
}
