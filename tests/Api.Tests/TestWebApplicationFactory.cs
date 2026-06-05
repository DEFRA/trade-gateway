using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;

namespace Api.Tests;

public class TradeGatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    public WireMockServer WireMockServer => Services.GetRequiredService<WireMockServer>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var server = WireMockServer.Start();

        var tracesBaseUrl = $"http://localhost:{server.Port}";

        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["TracesNt:BaseUrl"] = tracesBaseUrl,
                        ["TracesNt:Username"] = "test-user",
                        ["TracesNt:AuthenticationKey"] = "test-auth-key",
                        ["TracesNt:WebServiceClientId"] = "test-client-id",
                        ["XApiKey"] = "test-x-api-key",
                        ["Authentication:Authority"] = "https://test.example.com",
                        ["Authentication:Scope"] = "test-resource-srv/access",
                    }
                );
            }
        );

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(server);

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }
}
