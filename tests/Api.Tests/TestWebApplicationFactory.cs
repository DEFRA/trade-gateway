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

    public async Task<string> GetTokenAsync(string tokenEndpoint, string scope)
    {
        var response = await CreateClient().PostAsync(tokenEndpoint,
            new FormUrlEncodedContent([new("scope", scope)]));
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    public HttpClient CreateClientWithToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
