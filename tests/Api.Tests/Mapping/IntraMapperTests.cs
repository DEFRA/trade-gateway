using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;
using Trade.Gateway.Api.Contract.Certificate;

namespace Api.Tests.Mapping;

public class IntraMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_SetsConstModelAndType()
    {
        var result = IntraMapper.Map(MinimalCertificate(), Context);

        result.Model.Should().Be("defra/certificate-internal/1");
        result.Type.Should().Be("intra");
    }

    [Fact]
    public void Map_LaboratoryObservationResult_IsNull()
    {
        IntraMapper.Map(MinimalCertificate(), Context).LaboratoryObservationResult.Should().BeNull();
    }

    [Fact]
    public void Map_SpecifiedConsignment_IsNotNull()
    {
        var result = IntraMapper.Map(MinimalCertificate(), Context);

        result.SpecifiedConsignment.Should().NotBeNull();
    }

    [Fact]
    public void Map_ExchangedDocumentTypeCode_MapsFromSPSExchangedDocument()
    {
        IntraMapper.Map(MinimalCertificate(), Context).ExchangedDocument.DocumentTypeCode.Should().Be("856");
    }

    [Fact]
    public void ToDefraUNVTDINTRAProfile_ExtensionMethod_ProducesSameResult()
    {
        var cert = MinimalCertificate();

        IntraMapper.Map(cert, Context).Should().BeEquivalentTo(cert.ToDefraUNVTDINTRAProfile(Context));
    }

    [Fact]
    public async Task ToDefraUNVTDINTRAProfileSummary_Maps_All_Properties()
    {
        // Arrange
        var created = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updated = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        var source = new FindEuIntraCertificateResultType
        {
            offset = 10,
            pageSize = 2,
            EuIntraCertificateResult =
            [
                new EuIntraCertificateQueryResultType
                {
                    ID = "CERT123",
                    CreateDateTime = created,
                    UpdateDateTime = updated,
                    CountryOfOrigin = [new IDType() { Value = "GB" }],
                },
            ],
        };

        // Act
        var result = IntraMapper.Map(source);

        // Assert
        result.Should().NotBeNull();
        await Verify(result);
    }

    [Fact]
    public void ToDefraUNVTDINTRAProfileSummary_NullResults_ReturnsEmptyItems()
    {
        var source = new FindEuIntraCertificateResultType
        {
            offset = 0,
            pageSize = 10,
            EuIntraCertificateResult = null,
        };

        var result = IntraMapper.Map(source);

        result.Items.Should().NotBeNull().And.BeEmpty();
        result.HasMore.Should().BeFalse();
    }

    private static EuIntraCertificateType MinimalCertificate() =>
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
