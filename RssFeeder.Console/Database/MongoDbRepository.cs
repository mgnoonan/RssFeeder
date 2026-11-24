using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace RssFeeder.Console.Database;

public sealed class MongoDbRepository : IRepository, IExportRepository
{
    private const string DefaultDatabaseName = "feed-items";
    private readonly IMongoDatabase _database;
    private readonly ILogger _log;
    private readonly CrawlerConfig _crawlerConfig;

    public CrawlerConfig Config => _crawlerConfig;

    public MongoDbRepository(IConfigurationRoot configuration, ILogger log)
    {
        _log = log;

        var connectionString = configuration.GetConnectionString("feed-items")
            ?? configuration["MongoDB:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A MongoDB connection string is required in ConnectionStrings:feed-items or MongoDB:ConnectionString.");
        }

        var mongoUrl = new MongoUrl(connectionString);
        var databaseName = mongoUrl.DatabaseName ?? DefaultDatabaseName;
        _database = new MongoClient(mongoUrl).GetDatabase(databaseName);

        var configPath = Path.Combine(AppContext.BaseDirectory, "crawlerconfig.json");
        _crawlerConfig = JsonConvert.DeserializeObject<CrawlerConfig>(File.ReadAllText(configPath))
            ?? throw new InvalidOperationException($"Unable to read crawler configuration from '{configPath}'.");
    }

    public List<T> GetDocuments<T>(
        string collectionName,
        string sqlQueryText,
        Dictionary<string, object> parameters,
        bool addWait)
    {
        var filter = Builders<T>.Filter.Empty;
        foreach (var parameter in parameters ?? [])
        {
            var fieldName = GetMongoFieldName(parameter.Key);
            filter &= Builders<T>.Filter.Eq(fieldName, parameter.Value);
        }

        return GetCollection<T>(collectionName).Find(filter).ToList();
    }

    public List<T> GetExportDocuments<T>(string collectionName, string feedId, Guid runID)
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq("RunId", runID),
            Builders<T>.Filter.Eq("FeedAttributes.FeedId", feedId));

        return GetCollection<T>(collectionName).Find(filter).ToList();
    }

    public List<T> GetAllDocuments<T>(string collectionName)
    {
        return GetCollection<T>(collectionName).Find(FilterDefinition<T>.Empty).ToList();
    }

    public void SaveDocument<T>(string collectionName, T item, int expirationDays)
    {
        SaveDocument(collectionName, item, expirationDays, null, null, null);
    }

    public void SaveDocument<T>(
        string collectionName,
        T item,
        int expirationDays,
        string filename,
        Stream stream,
        string contentType)
    {
        ArgumentNullException.ThrowIfNull(item);

        var idProperty = typeof(T).GetProperty("Id");
        var id = idProperty?.GetValue(item)?.ToString();
        if (string.IsNullOrWhiteSpace(id))
        {
            id = ObjectId.GenerateNewId().ToString();
            if (idProperty?.CanWrite != true)
            {
                throw new InvalidOperationException(
                    $"{typeof(T).Name} requires a writable Id property to be saved in MongoDB.");
            }

            idProperty.SetValue(item, id);
        }

        var collection = GetCollection<T>(collectionName);
        collection.ReplaceOne(
            Builders<T>.Filter.Eq("_id", id),
            item,
            new ReplaceOptions { IsUpsert = true });

        if (expirationDays > 0)
        {
            var rawCollection = _database.GetCollection<BsonDocument>(GetCollectionName(collectionName));
            var expirationField = "ExpiresAt";
            rawCollection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending(expirationField),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }));
            rawCollection.UpdateOne(
                Builders<BsonDocument>.Filter.Eq("_id", id),
                Builders<BsonDocument>.Update.Set(expirationField, DateTime.UtcNow.AddDays(expirationDays)));
        }
    }

    public bool DocumentExists<T>(string collectionName, string feedID, string urlHash)
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq("FeedAttributes.UrlHash", urlHash),
            Builders<T>.Filter.Eq("FeedAttributes.FeedId", feedID));

        return GetCollection<T>(collectionName).Find(filter).Limit(1).Any();
    }

    public void DeleteDocument<T>(string collectionName, string documentID, string partitionKey)
    {
        GetCollection<T>(collectionName).DeleteOne(Builders<T>.Filter.Eq("_id", documentID));
    }

    public void UpsertDocument<T>(string collectionName, T item)
    {
        SaveDocument(collectionName, item, 0);
    }

    public void EnsureDatabaseExists(string database = null, bool createDatabaseIfNotExists = true)
    {
        if (!createDatabaseIfNotExists || string.IsNullOrWhiteSpace(database))
        {
            return;
        }

        var collectionName = GetCollectionName(database);
        var exists = _database.ListCollectionNames(
            new ListCollectionNamesOptions { Filter = Builders<BsonDocument>.Filter.Eq("name", collectionName) })
            .Any();
        if (!exists)
        {
            _database.CreateCollection(collectionName);
        }
    }

    public List<T> GetStaleDocuments<T>(string collectionName, string feedId, short maximumAgeInDays)
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq("FeedAttributes.FeedId", feedId),
            Builders<T>.Filter.Ne("HtmlAttributes.ParserResult", string.Empty),
            Builders<T>.Filter.Exists("HtmlAttributes.ParserResult"),
            Builders<T>.Filter.Lte("FeedAttributes.DateAdded", DateTime.UtcNow.AddDays(-maximumAgeInDays)));

        return GetCollection<T>(collectionName).Find(filter).ToList();
    }

    private IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        return _database.GetCollection<T>(GetCollectionName(collectionName));
    }

    private static string GetCollectionName(string collectionName)
    {
        return string.Equals(collectionName, "feed-items", StringComparison.OrdinalIgnoreCase)
            ? "rss-feed-items"
            : collectionName;
    }

    private static string GetMongoFieldName(string parameterName)
    {
        return parameterName switch
        {
            "feedId" => "FeedAttributes.FeedId",
            "urlHash" => "FeedAttributes.UrlHash",
            "runId" => "RunId",
            "ts" => "FeedAttributes.DateAdded",
            _ => parameterName
        };
    }
}
