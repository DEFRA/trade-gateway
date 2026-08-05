using System.Net;
using System.Timers;
using Trade.Gateway.Api.Client.DelegatingHandlers;

namespace Api.Client.Tests;

public class TracingDelegatingHandlerTests
{
    [Fact]
    public async Task SendAsync_Adds_Trace_Header()
    {
        // Arrange
        const string traceId = "trace-123";
        HttpRequestMessage? capturedRequest = null;

        var innerHandler = new TestHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var handler = new TracingDelegatingHandler(() => traceId)
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(handler);

        // Act
        await client.GetAsync("https://example.com");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.Headers.Contains("x-cdp-request-id"));
        Assert.Equal(traceId, capturedRequest.Headers.GetValues("x-cdp-request-id").Single());
    }

    [Fact]
    public async Task SendAsync_Calls_TraceIdAccessor_For_Each_Request()
    {
        // Arrange
        var count = 0;

        var innerHandler = new TestHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var handler = new TracingDelegatingHandler(() =>
        {
            count++;
            return $"trace-{count}";
        })
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(handler);

        // Act
        await client.GetAsync("https://example.com");
        await client.GetAsync("https://example.com");

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task SendAsync_Adds_Different_TraceId_Per_Request()
    {
        // Arrange
        var traceIds = new List<string>();

        var innerHandler = new TestHttpMessageHandler(request =>
        {
            traceIds.Add(request.Headers.GetValues("x-cdp-request-id").Single());
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var counter = 0;
        var handler = new TracingDelegatingHandler(() => $"trace-{++counter}")
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(handler);

        // Act
        await client.GetAsync("https://example.com");
        await client.GetAsync("https://example.com");

        // Assert
        Assert.Equal(new[] { "trace-1", "trace-2" }, traceIds);
    }

       
}