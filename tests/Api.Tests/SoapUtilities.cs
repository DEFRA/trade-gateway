using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using WireMock;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.Types;
using WireMock.Util;

namespace Api.Tests
{
    public static partial class SoapUtilities
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
            string resourceName,
            params TokenSubstitution[] resourceTokenSubstitutions
        )
        {
            var resourceContent = await GetEmbeddedResource(resourceName);

            if (resourceContent == null)
            {
                throw new FileNotFoundException("Resource not found", resourceName);
            }

            foreach (var substitution in resourceTokenSubstitutions)
            {
                resourceContent = resourceContent.Replace(substitution.Token, substitution.Substitution);
            }

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

        /// <summary>
        /// Matches a SOAP 1.2 request sent with MTOM encoding: the action travels in the Content-Type
        /// header rather than a SOAPAction header, and the envelope is the root part of a multipart body.
        /// </summary>
        public static IRequestBuilder CreateSoap12MtomRequestInterceptor(string action, string bodyXpathSuffix)
        {
            return Request
                .Create()
                .WithHeader("Content-Type", $"*multipart/related*action=*{action}*")
                .WithBody((string? body) => SoapEnvelopeMatches(body, BodyXPath + bodyXpathSuffix))
                .UsingPost();
        }

        /// <summary>
        /// Returns the SOAP envelope from a request body, unwrapping it from its MTOM multipart package if needed.
        /// </summary>
        public static string ExtractSoapEnvelope(string body)
        {
            var match = SoapEnvelopeRegex().Match(body);
            return match.Success ? match.Value : body;
        }

        public static ResponseMessage StubSoap12ResponseMessage(HttpStatusCode statusCode, string resourceContent)
        {
            return new ResponseMessage
            {
                StatusCode = statusCode,
                Headers = new Dictionary<string, WireMockList<string>>
                {
                    ["Content-Type"] = ["application/soap+xml; charset=utf-8"],
                },
                BodyData = new BodyData
                {
                    BodyAsString = resourceContent.Trim(),
                    DetectedBodyType = BodyType.String,
                },
            };
        }

        /// <summary>
        /// Builds an MTOM response: the SOAP 1.2 envelope as the root part, with <paramref name="attachment"/>
        /// as a binary part referenced from the envelope via <c>cid:</c><paramref name="attachmentContentId"/>.
        /// </summary>
        public static ResponseMessage StubSoap12MtomResponseMessage(
            HttpStatusCode statusCode,
            string envelope,
            string attachmentContentId,
            byte[] attachment
        )
        {
            const string boundary = "uuid:mtom-boundary";
            const string rootContentId = "<root.message@cxf.apache.org>";

            using var body = new MemoryStream();
            void Write(string text) => body.Write(Encoding.UTF8.GetBytes(text));

            Write($"--{boundary}\r\n");
            Write("Content-Type: application/xop+xml; charset=UTF-8; type=\"application/soap+xml\"\r\n");
            Write("Content-Transfer-Encoding: binary\r\n");
            Write($"Content-ID: {rootContentId}\r\n\r\n");
            Write(envelope.Trim());
            Write($"\r\n--{boundary}\r\n");
            Write("Content-Type: application/octet-stream\r\n");
            Write("Content-Transfer-Encoding: binary\r\n");
            Write($"Content-ID: <{attachmentContentId}>\r\n\r\n");
            body.Write(attachment);
            Write($"\r\n--{boundary}--\r\n");

            return new ResponseMessage
            {
                StatusCode = statusCode,
                Headers = new Dictionary<string, WireMockList<string>>
                {
                    ["Content-Type"] =
                    [
                        $"multipart/related; type=\"application/xop+xml\"; boundary=\"{boundary}\"; "
                            + $"start=\"{rootContentId}\"; start-info=\"application/soap+xml\"",
                    ],
                },
                BodyData = new BodyData { BodyAsBytes = body.ToArray(), DetectedBodyType = BodyType.Bytes },
            };
        }

        private static bool SoapEnvelopeMatches(string? body, string xpath)
        {
            if (string.IsNullOrEmpty(body))
            {
                return false;
            }

            try
            {
                return XDocument.Parse(ExtractSoapEnvelope(body)).XPathSelectElement(xpath) is not null;
            }
            catch (System.Xml.XmlException)
            {
                return false;
            }
        }

        [GeneratedRegex(@"<(\w+:)?Envelope[\s\S]*</(\w+:)?Envelope>")]
        private static partial Regex SoapEnvelopeRegex();
    }
}
