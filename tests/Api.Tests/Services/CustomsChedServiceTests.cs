using System.Net;
using System.Xml.Linq;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using TracesNT.Exceptions;
using TracesNT.Services;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Api.Tests.Services;

/// <summary>
/// The read endpoints are only read-only because of what this service puts on the wire, and nothing
/// in a successful response proves it. These tests drive the service out of DI and assert on the
/// outbound SOAP body captured by WireMock.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class CustomsChedServiceTests(TradeGatewayWebApplicationFactory factory)
{
    private const string CustomsPath = "/CustomsCertexChedServiceV06";

    /// <summary>
    /// Distinct from every CHED id the endpoint tests stub — the WireMock server is shared across
    /// the collection, and their SOAPAction+XPath matchers outscore the path-only stub used here.
    /// </summary>
    private const string ChedId = "CHEDA.GB.2026.0009999";

    /// <summary>Matches <c>TracesNt:CustomsOfficeReferenceNumber</c> in the test factory.</summary>
    private const string CustomsOffice = "GBTEST01";

    private static readonly XNamespace s_customsChedNs = "http://ec.europa.eu/sanco/tracesnt/customs_certex/ched/v06";
    private static readonly XNamespace s_customsBaseNs = "http://ec.europa.eu/sanco/tracesnt/customs_certex/base/v03";
    private static readonly XNamespace s_tracesBody = "http://ec.europa.eu/tracesnt/body/v1";

    [Fact]
    public async Task GetChedQuantitySummary_SendsQuantityManagementIndicationZero()
    {
        var request = await CaptureRequest();

        // "1" reserves quantities against a customs declaration. A read must never send it.
        request
            .Descendants(s_customsChedNs + "QuantityManagementIndication")
            .Single()
            .Value.Should()
            .Be("0", "a read must never reserve quantities");
    }

    [Fact]
    public async Task GetChedQuantitySummary_DoesNotRequestPdfOrPush()
    {
        var request = await CaptureRequest();

        request
            .Descendants(s_customsChedNs + "PdfGenerationIndication")
            .Should()
            .BeEmpty("every read would otherwise ask TracesNT to render a PDF");
        request
            .Descendants(s_customsChedNs + "PushIndication")
            .Should()
            .BeEmpty("the gateway does not subscribe to CHED updates");
    }

    [Fact]
    public async Task GetChedQuantitySummary_SendsTheConfiguredCustomsOfficeInAllThreePlaces()
    {
        var request = await CaptureRequest();

        request.Descendants(s_tracesBody + "CustomsOfficeReferenceNumber").Single().Value.Should().Be(CustomsOffice);
        request.Descendants(s_customsBaseNs + "UniqRequesterPrefix").Single().Value.Should().Be(CustomsOffice);
        request.Descendants(s_customsChedNs + "CompetentCustomsOffice").Single().Value.Should().Be(CustomsOffice);
    }

    [Fact]
    public async Task GetChedQuantitySummary_SendsTheRequestedChed()
    {
        var request = await CaptureRequest();

        request.Descendants(s_customsChedNs + "ChedCertificateId").Single().Value.Should().Be(ChedId);
    }

    /// <summary>
    /// The upstream schema marks <c>CustomsDeclarationReferenceNumber</c> optional, but TracesNT
    /// rejects a request that omits it, so the element is always sent — empty.
    /// </summary>
    [Fact]
    public async Task GetChedQuantitySummary_SendsAnEmptyCustomsDeclarationReferenceNumber()
    {
        var request = await CaptureRequest();

        var declaration = request
            .Descendants(s_customsChedNs + "CustomsDeclarationReferenceNumber")
            .Should()
            .ContainSingle("TracesNT rejects a request without the element, schema notwithstanding")
            .Subject;

        // Populating it would narrow the response to one declaration, and the ledger is defined as
        // the whole CHED's position. It is also the field that names the declaration a QMI=1 write
        // reserves against, so it must never acquire a value on a path that does not intend one.
        declaration
            .Descendants()
            .Should()
            .BeEmpty("the element is a protocol requirement, not a declaration filter");
    }

    [Fact]
    public async Task GetChedQuantitySummary_SendsAFreshMessageIdEachCall()
    {
        var first = MessageIdOf(await CaptureRequest());
        var second = MessageIdOf(await CaptureRequest());

        first.Should().NotBe(second);
        // xs:token, capped at 48 characters by the upstream schema.
        first.Should().HaveLength(32).And.MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public async Task GetChedQuantitySummary_WhenUpstreamFaults_ThrowsCustomsFaultExceptionCarryingTheUpstreamError()
    {
        StubResponse(HttpStatusCode.InternalServerError, CustomsFaultResponse);

        var act = () => InvokeAsync();

        var exception = (await act.Should().ThrowAsync<CustomsFaultException>()).Which;
        exception.UpstreamError.Should().Be(UpstreamErrorText);
        exception.MessageId.Should().Be("upstream-message-id");
        // The 502 detail is fixed text in GlobalExceptionHandler; the raw error only ever reaches logs.
        exception.Message.Should().NotContain(UpstreamErrorText);
    }

    private const string UpstreamErrorText = "CHED 12345 is locked by internal process PID-9981";

    private static readonly string CustomsFaultResponse = $"""
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            <soap:Fault>
              <faultcode>soap:Server</faultcode>
              <faultstring>Quantity management failed</faultstring>
              <detail>
                <ProcessedChedRequestFault xmlns="http://ec.europa.eu/sanco/tracesnt/customs_certex/ched/v06">
                  <MessageId xmlns="http://ec.europa.eu/sanco/tracesnt/customs_certex/base/v03">upstream-message-id</MessageId>
                  <UniqPrefix xmlns="http://ec.europa.eu/sanco/tracesnt/customs_certex/base/v03">{CustomsOffice}</UniqPrefix>
                  <errorMessage xmlns="http://ec.europa.eu/sanco/tracesnt/customs_certex/base/v03">{UpstreamErrorText}</errorMessage>
                </ProcessedChedRequestFault>
              </detail>
            </soap:Fault>
          </soap:Body>
        </soap:Envelope>
        """;

    private static string MessageIdOf(XDocument request) =>
        request.Descendants(s_customsBaseNs + "MessageId").Single().Value;

    private void StubResponse(HttpStatusCode statusCode, string body) =>
        factory
            .WireMockServer.Given(Request.Create().WithPath(CustomsPath).UsingPost())
            .RespondWith(Response.Create().WithCallback(_ => SoapUtilities.StubResponseMessage(statusCode, body)));

    private async Task InvokeAsync()
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ICustomsChedService>().GetChedQuantitySummary(ChedId, "en");
    }

    /// <summary>
    /// Sends one request and returns what went over the wire. The stub deliberately returns nothing
    /// parseable — only the request is under test, and the response samples come from acceptance.
    /// </summary>
    private async Task<XDocument> CaptureRequest()
    {
        StubResponse(HttpStatusCode.InternalServerError, "");

        // The WireMock server is shared across the collection, so ignore anything logged earlier.
        var loggedBefore = factory.WireMockServer.LogEntries.Count();

        try
        {
            await InvokeAsync();
        }
        catch (TracesCommunicationException)
        {
            // Expected: the stub returns no parseable SOAP envelope.
        }

        var body = factory
            .WireMockServer.LogEntries.Skip(loggedBefore)
            .Where(entry => entry.RequestMessage!.Path == CustomsPath)
            .Select(entry => entry.RequestMessage!.Body)
            .LastOrDefault();

        body.Should().NotBeNullOrEmpty("the service should have called {0}", CustomsPath);

        return XDocument.Parse(body!);
    }
}
