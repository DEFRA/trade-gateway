using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

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
