using System.Net;
using Refit;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class DocomEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    private const string GetDocomCertificateSoapAction = "\"getDocomCertificate\"";

    [Fact]
    public async Task Get_ReturnsMappedDefraUNVTDDOCOMProfile()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetDocomCertificateSoapAction,
                    "/*[local-name() = 'GetDocomCertificateRequest']/*[local-name() = 'ID' and text()]"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.DOCOM.GetDocomCertificateResponse.xml"
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-docom-reader");
        var response = await client.GetDocomCertification("GB123", TestContext.Current.CancellationToken);
        await Verify(response.Content);
    }

    [Fact]
    public async Task Get_WhenTracesReturnsInvalidSoapFault_ReturnsInternalServerError()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetDocomCertificateSoapAction,
                    "/*[local-name() = 'GetDocomCertificateRequest']/*[local-name() = 'ID' and text() = 'BADSOAP']"
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

        var client = await factory.CreateClientForPrincipalAsync("test-docom-reader");
        var response = await client.GetDocomCertification("BADSOAP", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_WhenTracesCommunicationFails_ReturnsBadGateway()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetDocomCertificateSoapAction,
                    "/*[local-name() = 'GetDocomCertificateRequest']/*[local-name() = 'ID' and text() = 'COMMFAIL']"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode((int)HttpStatusCode.BadGateway)
                    .WithHeader("Content-Type", "text/plain; charset=utf-8")
                    .WithBody("upstream failed")
            );

        var client = await factory.CreateClientForPrincipalAsync("test-docom-reader");
        var response = await client.GetDocomCertification("COMMFAIL", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_WhenTracesReturnsNotFoundFault_ReturnsNotFound()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetDocomCertificateSoapAction,
                    "/*[local-name() = 'GetDocomCertificateRequest']/*[local-name() = 'ID' and text() = 'MISSING']"
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
                                    <DocomCertificateNotFoundException xmlns="http://ec.europa.eu/tracesnt/certificate/docom/retrieval/v1">
                                      <CertificateIdentifier>MISSING</CertificateIdentifier>
                                    </DocomCertificateNotFoundException>
                                  </detail>
                                </s:Fault>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-docom-reader");
        var response = await client.GetDocomCertification("MISSING", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
        await Verify((response.Error as ValidationApiException)?.Content);
    }

    [Fact]
    public async Task Get_WhenTracesReturnsPermissionDeniedFault_ReturnsForbidden()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetDocomCertificateSoapAction,
                    "/*[local-name() = 'GetDocomCertificateRequest']/*[local-name() = 'ID' and text() = 'FORBIDDEN']"
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
                                    <DocomCertificatePermissionDeniedException xmlns="http://ec.europa.eu/tracesnt/certificate/docom/retrieval/v1">
                                      <CertificateIdentifier>FORBIDDEN</CertificateIdentifier>
                                    </DocomCertificatePermissionDeniedException>
                                  </detail>
                                </s:Fault>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-docom-reader");
        var response = await client.GetDocomCertification("FORBIDDEN", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
    }
}
