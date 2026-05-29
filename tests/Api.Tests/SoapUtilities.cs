using System.Net;
using System.Reflection;
using WireMock;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.Types;
using WireMock.Util;

namespace Api.Tests
{
    public static class SoapUtilities
    {
        private static readonly Assembly s_assembly = Assembly.GetExecutingAssembly();

        public const string BodyXPath = "/*[local-name() = 'Envelope']/*[local-name() = 'Body']";

        public static Task<string> GetEmbeddedResource(string resourceName)
        {
            using var stream =
                s_assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException("Resource not found", resourceName);
            using var reader = new StreamReader(stream);
            return Task.FromResult(reader.ReadToEnd());
        }

        public static async Task<ResponseMessage> CreateResponseFromResource(
            HttpStatusCode statusCode,
            string resourceName
        )
        {
            var resourceContent = await GetEmbeddedResource(resourceName);
            return StubResponseMessage(statusCode, resourceContent);
        }

        public static ResponseMessage StubResponseMessage(HttpStatusCode statusCode, string? resourceContent)
        {
            return new ResponseMessage
            {
                StatusCode = statusCode,
                Headers = new Dictionary<string, WireMockList<string>>
                {
                    ["Content-Type"] = ["text/xml; charset=utf-8"],
                },
                BodyData = new BodyData { BodyAsString = resourceContent, DetectedBodyType = BodyType.String },
            };
        }

        public static IRequestBuilder CreateSoapRequestInterceptor(string soapAction, string bodyXpathSuffix)
        {
            return Request
                .Create()
                .WithHeader("SOAPAction", soapAction)
                .WithBody(new XPathMatcher(BodyXPath + bodyXpathSuffix))
                .UsingPost();
        }
    }
}
