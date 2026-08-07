using Api.Contract;
using Refit;
using System.Net;
using Trade.Gateway.Api.Contract.Certificate;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class ChedEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    private const string GetChedCertificateSoapAction = "\"getChedCertificate\"";
    private const string FindChedCertificateSoapAction = "\"findChedCertificate\"";

    [Fact]
    public async Task Get_ReturnsMappedDefraUNVTDCHEDProfile()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetChedCertificateSoapAction,
                    "/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text()]"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.CHED.GetChedResponse_CHEDA.XI.2026.0000063.xml"
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertification("CHEDA.XI.2026.0000063", TestContext.Current.CancellationToken);

        Assert.Equal(MediaTypeAttribute.For<DefraUNVTDCHEDProfile>(), response.ContentHeaders?.ContentType?.MediaType);
        await Verify(response.Content);
    }

    [Fact]
    public async Task Get_WhenTracesReturnsInvalidSoapFault_ReturnsInternalServerError()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetChedCertificateSoapAction,
                    "/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() = 'BADSOAP']"
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

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertification("BADSOAP", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_WhenTracesCommunicationFails_ReturnsBadGateway()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetChedCertificateSoapAction,
                    "/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() = 'COMMFAIL']"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode((int)HttpStatusCode.BadGateway)
                    .WithHeader("Content-Type", "text/plain; charset=utf-8")
                    .WithBody("upstream failed")
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertification("COMMFAIL", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_WhenTracesReturnsNotFoundFault_ReturnsNotFound()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetChedCertificateSoapAction,
                    "/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() = 'MISSING']"
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
                                    <ChedCertificateNotFoundException xmlns="http://ec.europa.eu/tracesnt/certificate/ched/v2">
                                      <CertificateIdentifier>MISSING</CertificateIdentifier>
                                    </ChedCertificateNotFoundException>
                                  </detail>
                                </s:Fault>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertification("MISSING", TestContext.Current.CancellationToken);

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
                    GetChedCertificateSoapAction,
                    "/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() = 'FORBIDDEN']"
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
                                    <ChedCertificatePermissionDeniedException xmlns="http://ec.europa.eu/tracesnt/certificate/ched/v2">
                                      <CertificateIdentifier>FORBIDDEN</CertificateIdentifier>
                                    </ChedCertificatePermissionDeniedException>
                                  </detail>
                                </s:Fault>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertification("FORBIDDEN", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
    }

    [Fact]
    public async Task Find_WhenOffsetIsLessThanZero_ReturnsBadRequest()
    {
        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");

        var response = await client.FindChedUpdates(
            new DateTimeOffset(2002, 10, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            10,
            -1,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
        await Verify((response.Error as ValidationApiException)?.Content);
    }

    [Fact]
    public async Task Find_WhenValidRequest_AndNoOptionalParameters_ReturnsOk()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    FindChedCertificateSoapAction,
                    "/*[local-name() = 'FindChedCertificateRequest']"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.CHED.FindChedCertificateResponse.xml"
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.FindChedUpdates(new DateTime(2002, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            10,
            0,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            MediaTypeAttribute.For<DefraUNVTDCHEDSummaryProfile>(),
            response.ContentHeaders?.ContentType?.MediaType
        );
        await Verify(response.Content);
    }

    [Fact]
    public async Task Find_WhenTracesCommunicationFails_ReturnsBadGateway()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    FindChedCertificateSoapAction,
                    "/*[local-name() = 'FindChedCertificateRequest']"
                        + "/*[local-name() = 'UpdateDateTimeRange']"
                        + "/*[local-name() = 'From' and contains(text(), '1999')]"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode((int)HttpStatusCode.BadGateway)
                    .WithHeader("Content-Type", "text/plain; charset=utf-8")
                    .WithBody("upstream failed")
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.FindChedUpdates(new DateTime(1999, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            10,
            0,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
    }
}
