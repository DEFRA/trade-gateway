using System.Diagnostics.CodeAnalysis;
using System.Net;
using Amazon.S3;

namespace Api.Attachments;

public sealed record StoredAttachment(Stream Content, long? ContentLength) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}

public interface IAttachmentStore
{
    /// <summary>Opens a scanned attachment; null if the object no longer exists.</summary>
    Task<StoredAttachment?> Open(string bucket, string key, CancellationToken cancellationToken);
}

[ExcludeFromCodeCoverage]
public class S3AttachmentStore(IAmazonS3 s3Client) : IAttachmentStore
{
    public async Task<StoredAttachment?> Open(string bucket, string key, CancellationToken cancellationToken)
    {
        try
        {
            // Disposing the response stream releases the response and its connection.
            var response = await s3Client.GetObjectAsync(bucket, key, cancellationToken);
            return new StoredAttachment(response.ResponseStream, response.ContentLength);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
