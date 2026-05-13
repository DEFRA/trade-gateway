using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Api.Tests;

public class TradeGatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly Lock s_lock = new();
    
    public Action<IConfigurationBuilder> ConfigureHostConfiguration { get; set; } = _ => { };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // There is an issue with using CreateBootstrapLogger during host creation
        // that has started happening since the recent CDP Serilog changes have
        // been introduced. In tests, multiple hosts are created in parallel but
        // the CreateBootstrapLogger code is not thread safe and can throw errors.
        //
        // We can mitigate this issue from here by locking host creation so we
        // don't need to change host creation of the app itself.
        lock (s_lock)
        {
            builder.ConfigureHostConfiguration(config =>
            {
                ConfigureHostConfiguration(config);
            });

            return base.CreateHost(builder);
        }
    }
}