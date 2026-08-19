using System.Diagnostics.Metrics;
using System.Net;
using WireMock.ResponseBuilders;

namespace Api.Tests.Metrics;

[Collection(IntegrationTestCollection.Name)]
public class ApiMetricsStatusTests(TradeGatewayWebApplicationFactory factory)
{
    [Fact]
    public async Task FaultedRequest_RecordsStatusCode200_NotTheActualErrorStatus()
    {
        var statusCodes = new List<object?>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "Defra.TradeGateway.Api" && instrument.Name == "RequestFaulted")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (var t in tags)
                if (t.Key == "StatusCode")
                    statusCodes.Add(t.Value);
        });
        listener.Start();

        // Make a request that fails and sends back a 500
        factory.WireMockServer.Reset();
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    "\"getClassificationSections\"",
                    "/*[local-name() = 'GetClassificationSectionsRequest']"
                )
            )
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(_ =>
                        SoapUtilities.StubResponseMessage(HttpStatusCode.InternalServerError, "blobby")
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");

        var response = await client.GetClassificationSections(TestContext.Current.CancellationToken);

        // Should get a 502 because "blobby" was received and that is not valid
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        // Check which status codes were emitted - we would want a 502 here
        Assert.Equal(200, Assert.Single(statusCodes));
    }
}