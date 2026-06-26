using Api.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net.Http.Headers;
using System.Text.Json;
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

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TracesNt:BaseUrl"] = tracesBaseUrl,
                ["TracesNt:Username"] = "test-user",
                ["TracesNt:AuthenticationKey"] = "test-auth-key",
                ["TracesNt:WebServiceClientId"] = "test-client-id",
                ["XApiKey"] = "test-x-api-key",
                // Authentication authorities come from appsettings.Development.json so that BindConfig
                // (which reads config before WebApplicationFactory overrides apply) sees the same values
                // as the OIDC token endpoints registered by LocalOidcServer at runtime.
            }));

        builder.ConfigureServices(services =>
            services.AddSingleton(server));

        builder.ConfigureTestServices(services =>
            services.PostConfigureAll<JwtBearerOptions>(opts =>
            {
                if (string.IsNullOrEmpty(opts.Authority)) return;
                var oidcConfig = new OpenIdConnectConfiguration { Issuer = opts.Authority };
                oidcConfig.SigningKeys.Add(LocalOidcServer.Key);
                opts.ConfigurationManager =
                    new StaticConfigurationManager<OpenIdConnectConfiguration>(oidcConfig);
            }));
    }

    private const string CognitoTokenEndpoint = "/local/cognito/token";
    private const string StsTokenEndpoint = "/local/sts/token";

    public Task<string> GetCognitoTokenAsync(string scope, string? sub = null) =>
        PostTokenAsync(CognitoTokenEndpoint, Fields(("scope", scope), ("sub", sub)));

    public Task<string> GetStsTokenAsync(string audience, string? sub = null) =>
        PostTokenAsync(StsTokenEndpoint, Fields(("audience", audience), ("sub", sub)));

    private static IEnumerable<KeyValuePair<string, string>> Fields(params (string Key, string? Value)[] fields) =>
        fields
            .Where(f => f.Value is not null)
            .Select(f => new KeyValuePair<string, string>(f.Key, f.Value!));

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

    /// <summary>Creates a client authenticated as a principal with the given <c>sub</c> claim.</summary>
    public async Task<HttpClient> CreateClientForPrincipalAsync(string sub) =>
        CreateClientWithToken(await GetCognitoTokenAsync(CognitoScope, sub));
}
