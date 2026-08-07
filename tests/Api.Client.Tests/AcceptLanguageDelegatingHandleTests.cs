using System.Net;
using System.Net.Http.Headers;
using Trade.Gateway.Api.Client.DelegatingHandlers;

namespace Api.Client.Tests
{
    public class AcceptLanguageDelegatingHandleTests
    {
        [Fact]
        public async Task SendAsync_Adds_En_AcceptLanguage_Header()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;

            var innerHandler = new TestHttpMessageHandler(request =>
            {
                capturedRequest = request;

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            var handler = new AcceptLanguageDelegatingHandle
            {
                InnerHandler = innerHandler
            };

            var client = new HttpClient(handler);

            // Act
            await client.GetAsync("https://example.com");

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Single(capturedRequest!.Headers.AcceptLanguage);
            Assert.Equal("en", capturedRequest.Headers.AcceptLanguage.Single().Value);
        }

        [Fact]
        public async Task SendAsync_Replaces_Existing_AcceptLanguage_Header()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;

            var innerHandler = new TestHttpMessageHandler(request =>
            {
                capturedRequest = request;

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            var handler = new AcceptLanguageDelegatingHandle
            {
                InnerHandler = innerHandler
            };

            var invoker = new HttpMessageInvoker(handler);

            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("fr"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("de"));

            // Act
            await invoker.SendAsync(request, CancellationToken.None);

            // Assert
            Assert.Single(capturedRequest!.Headers.AcceptLanguage);
            Assert.Equal("en", capturedRequest.Headers.AcceptLanguage.Single().Value);
        }

       
    }
}
