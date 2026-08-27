using System.Diagnostics.Metrics;
using System.Net;
using WireMock.ResponseBuilders;

namespace Api.Tests.Metrics;

[Collection(IntegrationTestCollection.Name)]
public class ApiMetricsStatusTests(TradeGatewayWebApplicationFactory factory)
{
    [Fact]
    public async Task CompletedRequest_RecordsStatusCode()
    {
        var requestPaths = new List<object?>();
        var statusCodes = new List<object?>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "Defra.TradeGateway.Api" && instrument.Name == "RequestReceived")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>(
            (_, _, tags, _) =>
            {
                foreach (var t in tags)
                {
                    if (t.Key == "RequestPath")
                        requestPaths.Add(t.Value);

                    if (t.Key == "StatusCode")
                        statusCodes.Add(t.Value);
                }
            }
        );
        listener.Start();

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
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.REFERENCE_DATA.GetClassificationSectionsResponse.xml"
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");

        var response = await client.GetClassificationSections(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, statusCodes.Count); // Get Token call and Get Classification Sections call
        Assert.True(statusCodes.All(s => s is 200));
        Assert.True(requestPaths.Exists(r => r is "/reference-data/classifications/sections"));
    }

    [Fact]
    public async Task FaultedRequest_RecordsStatusCodeMappedByExceptionHandler()
    {
        var requestPaths = new List<object?>();
        var statusCodes = new List<object?>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "Defra.TradeGateway.Api" && instrument.Name == "RequestFaulted")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>(
            (_, _, tags, _) =>
            {
                foreach (var t in tags)
                {
                    if (t.Key == "RequestPath")
                        requestPaths.Add(t.Value);

                    if (t.Key == "StatusCode")
                        statusCodes.Add(t.Value);
                }
            }
        );
        listener.Start();

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
                    .WithCallback(_ => SoapUtilities.StubResponseMessage(HttpStatusCode.InternalServerError, "blobby"))
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");

        var response = await client.GetClassificationSections(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(502, Assert.Single(statusCodes));
        Assert.True(requestPaths.Exists(r => r is "/reference-data/classifications/sections"));
    }
}
