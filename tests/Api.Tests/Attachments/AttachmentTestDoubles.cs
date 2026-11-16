using System.Collections.Concurrent;
using System.Text.Json;
using Api.Attachments;
using Defra.TradeImports.Messaging;
using Defra.TradeImports.Messaging.Publishing;

namespace Api.Tests.Attachments;

/// <summary>Mongo stand-in. Stores copies, so a test sees only what was saved.</summary>
public sealed class InMemoryChedAttachmentRepository : IChedAttachmentRepository
{
    private readonly ConcurrentDictionary<string, string> _records = new();

    public Task<ChedAttachmentEntity?> Get(string id, CancellationToken cancellationToken) =>
        Task.FromResult(_records.TryGetValue(id, out var json) ? Deserialize(json) : null);

    public Task<bool> TryInsert(ChedAttachmentEntity entity, CancellationToken cancellationToken) =>
        Task.FromResult(_records.TryAdd(entity.Id, JsonSerializer.Serialize(entity)));

    public Task Replace(ChedAttachmentEntity entity, CancellationToken cancellationToken)
    {
        _records[entity.Id] = JsonSerializer.Serialize(entity);
        return Task.CompletedTask;
    }

    public Task Delete(string id, CancellationToken cancellationToken)
    {
        _records.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public ChedAttachmentEntity? Find(string chedId, long attachmentId) =>
        _records.TryGetValue(ChedAttachmentEntity.CreateId(chedId, attachmentId), out var json)
            ? Deserialize(json)
            : null;

    public void Save(ChedAttachmentEntity entity) => _records[entity.Id] = JsonSerializer.Serialize(entity);

    private static ChedAttachmentEntity Deserialize(string json) =>
        JsonSerializer.Deserialize<ChedAttachmentEntity>(json)!;
}

/// <summary>S3 stand-in: what the CDP uploader would have moved into the bucket.</summary>
public sealed class InMemoryAttachmentStore : IAttachmentStore
{
    private readonly ConcurrentDictionary<(string Bucket, string Key), byte[]> _objects = new();

    public void Put(string bucket, string key, byte[] content) => _objects[(bucket, key)] = content;

    public Task<StoredAttachment?> Open(string bucket, string key, CancellationToken cancellationToken) =>
        Task.FromResult(
            _objects.TryGetValue((bucket, key), out var content)
                ? new StoredAttachment(new MemoryStream(content), content.Length)
                : null
        );
}

public sealed record PublishedMessage(string TopicArn, string Body, string? DuplicationId, string? MessageGroupId);

/// <summary>SNS stand-in that records what was published, optionally failing.</summary>
public sealed class RecordingSnsPublisher : ISnsPublisher
{
    private readonly ConcurrentQueue<PublishedMessage> _published = new();

    public Func<string, Exception?> FailWhen { get; set; } = _ => null;

    public IReadOnlyList<PublishedMessage> Published => _published.ToList();

    public IReadOnlyList<PublishedMessage> For(string chedId, long attachmentId) =>
        _published
            .Where(m =>
                JsonSerializer.Deserialize<ChedAttachmentDownloadRequested>(m.Body) is { } request
                && request.ChedId == chedId
                && request.AttachmentId == attachmentId
            )
            .ToList();

    public Task PublishAsync(
        string topicArn,
        string messageBody,
        Dictionary<string, string>? headers = null,
        string? subject = null,
        string? duplicationId = null,
        string? messageGroupId = null,
        CancellationToken cancellationToken = default
    )
    {
        if (FailWhen(messageBody) is { } exception)
            throw exception;

        _published.Enqueue(new PublishedMessage(topicArn, messageBody, duplicationId, messageGroupId));
        return Task.CompletedTask;
    }

    public Task PublishAsync<T>(
        string topicArn,
        T message,
        Dictionary<string, string>? headers = null,
        string? subject = null,
        CancellationToken cancellationToken = default
    )
        where T : IMessage =>
        PublishAsync(
            topicArn,
            JsonSerializer.Serialize(message),
            headers,
            subject,
            message.DuplicationId,
            message.MessageGroupId,
            cancellationToken
        );
}

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
