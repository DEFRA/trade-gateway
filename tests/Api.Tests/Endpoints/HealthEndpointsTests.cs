using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NSubstitute.ExceptionExtensions;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class HealthEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    private const string SnsCheck = "SNS - CHED attachments";
    private const string SqsCheck = "SQS - CHED attachments";
    private const string S3Check = "S3 - CHED attachments";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<(HttpStatusCode Status, JsonElement Body, string Raw)> GetAll()
    {
        var response = await factory.CreateClient().GetAsync("/health/all", Ct);
        var raw = await response.Content.ReadAsStringAsync(Ct);
        return (response.StatusCode, JsonDocument.Parse(raw).RootElement, raw);
    }

    private static string StatusOf(JsonElement body, string check) =>
        body.GetProperty("results").GetProperty(check).GetString()!;

    [Fact]
    public async Task All_WhenEverythingIsReachable_ReportsEachDependencyHealthy()
    {
        var (status, body, _) = await GetAll();

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        Assert.Equal("Healthy", StatusOf(body, SnsCheck));
        Assert.Equal("Healthy", StatusOf(body, SqsCheck));
        Assert.Equal("Healthy", StatusOf(body, S3Check));
    }

    [Fact]
    public async Task All_ChecksTheConfiguredResources()
    {
        await GetAll();

        await factory
            .Sns.Received()
            .GetTopicAttributesAsync(
                Arg.Is<GetTopicAttributesRequest>(r =>
                    r.TopicArn == TradeGatewayWebApplicationFactory.AttachmentsTopicArn
                ),
                Arg.Any<CancellationToken>()
            );
        await factory
            .S3.Received()
            .ListObjectsV2Async(
                Arg.Is<ListObjectsV2Request>(r => r.BucketName == TradeGatewayWebApplicationFactory.AttachmentsBucket),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task All_WhenTheTopicIsUnreachable_ReportsItUnhealthy()
    {
        factory
            .Sns.GetTopicAttributesAsync(Arg.Any<GetTopicAttributesRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("sns down"));

        try
        {
            var (status, body, _) = await GetAll();

            Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
            Assert.Equal("Unhealthy", StatusOf(body, SnsCheck));
            Assert.Equal("Healthy", StatusOf(body, SqsCheck));
        }
        finally
        {
            factory.Sns.ClearSubstitute();
        }
    }

    [Fact]
    public async Task All_WhenTheQueueIsUnreachable_ReportsItUnhealthy()
    {
        factory
            .Sqs.GetQueueAttributesAsync(Arg.Any<string>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new QueueDoesNotExistException("sqs down"));

        try
        {
            var (status, body, _) = await GetAll();

            Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
            Assert.Equal("Unhealthy", StatusOf(body, SqsCheck));
        }
        finally
        {
            factory.Sqs.ClearSubstitute();
        }
    }

    [Fact]
    public async Task All_WhenTheBucketIsUnreachable_ReportsItUnhealthyWithoutDetails()
    {
        factory
            .S3.ListObjectsV2Async(Arg.Any<ListObjectsV2Request>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonS3Exception("NoSuchBucket secret-detail"));

        try
        {
            var (status, body, raw) = await GetAll();

            Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
            Assert.Equal("Unhealthy", StatusOf(body, S3Check));
            Assert.DoesNotContain("secret-detail", raw);
            Assert.DoesNotContain(TradeGatewayWebApplicationFactory.AttachmentsBucket, raw);
        }
        finally
        {
            factory.S3.ClearSubstitute();
        }
    }

    [Fact]
    public async Task Health_StaysHealthyAndDoesNotCallDependencies_WhenTheyAreDown()
    {
        factory
            .S3.ListObjectsV2Async(Arg.Any<ListObjectsV2Request>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonS3Exception("down"));

        try
        {
            factory.S3.ClearReceivedCalls();

            var response = await factory.CreateClient().GetAsync("/health", Ct);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await factory
                .S3.DidNotReceive()
                .ListObjectsV2Async(Arg.Any<ListObjectsV2Request>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            factory.S3.ClearSubstitute();
        }
    }
}
