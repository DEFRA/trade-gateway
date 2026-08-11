using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Api.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Refit;
using System.Net.Http.Headers;
using System.Text.Json;
using Trade.Gateway.Api.Client.Clients;
using Trade.Gateway.Api.Client.Extensions;
using WireMock.Server;

namespace Api.Tests;

public class TradeGatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    public WireMockServer WireMockServer => Services.GetRequiredService<WireMockServer>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        var server = WireMockServer.Start();
        var tracesBaseUrl = $"http://localhost:{server.Port}";

        builder.ConfigureAppConfiguration(
            (_, config) =>
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["TracesNt:BaseUrl"] = tracesBaseUrl,
                        ["TracesNt:CustomsOfficeReferenceNumber"] = "GBTEST01",
                        ["TracesNt:Credentials:Default:Username"] = "test-user",
                        ["TracesNt:Credentials:Default:AuthenticationKey"] = "test-auth-key",
                        ["TracesNt:Credentials:Default:WebServiceClientId"] = "test-client-id",
                        // Deliberately different from the default set — TracesNtCredentialsTests
                        // asserts the customs port authenticates as this account, not the default one.
                        ["TracesNt:Credentials:Customs:Username"] = "test-customs-user",
                        ["TracesNt:Credentials:Customs:AuthenticationKey"] = "test-customs-auth-key",
                        ["TracesNt:Credentials:Customs:WebServiceClientId"] = "test-customs-client-id",
                        // Authentication authorities come from appsettings.Development.json so that BindConfig
                        // (which reads config before WebApplicationFactory overrides apply) sees the same values
                        // as the token endpoints registered by LocalTokenServer at runtime.
                    }
                )
        );

        builder.ConfigureServices((context, services) =>
        {
            services.AddSingleton(server);
            services.AddTracesGatewayApiClients(context.Configuration);
        });

        builder.ConfigureTestServices(services =>
            services.PostConfigureAll<JwtBearerOptions>(opts =>
            {
                if (string.IsNullOrEmpty(opts.Authority))
                    return;
                var oidcConfig = new OpenIdConnectConfiguration { Issuer = opts.Authority };
                oidcConfig.SigningKeys.Add(LocalTokenServer.Key);
                opts.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(oidcConfig);
            })
        );

        
    }

    private const string CognitoTokenEndpoint = "/local/cognito/token";
    private const string StsEndpoint = "http://localhost/local/sts";

    public Task<string> GetCognitoTokenAsync(string scope, string? sub = null) =>
        PostTokenAsync(CognitoTokenEndpoint, Fields(("scope", scope), ("sub", sub)));

    /// <summary>
    /// Mints an Sts-issued token the way the publisher does — through the real AWS SDK against
    /// the local <c>GetWebIdentityToken</c> stand-in — so the XML envelope stays parseable by the
    /// SDK's unmarshaller. The <c>sub</c> is fixed by the endpoint; real STS takes no such parameter.
    /// </summary>
    public async Task<GetWebIdentityTokenResponse> GetWebIdentityTokenAsync(string audience)
    {
        using var sts = new AmazonSecurityTokenServiceClient(
            new BasicAWSCredentials("test-access-key", "test-secret-key"),
            new AmazonSecurityTokenServiceConfig
            {
                ServiceURL = StsEndpoint,
                AuthenticationRegion = "eu-west-2",
                HttpClientFactory = new TestServerHttpClientFactory(this),
            }
        );

        return await sts.GetWebIdentityTokenAsync(
            new GetWebIdentityTokenRequest { Audience = [audience], DurationSeconds = 900 }
        );
    }

    public async Task<string> GetStsTokenAsync(string audience) =>
        (await GetWebIdentityTokenAsync(audience)).WebIdentityToken;

    /// <summary>Routes the AWS SDK's requests into the in-memory test server.</summary>
    private sealed class TestServerHttpClientFactory(TradeGatewayWebApplicationFactory factory) : HttpClientFactory
    {
        public override HttpClient CreateHttpClient(IClientConfig clientConfig) =>
            factory.CreateDefaultClient(new CanonicaliseUriHandler());

        // The test server's handler is per-factory; caching clients across configs would outlive it.
        public override bool UseSDKHttpClientCaching(IClientConfig clientConfig) => false;
    }

    /// <summary>
    /// The AWS SDK builds request URIs with <c>DangerousDisablePathAndQueryCanonicalization</c> so
    /// that SigV4 signs exactly the path it sends. TestServer's <c>ClientHandler</c> resolves the
    /// path via <c>PathString.FromUriComponent</c>, which throws on such URIs. Signing has already
    /// happened by the time the request reaches here, so rebuilding the URI is safe.
    /// </summary>
    private sealed class CanonicaliseUriHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (request.RequestUri is not null)
                request.RequestUri = new Uri(request.RequestUri.OriginalString);

            return base.SendAsync(request, cancellationToken);
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> Fields(params (string Key, string? Value)[] fields) =>
        fields.Where(f => f.Value is not null).Select(f => new KeyValuePair<string, string>(f.Key, f.Value!));

    private async Task<string> PostTokenAsync(string endpoint, IEnumerable<KeyValuePair<string, string>> fields)
    {
        var response = await CreateClient().PostAsync(endpoint, new FormUrlEncodedContent(fields));
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    public HttpClient CreateClientWithToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public const string CognitoScope = "trade-gateway-resource-srv/access";

    public async Task<ITracesGatewayClient> CreateClientForPrincipalAsync(string sub)
    {
        var client = CreateClient();
        
        var token = await GetCognitoTokenAsync(CognitoScope, sub);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return RestService.For<ITracesGatewayClient>(client, new RefitSettings(
            new SystemTextJsonContentSerializer(
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
                }
            )
        ));
    }

    public ITracesGatewayClient CreateITracesGatewayClient()
    {
        var client = CreateClient();
        return RestService.For<ITracesGatewayClient>(client, new RefitSettings(
            new SystemTextJsonContentSerializer(
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
                }
            )
        ));
    }
}
