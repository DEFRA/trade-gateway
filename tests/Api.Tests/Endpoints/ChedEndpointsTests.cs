using System.Net;
using System.Net.Http.Json;
using Api.Contract;
using Refit;
using Trade.Gateway.Api.Contract.Certificate;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class ChedEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    private const string GetChedCertificateSoapAction = "\"getChedCertificate\"";
    private const string FindChedCertificateSoapAction = "\"findChedCertificate\"";
    private const string GetCertificateAttachmentSoapAction = "\"getCertificateAttachment\"";

    private static string AttachmentRequestXPath(string chedId) =>
        $"/*[local-name() = 'GetCertificateAttachmentRequest']/*[local-name() = 'ChedCertificateReference' and text() = '{chedId}']";

    private static string AttachmentRequestXPath(string chedId, long documentId, string fileName) =>
        $"/*[local-name() = 'GetCertificateAttachmentRequest'"
        + $" and *[local-name() = 'ChedCertificateReference' and text() = '{chedId}']"
        + $" and *[local-name() = 'DocumentId' and text() = '{documentId}']"
        + $" and *[local-name() = 'FileName' and text() = '{fileName}']]";

    private static string ChedRequestXPath(string chedId) =>
        $"/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() = '{chedId}']";

    private const string ChedSampleResource = "Api.Tests.Samples.CHED.GetChedResponse_CHEDA.XI.2026.0000063.xml";

    // The last element of the sample's exchanged-document ReferenceSPSReferencedDocument, which
    // AttachmentBinaryObject must follow to keep the schema's element order.
    private const string SampleSupportingDocumentId = """<ns4:ID schemeAgencyID="GB">455645665566</ns4:ID>""";

    private const string TradeLineItemEnd = "</ns4:IncludedSPSTradeLineItem>";

    private static string AttachmentBinaryObject(long documentId, string fileName) =>
        $"""<ns4:AttachmentBinaryObject uri="uri:documentid:{documentId}" filename="{fileName}" />""";

    /// <summary>
    /// Stubs getChedCertificate for <paramref name="chedId"/> with the sample CHED, adding the given
    /// AttachmentBinaryObject elements to the exchanged document and/or every trade line item.
    /// </summary>
    private void StubChedWithAttachments(
        string chedId,
        string exchangedDocumentAttachments = "",
        string tradeLineItemAttachments = ""
    )
    {
        var tradeLineItemDocument =
            tradeLineItemAttachments.Length == 0
                ? ""
                : $"<ns4:ReferenceSPSReferencedDocument>{tradeLineItemAttachments}</ns4:ReferenceSPSReferencedDocument>";

        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(GetChedCertificateSoapAction, ChedRequestXPath(chedId))
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            ChedSampleResource,
                            new TokenSubstitution
                            {
                                Token = SampleSupportingDocumentId,
                                Substitution = SampleSupportingDocumentId + exchangedDocumentAttachments,
                            },
                            new TokenSubstitution
                            {
                                Token = TradeLineItemEnd,
                                Substitution = tradeLineItemDocument + TradeLineItemEnd,
                            }
                        )
                    )
            );
    }

    [Fact]
    public async Task Get_ReturnsMappedDefraUNVTDCHEDProfile()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetChedCertificateSoapAction,
                    ChedRequestXPath("CHEDA.XI.2026.0000063")
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(HttpStatusCode.OK, ChedSampleResource)
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertification(
            "CHEDA.XI.2026.0000063",
            TestContext.Current.CancellationToken
        );

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
    public async Task GetAttachment_ReturnsTheFileWithItsContentTypeAndFileName()
    {
        const string chedId = "CHEDA.XI.2026.0001001";
        var fileBytes = "%PDF-1.4 attachment"u8.ToArray();

        StubChedWithAttachments(
            chedId,
            exchangedDocumentAttachments: AttachmentBinaryObject(1000, "other.pdf")
                + AttachmentBinaryObject(1001, "health-certificate.pdf")
        );
        StubAttachment(chedId, 1001, "health-certificate.pdf", fileBytes);

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertificationAttachment(chedId, 1001, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("health-certificate.pdf", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal(fileBytes, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAttachment_WhenAttachmentIsOnATradeLineItem_RequestsItByItsFileName()
    {
        const string chedId = "CHEDA.XI.2026.0001002";
        var fileBytes = "%PDF-1.4 line item attachment"u8.ToArray();

        StubChedWithAttachments(chedId, tradeLineItemAttachments: AttachmentBinaryObject(2002, "lab-report.pdf"));
        StubAttachment(chedId, 2002, "lab-report.pdf", fileBytes);

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertificationAttachment(chedId, 2002, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(fileBytes, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAttachment_WhenCertificateHasNoMatchingAttachment_ReturnsNotFound()
    {
        const string chedId = "CHEDA.XI.2026.0001003";

        StubChedWithAttachments(chedId, exchangedDocumentAttachments: AttachmentBinaryObject(3000, "other.pdf"));

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertificationAttachment(chedId, 3001, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal($"Ched certificate attachment '{chedId} - 3001' was not found.", problem?.Detail);
        Assert.DoesNotContain(
            factory.WireMockServer.LogEntries,
            entry =>
                entry.RequestMessage?.Body is { } body
                && body.Contains("GetCertificateAttachmentRequest")
                && body.Contains(chedId)
        );
    }

    [Fact]
    public async Task GetAttachment_WhenCertificateIsNotFound_ReturnsNotFound()
    {
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetChedCertificateSoapAction,
                    ChedRequestXPath("ATTACHMENT-CHED-MISSING")
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
                                      <CertificateIdentifier>ATTACHMENT-CHED-MISSING</CertificateIdentifier>
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
        var response = await client.GetChedCertificationAttachment(
            "ATTACHMENT-CHED-MISSING",
            1,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal("Ched certificate 'ATTACHMENT-CHED-MISSING' was not found.", problem?.Detail);
    }

    private void StubAttachment(string chedId, long documentId, string fileName, byte[] fileBytes) =>
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetCertificateAttachmentSoapAction,
                    AttachmentRequestXPath(chedId, documentId, fileName)
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(_ =>
                        SoapUtilities.StubResponseMessage(
                            HttpStatusCode.OK,
                            $"""
                            <?xml version="1.0" encoding="utf-8"?>
                            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              <s:Body>
                                <GetCertificateAttachmentResponse
                                    xmlns="http://ec.europa.eu/tracesnt/certificate/attachments/v1"
                                    xmlns:xmime="http://www.w3.org/2005/05/xmlmime"
                                    fileName="{fileName}"
                                    xmime:contentType="application/pdf">
                                  <Attachment>{Convert.ToBase64String(fileBytes)}</Attachment>
                                </GetCertificateAttachmentResponse>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

    [Fact]
    public async Task GetAttachment_WhenTracesReturnsInvalidSoapFault_ReturnsInternalServerError()
    {
        StubChedWithAttachments(
            "ATTACHMENT-BADSOAP",
            exchangedDocumentAttachments: AttachmentBinaryObject(1, "file.pdf")
        );

        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetCertificateAttachmentSoapAction,
                    AttachmentRequestXPath("ATTACHMENT-BADSOAP")
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
        var response = await client.GetChedCertificationAttachment(
            "ATTACHMENT-BADSOAP",
            1,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetAttachment_WhenTracesCommunicationFails_ReturnsBadGateway()
    {
        StubChedWithAttachments(
            "ATTACHMENT-COMMFAIL",
            exchangedDocumentAttachments: AttachmentBinaryObject(1, "file.pdf")
        );

        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetCertificateAttachmentSoapAction,
                    AttachmentRequestXPath("ATTACHMENT-COMMFAIL")
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
        var response = await client.GetChedCertificationAttachment(
            "ATTACHMENT-COMMFAIL",
            1,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetAttachment_WhenTracesReturnsNotFoundFault_ReturnsNotFound()
    {
        StubChedWithAttachments(
            "ATTACHMENT-MISSING",
            exchangedDocumentAttachments: AttachmentBinaryObject(42, "file.pdf")
        );

        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetCertificateAttachmentSoapAction,
                    AttachmentRequestXPath("ATTACHMENT-MISSING")
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
                                  <faultstring>Attachment not found</faultstring>
                                  <detail>
                                    <CertificateAttachmentNotFoundException xmlns="http://ec.europa.eu/tracesnt/certificate/attachments/v1" />
                                  </detail>
                                </s:Fault>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertificationAttachment(
            "ATTACHMENT-MISSING",
            42,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        await Verify(await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAttachment_WhenTracesReturnsPermissionDeniedFault_ReturnsForbidden()
    {
        StubChedWithAttachments(
            "ATTACHMENT-FORBIDDEN",
            exchangedDocumentAttachments: AttachmentBinaryObject(1, "file.pdf")
        );

        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    GetCertificateAttachmentSoapAction,
                    AttachmentRequestXPath("ATTACHMENT-FORBIDDEN")
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
                                    <CertificateAttachmentPermissionDeniedException xmlns="http://ec.europa.eu/tracesnt/certificate/attachments/v1" />
                                  </detail>
                                </s:Fault>
                              </s:Body>
                            </s:Envelope>
                            """
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-ched-reader");
        var response = await client.GetChedCertificationAttachment(
            "ATTACHMENT-FORBIDDEN",
            1,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
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
        var response = await client.FindChedUpdates(
            new DateTime(2002, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            10,
            0,
            TestContext.Current.CancellationToken
        );

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
        var response = await client.FindChedUpdates(
            new DateTime(1999, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc),
            10,
            0,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentHeaders?.ContentType?.MediaType);
    }
}
