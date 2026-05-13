using FluentValidation;
using System.Diagnostics.CodeAnalysis;
using Api.Config;
using MongoDB.Driver;
using MongoDB.Driver.Authentication.AWS;
using Serilog;
using Api.Endpoints;
using Api.Utils;
using Api.Utils.Http;
using Api.Utils.Logging;
using Api.Utils.Mongo;

var app = CreateWebApplication(args);
await app.RunAsync();
return;

[ExcludeFromCodeCoverage]
static WebApplication CreateWebApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    ConfigureBuilder(builder);

    var app = builder.Build();
    return SetupApplication(app);
}

[ExcludeFromCodeCoverage]
static void ConfigureBuilder(WebApplicationBuilder builder)
{
    builder.Configuration.AddEnvironmentVariables();

    // Load certificates into Trust Store - Note must happen before Mongo and Http client connections.
    builder.Services.AddCustomTrustStore();
    
    const string apiVersion = "v1";
    
    builder.Services.AddOpenApi(apiVersion, options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Version = apiVersion;
            return Task.CompletedTask;
        });
    });

    // Configure logging to use the CDP Platform standards.
    builder.Services.AddHttpContextAccessor();
    builder.Host.UseSerilog(CdpLogging.Configuration);

    // Default HTTP Client
    builder.Services.AddHttpClient("DefaultClient").AddHeaderPropagation();

    // Proxy HTTP Client
    builder.Services.AddTransient<ProxyHttpMessageHandler>();
    builder.Services.AddHttpClient("proxy").ConfigurePrimaryHttpMessageHandler<ProxyHttpMessageHandler>();

    // Propagate trace header.
    builder.Services.AddHeaderPropagation(options =>
    {
        var traceHeader = builder.Configuration.GetValue<string>("TraceHeader");
        if (!string.IsNullOrWhiteSpace(traceHeader))
        {
            options.Headers.Add(traceHeader);
        }
    });

    // Set up the MongoDB client. Config and credentials are injected automatically at runtime.
    MongoClientSettings.Extensions.AddAWSAuthentication();
    builder.Services.Configure<MongoConfig>(builder.Configuration.GetSection("Mongo"));
    builder.Services.AddSingleton<IMongoDbClientFactory, MongoDbClientFactory>();

    // Add healthcheck, this is required for the platform to know your service is alive.
    builder.Services.AddHealthChecks();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
}

[ExcludeFromCodeCoverage]
static WebApplication SetupApplication(WebApplication app)
{
    app.UseHeaderPropagation();
    app.UseRouting();
    app.MapHealthChecks("/health");
    
    app.UseIntraEndpoints();

    const string openApiPath = "/.well-known/open-api.json";
    
    app.MapOpenApi(openApiPath);
    
    app.UseReDoc(options =>
    {
        options.RoutePrefix = "redoc";
        options.SpecUrl(openApiPath);
    });

    return app;
}