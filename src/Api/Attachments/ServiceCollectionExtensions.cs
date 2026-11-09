using System.Diagnostics.CodeAnalysis;
using Amazon;
using Amazon.S3;
using Api.Utils;
using Defra.TradeImports.CdpUploader;
using Defra.TradeImports.Messaging;
using Defra.TradeImports.Messaging.Consuming;
using Defra.TradeImports.Messaging.Extensions;
using Defra.TradeImports.Messaging.Health;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Api.Attachments;

/// <summary>
/// CHED attachments: queued downloads (Defra.TradeImports.Messaging), malware scanned by the CDP
/// uploader (Defra.TradeImports.CdpUploader), kept in S3. Expects <c>AddMessaging</c> to have been called.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChedAttachments(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<ChedAttachmentOptions>()
            .Bind(configuration.GetSection(ChedAttachmentOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Optional in the package; required here, as the scan result only ever arrives by callback.
        services
            .AddOptions<CdpUploaderOptions>()
            .Validate(o => !string.IsNullOrWhiteSpace(o.CallbackUrl), "CdpUploader:CallbackUrl is required.");

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IChedAttachmentRepository, ChedAttachmentRepository>();
        services.AddSingleton<IAttachmentStore, S3AttachmentStore>();
        services.AddSingleton<IChedAttachmentService, ChedAttachmentService>();
        services.AddSingleton<IScanResultsHandler, ScanResultsHandler>();
        services.AddSingleton<IAmazonS3>(CreateS3Client);

        services.AddPublisher<ChedAttachmentDownloadRequested>(sp =>
            sp.GetRequiredService<IOptions<ChedAttachmentOptions>>().Value.TopicArn
        );

        AddHealthChecks(services);

        services.AddCdpUploaderClient(configuration).AddHeaderPropagation();

        // Enabled is read when the service starts, so test configuration overrides apply.
        services.AddScoped<ChedAttachmentDownloadConsumer>();
        services.AddConsumer<ChedAttachmentDownloadConsumer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ChedAttachmentOptions>>().Value;
            return new SqsConsumerOptions
            {
                QueueUrl = options.QueueUrl,
                VisibilityTimeoutSeconds = options.VisibilityTimeoutSeconds,
                Enabled = options.ConsumerEnabled,
            };
        });

        return services;
    }

    private static void AddHealthChecks(IServiceCollection services)
    {
        static ChedAttachmentOptions Settings(IServiceProvider sp) =>
            sp.GetRequiredService<IOptions<ChedAttachmentOptions>>().Value;

        var timeout = TimeSpan.FromSeconds(10);
        string[] tags = [HealthEndpoints.Extended];

        services
            .AddHealthChecks()
            .AddSns("SNS - CHED attachments", sp => Settings(sp).TopicArn, tags, timeout)
            .AddSqs("SQS - CHED attachments", sp => Settings(sp).QueueUrl, tags, timeout)
            .Add(
                new HealthCheckRegistration(
                    "S3 - CHED attachments",
                    sp => new S3BucketHealthCheck(sp.GetRequiredService<IAmazonS3>(), Settings(sp).S3Bucket),
                    HealthStatus.Unhealthy,
                    tags,
                    timeout
                )
            );
    }

    private static IAmazonS3 CreateS3Client(IServiceProvider sp)
    {
        var floci = sp.GetRequiredService<IOptions<FlociOptions>>().Value;
        if (floci.UseFloci != true)
            return new AmazonS3Client();

        return new AmazonS3Client(
            floci.Credentials(),
            new AmazonS3Config
            {
                AuthenticationRegion = floci.Region,
                RegionEndpoint = RegionEndpoint.GetBySystemName(floci.Region),
                ServiceURL = floci.S3Endpoint,
                ForcePathStyle = true,
            }
        );
    }
}
