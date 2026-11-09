using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Attachments;

/// <summary>Checks the bucket exists and can be listed by this service.</summary>
public class S3BucketHealthCheck(IAmazonS3 s3Client, string bucketName) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await s3Client.ListObjectsV2Async(
                new ListObjectsV2Request { BucketName = bucketName, MaxKeys = 1 },
                cancellationToken
            );

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                exception: new InvalidOperationException($"Failed to connect to S3 bucket: {bucketName}", ex)
            );
        }
    }
}
