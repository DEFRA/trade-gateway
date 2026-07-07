using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Tests.Mapping;

public class ChedMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_SetsConstModelAndType()
    {
        var result = ChedMapper.Map(MinimalCertificate(), Context);

        result.Model.Should().Be("defra/certificate-internal/1");
        result.Type.Should().Be("ched");
    }

    [Fact]
    public void Map_LaboratoryObservationResult_IsNull()
    {
        ChedMapper.Map(MinimalCertificate(), Context).LaboratoryObservationResult.Should().BeNull();
    }

    [Fact]
    public void Map_SpecifiedConsignment_IsNotNull()
    {
        var result = ChedMapper.Map(MinimalCertificate(), Context);

        result.SpecifiedConsignment.Should().NotBeNull();
    }

    [Fact]
    public void Map_ExchangedDocumentTypeCode_MapsFromSPSExchangedDocument()
    {
        ChedMapper.Map(MinimalCertificate(), Context).ExchangedDocument.DocumentTypeCode.Should().Be("856");
    }

    [Fact]
    public async Task ToDefraUNVTDCHEDProfileSummary_Maps_All_Properties()
    {
        // Arrange
        var created = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updated = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        var source = new FindChedCertificateResultType
        {
            offset = 10,
            pageSize = 2,
            ChedCertificateResult =
            [
                new ChedCertificateQueryResultType
                {
                    ID = "CHEDA.XI.2026.0000063",
                    CreateDateTime = created,
                    UpdateDateTime = updated,
                    CountryOfOrigin = [new IDType() { Value = "GB" }],
                },
            ],
        };

        // Act
        var result = ChedMapper.Map(source);

        // Assert
        result.Should().NotBeNull();
        await Verify(result);
    }

    [Fact]
    public void ToDefraUNVTDCHEDProfileSummary_NullResults_ReturnsEmptyItems()
    {
        var source = new FindChedCertificateResultType
        {
            offset = 0,
            pageSize = 10,
            ChedCertificateResult = null,
        };

        var result = ChedMapper.Map(source);

        result.Items.Should().NotBeNull().And.BeEmpty();
        result.HasMore.Should().BeFalse();
    }

    private static ChedCertificateType MinimalCertificate() =>
        new()
        {
            SPSCertificate = new SPSCertificateType
            {
                SPSExchangedDocument = new SPSExchangedDocumentType
                {
                    ID = new IDType { Value = "DOC-1" },
                    TypeCode = new DocumentCodeType { Value = DocumentNameCodeContentType.Item856 },
                },
                SPSConsignment = new SPSConsignmentType(),
            },
        };
}
