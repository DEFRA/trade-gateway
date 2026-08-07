using Amazon.SecurityToken;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using Trade.Gateway.Api.Client.Clients;
using Trade.Gateway.Api.Client.DelegatingHandlers;

namespace Trade.Gateway.Api.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTracesGatewayApiClients(this IServiceCollection services, IConfiguration configuration,
        Func<IServiceProvider, string> traceIdAccessor)
    {
        services
            .AddOptions<TracesGatewayOptions>()
            .Bind(configuration.GetSection(TracesGatewayOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IAmazonSecurityTokenService>(_ => new AmazonSecurityTokenServiceClient());

        services.ConfigureHttpClientDefaults(http =>
        {
            http.RedactLoggedHeaders(_ => false);
        });

        services.AddRefitClient<ITracesGatewayClient>();
        services.AddRefitClient<IReferenceDataClient>();
        services.AddRefitClient<ITracesGatewayChedClient>();
        services.AddRefitClient<ITracesGatewayIntraClient>();

        services.AddTransient<StsAuthDelegatingHandler>();
        services.AddTransient<TracingDelegatingHandler>(sp => new TracingDelegatingHandler(traceIdAccessor, sp));
        services.AddTransient<AcceptLanguageDelegatingHandle>();
        services.AddTransient<HttpLoggingDelegatingHandler>();

        return services;
    }

    private static void AddRefitClient<TClient>(this IServiceCollection services) where TClient : class
    {
        services
            .AddRefitClient<TClient>(_ => new RefitSettings())
            .ConfigureHttpClient(
                (sp, c) =>
                {
                    var options = sp.GetRequiredService<IOptions<TracesGatewayOptions>>().Value;
                    c.BaseAddress = new Uri(options.BaseUrl);
                }
            )
            .AddHttpMessageHandler<StsAuthDelegatingHandler>()
            .AddHttpMessageHandler<HttpLoggingDelegatingHandler>()
            .AddHttpMessageHandler<TracingDelegatingHandler>()
            .AddHttpMessageHandler<AcceptLanguageDelegatingHandle>();
    }
}
