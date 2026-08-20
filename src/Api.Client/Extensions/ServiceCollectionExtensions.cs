using System.Diagnostics.CodeAnalysis;
using Amazon.SecurityToken;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trade.Gateway.Api.Client.Clients;
using Trade.Gateway.Api.Client.DelegatingHandlers;

namespace Trade.Gateway.Api.Client.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static TracesGatewayApiClientsBuilder AddTracesGatewayApiClients(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<TracesGatewayOptions>()
            .Bind(configuration.GetSection(TracesGatewayOptions.SectionName))
            .ValidateOnStart();

        services.ConfigureHttpClientDefaults(http =>
        {
            http.RedactLoggedHeaders(_ => false);
        });

        var builder = new TracesGatewayApiClientsBuilder(services);

        builder.AddClient<ITracesGatewayClient>();
        builder.AddClient<IReferenceDataClient>();
        builder.AddClient<ITracesGatewayChedClient>();
        builder.AddClient<ITracesGatewayIntraClient>();
        builder.AddClient<ITracesGatewayDocomClient>();

        return builder;
    }

    public static TracesGatewayApiClientsBuilder WithSts(this TracesGatewayApiClientsBuilder builder)
    {
        builder.Services.AddSingleton<IAmazonSecurityTokenService>(_ => new AmazonSecurityTokenServiceClient());

        builder.AddHandler<StsAuthDelegatingHandler>();

        return builder;
    }

    public static TracesGatewayApiClientsBuilder WithTracing(
        this TracesGatewayApiClientsBuilder builder,
        Func<IServiceProvider, string> traceIdAccessor
    )
    {
        builder.AddHandler<TracingDelegatingHandler>(sp => new TracingDelegatingHandler(traceIdAccessor, sp));

        return builder;
    }

    public static TracesGatewayApiClientsBuilder WithLogging(this TracesGatewayApiClientsBuilder builder)
    {
        builder.AddHandler<HttpLoggingDelegatingHandler>();

        return builder;
    }

    public static TracesGatewayApiClientsBuilder WithAcceptLanguage(this TracesGatewayApiClientsBuilder builder)
    {
        builder.AddHandler<AcceptLanguageDelegatingHandle>();

        return builder;
    }
}
