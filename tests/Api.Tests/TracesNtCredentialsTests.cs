using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Api.Utils;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TracesNT;
using TracesNT.WebServices;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Api.Tests;

/// <summary>
/// The customs port authenticates as a different TracesNT account from every other port. Nothing in
/// the response surface reveals which account a call was made as, so these tests drive the port
/// clients straight out of DI and read the outbound WS-Security header off the WireMock request log.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class TracesNtCredentialsTests(TradeGatewayWebApplicationFactory factory)
{
    private const string CustomsPath = "/CustomsCertexChedServiceV06";
    private const string ChedPath = "/ChedCertificateServiceV2";

    private static readonly XNamespace s_wsse =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private static readonly XNamespace s_wsu =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

    [Fact]
    public async Task CustomsClient_AuthenticatesAsTheCustomsAccount()
    {
        var token = await CaptureUsernameToken<CustomsCertexChedPortClient>(
            CustomsPath,
            client =>
                client.processedChedRequestAsync(
                    new SecurityHeaderType(),
                    "irrelevant",
                    ISO2AlphaLanguageCodeContentType.en,
                    "GBTEST01",
                    new CertexHeaderType(),
                    new ProcessedChedRequestType()
                )
        );

        token.Username.Should().Be("test-customs-user");
        token.DigestMatchesKey("test-customs-auth-key").Should().BeTrue();
        token.DigestMatchesKey("test-auth-key").Should().BeFalse("the customs port must not use the default key");
    }

    [Fact]
    public async Task ChedClient_StillAuthenticatesAsTheDefaultAccount()
    {
        var token = await CaptureUsernameToken<ChedCertificatePortClient>(
            ChedPath,
            client =>
                client.getChedCertificateAsync(
                    new SecurityHeaderType(),
                    "irrelevant",
                    ISO2AlphaLanguageCodeContentType.en,
                    [],
                    new GetChedCertificateRequestType { ID = "CHEDA.GB.2026.0000001" }
                )
        );

        token.Username.Should().Be("test-user");
        token.DigestMatchesKey("test-auth-key").Should().BeTrue();
    }

    [Fact]
    public void EachCredentialKeyBindsItsOwnSection()
    {
        var credentials = factory.Services.GetRequiredService<IOptionsMonitor<TracesNtCredentials>>();

        credentials.Get(TracesNtCredentialKeys.Default).WebServiceClientId.Should().Be("test-client-id");
        credentials.Get(TracesNtCredentialKeys.Customs).WebServiceClientId.Should().Be("test-customs-client-id");
    }

    [Fact]
    public void MissingCredentials_FailStartupValidation()
    {
        // Exercises the real registration used by Program.cs. A second WebApplicationFactory cannot
        // be started in-process — Program.cs registers the MONGODB-AWS SASL mechanism globally — so
        // the startup validator is resolved from a bare container instead.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Credentials:Default:Username"] = "someone",
                    ["Credentials:Default:AuthenticationKey"] = "a-key",
                    ["Credentials:Default:WebServiceClientId"] = "a-client-id",
                    // Customs omitted entirely.
                }
            )
            .Build();

        var provider = new ServiceCollection().AddTracesNtCredentials(configuration).BuildServiceProvider();

        var act = () => provider.GetRequiredService<IStartupValidator>().Validate();

        act.Should().Throw<OptionsValidationException>().Which.OptionsName.Should().Be(TracesNtCredentialKeys.Customs);
    }

    private async Task<UsernameToken> CaptureUsernameToken<TClient>(string servicePath, Func<TClient, Task> send)
        where TClient : notnull
    {
        factory
            .WireMockServer.Given(Request.Create().WithPath(servicePath).UsingPost())
            .RespondWith(
                Response
                    .Create()
                    .WithCallback(_ => SoapUtilities.StubResponseMessage(HttpStatusCode.InternalServerError, ""))
            );

        // The shared WireMock server carries other tests' traffic, so only entries logged from here on.
        var loggedBefore = factory.WireMockServer.LogEntries.Count();

        using var scope = factory.Services.CreateScope();
        try
        {
            await send(scope.ServiceProvider.GetRequiredService<TClient>());
        }
        catch (Exception)
        {
            // Expected: the stub returns no parseable SOAP envelope. Only the request is under test.
        }

        var body = factory
            .WireMockServer.LogEntries.Skip(loggedBefore)
            .Where(entry => entry.RequestMessage!.Path == servicePath)
            .Select(entry => entry.RequestMessage!.Body)
            .LastOrDefault();

        body.Should().NotBeNullOrEmpty("the client should have sent a request to {0}", servicePath);

        var token = XDocument.Parse(body!).Descendants(s_wsse + "UsernameToken").SingleOrDefault();
        token.Should().NotBeNull("the WS-Security header is omitted entirely when credentials are blank");
        var usernameToken = token!;

        return new UsernameToken(
            usernameToken.Element(s_wsse + "Username")!.Value,
            usernameToken.Element(s_wsse + "Nonce")!.Value,
            usernameToken.Element(s_wsu + "Created")!.Value,
            usernameToken.Element(s_wsse + "Password")!.Value
        );
    }

    private sealed record UsernameToken(string Username, string Nonce, string Created, string PasswordDigest)
    {
        /// <summary>
        /// Recomputes the PasswordDigest per the WS-Security UsernameToken profile. Asserting on the
        /// username alone would not prove the matching authentication key was used.
        /// </summary>
        public bool DigestMatchesKey(string authenticationKey)
        {
            byte[] combined =
            [
                .. Convert.FromBase64String(Nonce),
                .. Encoding.UTF8.GetBytes(Created),
                .. Encoding.UTF8.GetBytes(authenticationKey),
            ];

            return Convert.ToBase64String(SHA1.HashData(combined)) == PasswordDigest; //NOSONAR - matches the digest the WS-Security spec mandates
        }
    }
}
