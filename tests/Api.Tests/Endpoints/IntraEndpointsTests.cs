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
    private const string GetEuIntraCertificateSoapAction = "\"getEuIntraCertificate\"";

    [Fact]
    public async Task Get_NoAcceptHeader_DefaultsToV2()
    {
        factory
            .WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor(GetEuIntraCertificateSoapAction, "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text()]"))
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
            .Given(SoapUtilities.CreateSoapRequestInterceptor(GetEuIntraCertificateSoapAction, "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text()]"))
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
            .Given(SoapUtilities.CreateSoapRequestInterceptor(GetEuIntraCertificateSoapAction, "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text()]"))
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

    [Fact]
    public async Task Get_WhenTracesReturnsInvalidSoapFault_ReturnsInternalServerError()
    {
        factory
            .WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor(
                GetEuIntraCertificateSoapAction,
                "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text() = 'BADSOAP']"
            ))
            .RespondWith(
                Response.Create().WithCallback(_ =>
                    SoapUtilities.StubResponseMessage(
                        HttpStatusCode.InternalServerError,
                        """
                        <?xml version="1.0" encoding="utf-8"?>
                        <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                          <s:Body>
                            <s:Fault>
                              <faultcode>s:Client</faultcode>
                              <faultstring>SAXException: invalid request</faultstring>
                            </s:Fault>
                          </s:Body>
                        </s:Envelope>
                        """
                    )
                )
            );

        var client = factory.CreateClient();
        var response = await client.GetAsync("/intra/BADSOAP", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenTracesCommunicationFails_ReturnsInternalServerError()
    {
        factory
            .WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor(
                GetEuIntraCertificateSoapAction,
                "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text() = 'COMMFAIL']"
            ))
            .RespondWith(
                Response.Create().WithStatusCode((int)HttpStatusCode.BadGateway)
                    .WithHeader("Content-Type", "text/plain; charset=utf-8")
                    .WithBody("upstream failed")
            );

        var client = factory.CreateClient();
        var response = await client.GetAsync("/intra/COMMFAIL", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenTracesReturnsNotFoundFault_ReturnsNotFound()
    {
        factory
            .WireMockServer
            .Given(SoapUtilities.CreateSoapRequestInterceptor(
                GetEuIntraCertificateSoapAction,
                "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text() = 'MISSING']"
            ))
            .RespondWith(
                Response.Create().WithCallback(_ =>
                    SoapUtilities.StubResponseMessage(
                        HttpStatusCode.InternalServerError,
                        """
                        <?xml version="1.0" encoding="utf-8"?>
                        <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                          <s:Body>
                            <s:Fault>
                              <faultcode>s:Client</faultcode>
                              <faultstring>Certificate not found</faultstring>
                              <detail>
                                <EuIntraCertificateNotFoundException xmlns="http://ec.europa.eu/tracesnt/certificate/euintra/v1">
                                  <CertificateIdentifier>MISSING</CertificateIdentifier>
                                </EuIntraCertificateNotFoundException>
                              </detail>
                            </s:Fault>
                          </s:Body>
                        </s:Envelope>
                        """
                    )
                )
            );

        var client = factory.CreateClient();
        var response = await client.GetAsync("/intra/MISSING", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
