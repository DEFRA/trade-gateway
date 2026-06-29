using System.Net;
using Api.Contract;
using Trade.Gateway.Api.Contract.Certificate;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class IntraEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    private const string GetEuIntraCertificateSoapAction = "\"getEuIntraCertificate\"";
    private const string FindEuIntraCertificateSoapAction = "\"findEuIntraCertificate\"";

    [Fact]
    public async Task Get_ReturnsMappedDefraUNVTDINTRAProfile()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetEuIntraCertificateSoapAction,
                    "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text()]"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.INTRA.GetEuIntraCertificateResponse.xml"
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-intra-reader");
        var response = await client.GetAsync("/certificates/intras/GB123", TestContext.Current.CancellationToken);

        Assert.Equal(MediaTypeAttribute.For<DefraUNVTDINTRAProfile>(), response.Content.Headers.ContentType?.MediaType);
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_WhenTracesReturnsInvalidSoapFault_ReturnsInternalServerError()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetEuIntraCertificateSoapAction,
                    "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text() = 'BADSOAP']"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(_ =>
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

        var client = await factory.CreateClientForPrincipalAsync("test-intra-reader");
        var response = await client.GetAsync("/certificates/intras/BADSOAP", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_WhenTracesCommunicationFails_ReturnsBadGateway()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetEuIntraCertificateSoapAction,
                    "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text() = 'COMMFAIL']"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode((int)HttpStatusCode.BadGateway)
                    .WithHeader("Content-Type", "text/plain; charset=utf-8")
                    .WithBody("upstream failed")
            );

        var client = await factory.CreateClientForPrincipalAsync("test-intra-reader");
        var response = await client.GetAsync("/certificates/intras/COMMFAIL", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_WhenTracesReturnsNotFoundFault_ReturnsNotFound()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetEuIntraCertificateSoapAction,
                    "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text() = 'MISSING']"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(_ =>
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

        var client = await factory.CreateClientForPrincipalAsync("test-intra-reader");
        var response = await client.GetAsync("/certificates/intras/MISSING", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_WhenTracesReturnsPermissionDeniedFault_ReturnsForbidden()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetEuIntraCertificateSoapAction,
                    "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text() = 'FORBIDDEN']"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(_ =>
                        SoapUtilities.StubResponseMessage(
                            HttpStatusCode.InternalServerError,
                            """
                            <?xml version="1.0" encoding="utf-8"?>
                            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              <s:Body>
                                <s:Fault>
                                  <faultcode>s:Client</faultcode>
                                  <faultstring>Permission denied</faultstring>
                                  <detail>
                                    <EuIntraCertificatePermissionDeniedException xmlns="http://ec.europa.eu/tracesnt/certificate/euintra/v1">
                                      <CertificateIdentifier>FORBIDDEN</CertificateIdentifier>
                                    </EuIntraCertificatePermissionDeniedException>
                                  </detail>
                                </s:Fault>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-intra-reader");
        var response = await client.GetAsync("/certificates/intras/FORBIDDEN", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Find_WhenUpdatedFromIsMissing_ReturnsBadRequest()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-intra-reader");
        var response = await client.GetAsync(
            "/certificates/intras?pageSize=5&offset=5&updatedFrom1=2002-10-28Z&updatedBefore=2026-10-28Z",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Find_WhenValidRequest_AndNoAcceptHeader_ReturnsOk()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    FindEuIntraCertificateSoapAction,
                    "/*[local-name() = 'FindEuIntraCertificateRequest']"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.INTRA.FindEuIntraCertificateResponse.xml"
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-intra-reader");
        client.DefaultRequestHeaders.Add("Accept-Language", "en");
        var response = await client.GetAsync(
            "/certificates/intras?pageSize=10&offset=5&updatedFrom=2002-10-28Z&updatedBefore=2026-10-28Z",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            MediaTypeAttribute.For<DefraUNVTDINTRASummaryProfile>(),
            response.Content.Headers.ContentType?.MediaType
        );
        await VerifyJson(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
