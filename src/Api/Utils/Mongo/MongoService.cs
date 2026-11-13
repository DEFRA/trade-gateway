using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Api.Utils.Mongo;

[ExcludeFromCodeCoverage]
public abstract class MongoService<T>
{
    protected readonly IMongoClient Client;
    protected readonly IMongoCollection<T> Collection;

    protected readonly ILogger Logger;

    protected MongoService(IMongoDbClientFactory connectionFactory, string collectionName, ILoggerFactory loggerFactory)
    {
        Client = connectionFactory.GetClient();
        Collection = connectionFactory.GetCollection<T>(collectionName);
        var loggerName = GetType().FullName ?? GetType().Name;
        Logger = loggerFactory.CreateLogger(loggerName);
        EnsureIndexes();
    }

    protected abstract List<CreateIndexModel<T>> DefineIndexes(IndexKeysDefinitionBuilder<T> builder);

    protected void EnsureIndexes()
    {
        var builder = Builders<T>.IndexKeys;
        var indexes = DefineIndexes(builder);
        if (indexes.Count == 0)
            return;

        Logger.LogInformation(
            "Ensuring index is created if it does not exist for collection {CollectionNamespaceCollectionName} in DB {DatabaseDatabaseNamespace}",
            Collection.CollectionNamespace.CollectionName,
            Collection.Database.DatabaseNamespace
        );
        try
        {
            Collection.Indexes.CreateMany(indexes);
        }
        catch (MongoCommandException ex) when (ex.Code == IndexOptionsConflict)
        {
            // An index already exists with the same name but different options. A TTL that changed in
            // config is updated in place; anything else still fails, as before.
            UpdateTimeToLives(indexes);
            Collection.Indexes.CreateMany(indexes);
        }
    }

    private const int IndexOptionsConflict = 85;

    private void UpdateTimeToLives(List<CreateIndexModel<T>> indexes)
    {
        var timeToLives = indexes
            .Select(index => index.Options)
            .Where(options => options?.ExpireAfter is not null && options.Name is not null)
            .Select(options => (Name: options!.Name, ExpireAfter: options.ExpireAfter!.Value));

        foreach (var (name, expireAfter) in timeToLives)
        {
            Logger.LogInformation(
                "Updating TTL of index {IndexName} on collection {CollectionName} to {ExpireAfter}",
                name,
                Collection.CollectionNamespace.CollectionName,
                expireAfter
            );
            Collection.Database.RunCommand<BsonDocument>(
                new BsonDocument
                {
                    { "collMod", Collection.CollectionNamespace.CollectionName },
                    {
                        "index",
                        new BsonDocument { { "name", name }, { "expireAfterSeconds", (long)expireAfter.TotalSeconds } }
                    },
                }
            );
        }
    }
}
