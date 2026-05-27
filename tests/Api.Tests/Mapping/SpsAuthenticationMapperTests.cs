using Api.Mapping;
using AwesomeAssertions;
using TracesNT.WebServices;

namespace Api.Tests.Mapping;

public class SpsAuthenticationMapperTests
{
    private static readonly MappingContext Context = new("en");

    [Fact]
    public void Map_NullSource_ReturnsNull() =>
        SpsAuthenticationMapper.Map(null, Context).Should().BeNull();

    [Fact]
    public void Map_TypeCode_ExtractsXmlEnumCode()
    {
        var source = new SPSAuthenticationType
        {
            TypeCode = new GovernmentActionCodeType { Value = GovernmentActionCodeContentType.Item4 }
        };

        SpsAuthenticationMapper.Map(source, Context)!.TypeCode.Should().Be("4");
    }

    [Fact]
    public void Map_TypeCode_NameMapsToGovernmentActionTypeCode()
    {
        var source = new SPSAuthenticationType
        {
            TypeCode = new GovernmentActionCodeType
            {
                Value = GovernmentActionCodeContentType.Item4,
                name = "Inspection"
            }
        };

        SpsAuthenticationMapper.Map(source, Context)!.GovernmentActionTypeCode.Should().Be("Inspection");
    }

    [Fact]
    public void Map_NullClauses_ReturnsNullIncludedClause()
    {
        var result = SpsAuthenticationMapper.Map(new SPSAuthenticationType(), Context);

        result!.IncludedClause.Should().BeNull();
    }

    [Fact]
    public void Map_Clauses_MapToIncludedClause()
    {
        var source = new SPSAuthenticationType
        {
            IncludedSPSClause =
            [
                new SPSClauseType
                {
                    ID = new IDType { Value = "PURPOSE" },
                    Content = [new TextType { languageID = "en", Value = "For transit" }]
                }
            ]
        };

        var result = SpsAuthenticationMapper.Map(source, Context)!;

        result.IncludedClause.Should().ContainSingle()
            .Which.Identifier.Should().Be("PURPOSE");
    }

    [Fact]
    public void Map_ProviderParty_DelegatesToSpsPartyMapper()
    {
        var source = new SPSAuthenticationType
        {
            ProviderSPSParty = new SPSPartyType
            {
                ID = new IDType { Value = "PARTY-1" }
            }
        };

        SpsAuthenticationMapper.Map(source, Context)!.ProviderParty!.Identifier.Should().Be("PARTY-1");
    }
}
