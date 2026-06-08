using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Api.Config;
using Api.Endpoints;
using Api.Utils;
using Api.Utils.Http;
using Api.Utils.Logging;
using Api.Utils.Mongo;
using FluentValidation;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using MongoDB.Driver;
using MongoDB.Driver.Authentication.AWS;
using Serilog;
using TracesNT;
using TracesNT.Extensions;

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
    // Load certificates into Trust Store - Note must happen before Mongo and Http client connections.
    builder.Services.AddCustomTrustStore();

    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = ctx => ctx.ProblemDetails.Extensions.Remove("traceId")
    );
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Trade Gateway", Version = "v1" });
        options.OperationFilter<ContentNegotiationOperationFilter>();
        options.SchemaFilter<ConstValueSchemaFilter>();
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

    // add the Traces NT clients
    var tracesNtSection = builder.Configuration.GetRequiredSection("TracesNt");
    builder.Services.AddOptions<TracesNtConfig>().Bind(tracesNtSection).ValidateDataAnnotations().ValidateOnStart();

    var xApiKey = builder.Configuration.GetValue<string?>("XApiKey");
    builder.Services.AddTracesNtClients(xApiKey!);

    // Set up the MongoDB client. Config and credentials are injected automatically at runtime.
    MongoClientSettings.Extensions.AddAWSAuthentication();
    builder.Services.Configure<MongoConfig>(builder.Configuration.GetSection("Mongo"));
    builder.Services.AddSingleton<IMongoDbClientFactory, MongoDbClientFactory>();

    // Add health check, this is required for the platform to know your service is alive.
    builder.Services.AddHealthChecks();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    builder.AddApiAuthentication();
}

[ExcludeFromCodeCoverage]
static WebApplication SetupApplication(WebApplication app)
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = ".well-known/openapi/{documentName}/openapi.json";
    });
    app.UseReDoc(options =>
    {
        options.RoutePrefix = "redoc";
        options.ConfigObject.ExpandResponses = "200";
        options.SpecUrl("/.well-known/openapi/v1/openapi.json");
    });

    app.UseExceptionHandler();
    app.UseHeaderPropagation();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health");
    app.UseIntraEndpoints();
    app.UseAuthTestEndpoints();

    return app;
}
