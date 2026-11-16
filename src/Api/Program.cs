using System.Diagnostics.CodeAnalysis;
using Api.Attachments;
using Api.Config;
using Api.Endpoints;
using Api.Utils;
using Api.Utils.Http;
using Api.Utils.Logging;
using Api.Utils.Mongo;
using Defra.TradeImports.Api.Metrics;
using Defra.TradeImports.EmfExporter;
using Defra.TradeImports.Messaging.Extensions;
using FluentValidation;
using Microsoft.OpenApi;
using MongoDB.Driver;
using MongoDB.Driver.Authentication.AWS;
using Serilog;
using TracesNT;
using TracesNT.Extensions;
using TracesNT.Services;

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
        options.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title = "Trade Gateway",
                Version = "v1",
                Description =
                    "REST API for TRACES: read access to TRACES certificates "
                    + "(CHED, INTRA and DOCOMs) and customs quantity management for CHEDs.",
                License = new OpenApiLicense
                {
                    Name = "Open Government Licence v3.0",
#pragma warning disable S1075
                    Url = new Uri("https://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/"),
#pragma warning restore S1075
                },
                Contact = new OpenApiContact { Name = "Nexus Team", Email = "#eudp-traces-integration-questions" },
            }
        );
        options.AddServer(
            new OpenApiServer { Url = "/", Description = "The service as reached through the CDP API Gateway." }
        );
        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Cognito or STS-issued JWT. Send as: Authorization: Bearer <token>",
            }
        );
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
        });
        options.OperationFilter<ContentNegotiationOperationFilter>();
        options.OperationFilter<AcceptLanguageHeaderOperationFilter>();
        options.OperationFilter<ParameterDescriptionDedupOperationFilter>();
        options.DocumentFilter<TagDocumentFilter>();
        options.SchemaFilter<ConstValueSchemaFilter>();
        options.SupportNonNullableReferenceTypes();
        options.IncludeXmlComments(typeof(Program).Assembly);
    });

    // Configure logging to use the CDP Platform standards.
    builder.Services.AddHttpContextAccessor();
    builder.Host.UseSerilog(CdpLogging.Configuration);

    // Default HTTP Client
    builder.Services.AddHttpClient("DefaultClient").AddHeaderPropagation();

    // Proxy HTTP Client
    builder.Services.AddTransient<ProxyHttpMessageHandler>();
    builder.Services.AddHttpClient("proxy").ConfigurePrimaryHttpMessageHandler<ProxyHttpMessageHandler>();

    // Propagate the trace header. Which header is set by AddTraceContextAccessor (called by
    // AddMessaging below) from the required "TraceHeader" setting.
    builder.Services.AddHeaderPropagation();

    // add the Traces NT clients
    var tracesNtSection = builder.Configuration.GetRequiredSection("TracesNt");
    builder.Services.AddOptions<TracesNtConfig>().Bind(tracesNtSection).ValidateDataAnnotations().ValidateOnStart();

    builder.Services.AddSingleton<ITracesNtClientMetricsService, TracesNtClientMetricsService>();
    builder.Services.AddTracesNtCredentials(tracesNtSection);
    builder.Services.AddTracesNtClients();

    // Set up the MongoDB client. Config and credentials are injected automatically at runtime.
    try
    {
        MongoClientSettings.Extensions.AddAWSAuthentication();
    }
    catch (ArgumentException ex)
        when (ex.ParamName == "mechanismName"
            || (ex.Message != null && ex.Message.IndexOf("already registered", StringComparison.OrdinalIgnoreCase) >= 0)
        )
    {
        // The MONGODB-AWS mechanism may already be registered (e.g. when tests/startup run multiple times). Ignore this specific error.
    }

    builder.Services.Configure<MongoConfig>(builder.Configuration.GetSection("Mongo"));
    builder.Services.AddSingleton<IMongoDbClientFactory, MongoDbClientFactory>();

    // CHED attachments: queued downloads (SNS/SQS FIFO), malware scanned by the CDP uploader, kept in S3.
    builder.Services.AddMessaging(
        builder.Configuration,
        options => options.MeterName = TracesNtClientMetricsService.MeterName
    );
    builder.Services.AddChedAttachments(builder.Configuration);

    // Add health check, this is required for the platform to know your service is alive.
    builder.Services.AddHealthChecks();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    builder.Services.AddValidation();
    builder.AddApiAuthentication();
    builder.Services.AddApiMetrics();
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

    app.UseHeaderPropagation();
    app.UseRouting();
    app.UseMiddleware<ApiMetricsMiddleware>();
    app.UseExceptionHandler();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthEndpoints();
    app.MapLocalTokenEndpoints();
    app.UseChedEndpoints();
    app.UseAttachmentScanResultsEndpoints();
    app.UseCustomsChedQuantityEndpoints();
    app.UseIntraEndpoints();
    app.UseDocomEndpoints();
    app.UseAuthTestEndpoints();
    app.UseReferenceDataEndpoints();
    app.UseEmfExporter(TracesNtClientMetricsService.MeterName);

    return app;
}
