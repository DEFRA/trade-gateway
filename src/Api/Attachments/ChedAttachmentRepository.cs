using System.Diagnostics.CodeAnalysis;
using Api.Utils.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Api.Attachments;

public interface IChedAttachmentRepository
{
    Task<ChedAttachmentEntity?> Get(string id, CancellationToken cancellationToken);

    /// <summary>Inserts the record; false if one with the same id already exists.</summary>
    Task<bool> TryInsert(ChedAttachmentEntity entity, CancellationToken cancellationToken);

    Task Replace(ChedAttachmentEntity entity, CancellationToken cancellationToken);

    Task Delete(string id, CancellationToken cancellationToken);
}

[ExcludeFromCodeCoverage]
public class ChedAttachmentRepository(
    IMongoDbClientFactory connectionFactory,
    IOptions<ChedAttachmentOptions> options,
    ILoggerFactory loggerFactory
) : MongoService<ChedAttachmentEntity>(connectionFactory, CollectionName, loggerFactory), IChedAttachmentRepository
{
    public const string CollectionName = "ched_attachments";

    // A field initialiser so it is set before the base constructor calls DefineIndexes.
    private readonly TimeSpan _timeToLive = options.Value.RecordTimeToLive;

    protected override List<CreateIndexModel<ChedAttachmentEntity>> DefineIndexes(
        IndexKeysDefinitionBuilder<ChedAttachmentEntity> builder
    ) =>
        [
            new(
                builder.Ascending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "CreatedAtTtlIdx", ExpireAfter = _timeToLive }
            ),
        ];

    public async Task<ChedAttachmentEntity?> Get(string id, CancellationToken cancellationToken) =>
        await Collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> TryInsert(ChedAttachmentEntity entity, CancellationToken cancellationToken)
    {
        try
        {
            await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public Task Replace(ChedAttachmentEntity entity, CancellationToken cancellationToken) =>
        Collection.ReplaceOneAsync(
            x => x.Id == entity.Id,
            entity,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken
        );

    public Task Delete(string id, CancellationToken cancellationToken) =>
        Collection.DeleteOneAsync(x => x.Id == id, cancellationToken);
}
