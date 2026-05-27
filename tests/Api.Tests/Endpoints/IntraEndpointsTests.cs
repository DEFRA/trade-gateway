using System.Net;
using Api.Contract;
using Trade.Gateway.Api.Contract;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class IntraEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    [Fact]
    public async Task Get_ReturnsMappedDefraUNVTDINTRAProfile()
    {
        factory
            .WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor("\"getEuIntraCertificate\"", "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text()]"))
            .RespondWith(
                Response.Create().WithCallback(async _ => await SoapUtilities.CreateResponseFromResource(HttpStatusCode.OK, "Api.Tests.Samples.INTRA.GetEuIntraCertificateResponse.xml"))
            );

        var client = factory.CreateClient();
        var response = await client.GetAsync("/intra/GB123", TestContext.Current.CancellationToken);

        Assert.Equal(MediaTypeAttribute.For<DefraUNVTDINTRAProfile>(), response.Content.Headers.ContentType?.MediaType);
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
