using System.Net;
using TracesNT.ClientBehaviours;

namespace TracesNT.Tests.ClientBehaviours;

public class CustomHeaderDelegatingHandlerTests
{
    [Fact]
    public async Task SendAsync_WithApiKey_AddsHeader()
    {
        var innerHandler = new CapturingHandler();
        var sut = new CustomHeaderDelegatingHandler(innerHandler, "test-key");
        using var client = new HttpMessageInvoker(sut);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        innerHandler.Request.Should().NotBeNull();
        innerHandler.Request!.Headers.TryGetValues("x-api-key", out var values).Should().BeTrue();
        values.Should().ContainSingle().Which.Should().Be("test-key");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SendAsync_WithoutApiKey_DoesNotAddHeader(string? xApiKey)
    {
        var innerHandler = new CapturingHandler();
        var sut = new CustomHeaderDelegatingHandler(innerHandler, xApiKey);
        using var client = new HttpMessageInvoker(sut);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        innerHandler.Request.Should().NotBeNull();
        innerHandler.Request!.Headers.Contains("x-api-key").Should().BeFalse();
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
