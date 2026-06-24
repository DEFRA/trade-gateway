using System.Net;
using AwesomeAssertions;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class FineGrainedAuthorizationTests(TradeGatewayWebApplicationFactory factory)
{
    private const string IntraInstancePath = "/certificates/intra/AUTHZ1";
    private const string IntraCollectionPath = "/certificates/intra";
    private const string ReferenceDataPath = "/reference-data/classifications/sections";

    [Fact]
    public async Task IntraReader_can_read_intra()
    {
        StubIntraGet();
        var client = await factory.CreateClientForPrincipalAsync("test-intra-reader");

        var response = await client.GetAsync(IntraInstancePath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task IntraReader_cannot_read_reference_data()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-intra-reader");

        var response = await client.GetAsync(ReferenceDataPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReferenceDataReader_can_read_reference_data()
    {
        StubClassificationSections();
        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");

        var response = await client.GetAsync(ReferenceDataPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReferenceDataReader_cannot_read_intra()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");

        var response = await client.GetAsync(IntraInstancePath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InstanceReader_can_read_instance_but_not_collection()
    {
        StubIntraGet();
        var client = await factory.CreateClientForPrincipalAsync("test-intra-instance-reader");

        var instance = await client.GetAsync(IntraInstancePath, TestContext.Current.CancellationToken);
        instance.StatusCode.Should().Be(HttpStatusCode.OK);

        var collection = await client.GetAsync(IntraCollectionPath, TestContext.Current.CancellationToken);
        collection.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Authenticated_principal_with_no_config_entry_is_forbidden()
    {
        var client = await factory.CreateClientForPrincipalAsync("not-a-configured-principal");

        var response = await client.GetAsync(IntraInstancePath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task No_token_is_unauthorized()
    {
        var response = await factory.CreateClient().GetAsync(IntraInstancePath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_is_accessible_without_a_token()
    {
        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private void StubIntraGet() =>
        factory.WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor(
                "\"getEuIntraCertificate\"",
                "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text()]"))
            .RespondWith(Response.Create().WithCallback(async _ =>
                await SoapUtilities.CreateResponseFromResource(
                    HttpStatusCode.OK,
                    "Api.Tests.Samples.INTRA.GetEuIntraCertificateResponse.xml")));

    private void StubClassificationSections() =>
        factory.WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor(
                "\"getClassificationSections\"",
                "/*[local-name() = 'GetClassificationSectionsRequest']"))
            .RespondWith(Response.Create().WithCallback(async _ =>
                await SoapUtilities.CreateResponseFromResource(
                    HttpStatusCode.OK,
                    "Api.Tests.Samples.REFERENCE_DATA.GetClassificationSectionsResponse.xml")));
}
