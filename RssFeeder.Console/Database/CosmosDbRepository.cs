namespace RssFeeder.Console.Database;

public class CosmosDbRepository : IRepository, IExportRepository
{
    string _databaseName;
    ILogger _log;
    CosmosClient _client = null;

    public CosmosDbRepository(string databaseName, string endpointUrl, string authKey, ILogger log)
    {
        _databaseName = databaseName;
        _log = log;

        _client = new CosmosClient(endpointUrl, authKey);
    }

    public List<T> GetDocuments<T>(string collectionName, string sqlQueryText, Dictionary<string, object> parameters = default, bool addWait = false)
    {
        Log.Information("GetDocuments: query = '{sqlQueryText}'", sqlQueryText);
        var result = QueryItems<T>(collectionName, sqlQueryText);

        _log.Information("GetDocuments returned {count} documents from collection '{collectionName}'", result.Count, collectionName);
        return result;
    }

    public void CreateDocument<T>(string collectionName, T item, int expirationDays)
    {
        CreateDocument<T>(collectionName, item, expirationDays, null, null, null);
    }

    public void CreateDocument<T>(string collectionName, T item, int expirationDays, string filename, Stream stream, string contentType)
    {
        var container = _client.GetContainer(_databaseName, collectionName);
        var result = container.CreateItemAsync(item).Result;

        if (result.StatusCode != HttpStatusCode.Created)
        {
            _log.Error("Status code:{statusCode} for CreateDocument on '{@item}'", result.StatusCode, item);
        }
    }

    public bool DocumentExists<T>(string collectionName, string feedID, string urlHash)
    {
        string sqlQueryText = $"SELECT c.UrlHash FROM c WHERE c.UrlHash = '{urlHash}' AND c.FeedId = '{feedID}'";

        return QueryItems<T>(collectionName, sqlQueryText)
            .Count() > 0;
    }

    public void DeleteDocument<T>(string collectionName, string documentID, string partitionKey)
    {
        try
        {
            var pk = string.IsNullOrEmpty(partitionKey) ? PartitionKey.None : new PartitionKey(partitionKey);

            var container = _client.GetContainer(_databaseName, collectionName);
            var result = container.DeleteItemAsync<T>(documentID, pk).Result;

            if (result.StatusCode != HttpStatusCode.NoContent)
            {
                _log.Warning("Status code:{statusCode} for DeleteDocument '{documentID}' from collection '{collectionName}'", result.StatusCode, documentID, collectionName);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error deleting document '{documentID}' from collection '{collectionName}'", documentID, collectionName);
        }
    }

    public List<T> QueryItems<T>(string collectionName, string sqlQueryText)
    {
        var container = _client.GetContainer(_databaseName, collectionName);

        QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
        FeedIterator<T> iterator = container.GetItemQueryIterator<T>(queryDefinition);

        List<T> results = new List<T>();

        while (iterator.HasMoreResults)
        {
            FeedResponse<T> response = iterator.ReadNextAsync().Result;
            foreach (T item in response)
            {
                results.Add(item);
            }
        }

        return results;
    }

    public List<T> GetAllDocuments<T>(string collectionName)
    {
        string sqlQueryText = $"SELECT * FROM c";

        return GetDocuments<T>(collectionName, sqlQueryText);
    }

    public void EnsureDatabaseExists(string database = null, bool createDatabaseIfNotExists = true)
    {
        //throw new NotImplementedException();
    }

    public List<T> GetExportDocuments<T>(string collectionName, string feedId, Guid runID)
    {
        throw new NotImplementedException();
    }

    public void UpsertDocument<T>(string collectionName, T item)
    {
        var container = _client.GetContainer(_databaseName, collectionName);
        var result = container.UpsertItemAsync(item).Result;

        if (result.StatusCode != HttpStatusCode.Created)
        {
            _log.Error("Unable to create document for '{@item}'", item);
        }
    }
}
