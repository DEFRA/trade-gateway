using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Api.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Api.Tests.Endpoints;

public class AuthSchemesTests(AuthSchemesTests.Factory factory) : IClassFixture<AuthSchemesTests.Factory>
{
    private const string Scope = "trade-gateway-resource-srv/access";
    private const string CognitoTokenEndpoint = "/local/cognito/token";
    private const string StsTokenEndpoint = "/local/sts/token";

    [Fact]
    public async Task AuthTest_WithCognitoToken_Returns200()
    {
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync(CognitoTokenEndpoint, Scope));
        var response = await client.GetAsync("/auth-test", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthTest_WithStsToken_Returns200()
    {
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync(StsTokenEndpoint, Scope));
        var response = await client.GetAsync("/auth-test", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthTest_WithNoToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/auth-test", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }


    public class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TracesNt:BaseUrl"] = "http://localhost",
                    ["TracesNt:Username"] = "test-user",
                    ["TracesNt:AuthenticationKey"] = "test-auth-key",
                    ["TracesNt:WebServiceClientId"] = "test-client-id",
                    ["XApiKey"] = "test-api-key",
                })
            );

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

        public async Task<string> GetTokenAsync(string endpoint, string scope)
        {
            var response = await CreateClient().PostAsync(endpoint,
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
}
