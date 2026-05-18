using Api.Models;
using ProtoBuf.Serializers;
using System.Net;
using System.Net.Http.Headers;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class IntraEndpointsTests(TradeGatewayWebApplicationFactory factory) 
{
    [Fact]
    public async Task Get_NoAcceptHeader_DefaultsToV2()
    {
        factory
            .WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor("\"getEuIntraCertificate\"","/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text()]"))
            .RespondWith(
                Response.Create().WithCallback(async _ => await SoapUtilities.CreateResponseFromResource(HttpStatusCode.OK, "Api.Tests.Samples.INTRA.GetEuIntraCertificateResponse.xml"))
            );

        var client = factory.CreateClient();
        var response = await client.GetAsync("/intra/GB123", TestContext.Current.CancellationToken);

        Assert.Equal(MediaTypeAttribute.For<IntraCertificateV2>(), response.Content.Headers.ContentType?.MediaType);
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_V1AcceptHeader_ReturnsV1()
    {
        factory
            .WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor("\"getEuIntraCertificate\"", "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text()]"))
            .RespondWith(
                Response.Create().WithCallback(async _ => await SoapUtilities.CreateResponseFromResource(HttpStatusCode.OK, "Api.Tests.Samples.INTRA.GetEuIntraCertificateResponse.xml"))
            );

        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/intra/GB123");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeAttribute.For<IntraCertificate>()));

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(MediaTypeAttribute.For<IntraCertificate>(), response.Content.Headers.ContentType?.MediaType);
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_V2AcceptHeader_ReturnsV2()
    {
        factory
            .WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor("\"getEuIntraCertificate\"", "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text()]"))
            .RespondWith(
                Response.Create().WithCallback(async _ => await SoapUtilities.CreateResponseFromResource(HttpStatusCode.OK, "Api.Tests.Samples.INTRA.GetEuIntraCertificateResponse.xml"))
            );

        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/intra/GB123");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeAttribute.For<IntraCertificateV2>()));

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(MediaTypeAttribute.For<IntraCertificateV2>(), response.Content.Headers.ContentType?.MediaType);
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
