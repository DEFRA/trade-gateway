using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Api.Tests;

public class TradeGatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TracesNt:BaseUrl"] = "https://example.test",
                ["TracesNt:Username"] = "test-user",
                ["TracesNt:AuthenticationKey"] = "test-auth-key",
                ["TracesNt:WebServiceClientId"] = "test-client-id",
                ["XApiKey"] = "test-x-api-key"
            });
        });
    }
}
