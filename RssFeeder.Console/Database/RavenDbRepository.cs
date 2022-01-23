using RssFeeder.Models;

namespace RssFeeder.Console.Database;

public class RavenDbRepository : IRepository, IExportRepository
{
    readonly ILogger _log;
    readonly IDocumentStore _store;

    public RavenDbRepository(IDocumentStore store, ILogger log)
    {
        _store = store;
        _log = log;
    }

    public void EnsureDatabaseExists(string database = null, bool createDatabaseIfNotExists = true)
    {
        database ??= _store.Database;

        if (string.IsNullOrWhiteSpace(database))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(database));

        try
        {
            _store.Maintenance.ForDatabase(database).Send(new GetStatisticsOperation());
        }
        catch (DatabaseDoesNotExistException)
        {
            if (createDatabaseIfNotExists == false)
                throw;

            try
            {
                _store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(database)));
            }
            catch (ConcurrencyException)
            {
                // The database was already created before calling CreateDatabaseOperation
            }
        }
    }

    public void CreateDocument<T>(string collectionName, T item, int expirationDays)
    {
        CreateDocument<T>(collectionName, item, expirationDays, null, null, null);
    }

    public void CreateDocument<T>(string collectionName, T item, int expirationDays, string filename, Stream stream, string contentType)
    {
        using (IDocumentSession session = _store.OpenSession(database: collectionName))
        {
            session.Store(item);

            if (expirationDays > 0)
            {
                DateTime expiry = DateTime.UtcNow.AddDays(expirationDays);
                session.Advanced.GetMetadataFor(item)[Raven.Client.Constants.Documents.Metadata.Expires] = expiry;
            }

            // Store the attachement
            if (!string.IsNullOrEmpty(filename))
            {
                session.Advanced.Attachments.Store(item, filename, stream, contentType);
            }

            session.SaveChanges();
        }
    }

    public void DeleteDocument<T>(string collectionName, string documentID, string partitionKey)
    {
        using (IDocumentSession session = _store.OpenSession(database: collectionName))
        {
            session.Delete(documentID);
            session.SaveChanges();
        }
    }

    public bool DocumentExists<T>(string collectionName, string feedID, string urlHash)
    {
        string sqlQueryText = "from RssFeedItems where FeedAttributes.UrlHash = $urlHash and FeedAttributes.FeedId = $feedId";

        var parameters = new Dictionary<string, object>
        {
            { "urlHash", urlHash },
            { "feedId", feedID }
        };

        using (IDocumentSession session = _store.OpenSession(database: collectionName))
        {
            var query = session.Advanced.RawQuery<T>(sqlQueryText);
            foreach (var p in parameters ?? new Dictionary<string, object>())
            {
                query.AddParameter(p.Key, p.Value);
            }

            var list = query.ToList();
            return list.Count > 0;
        }
    }

    public List<T> GetDocuments<T>(string collectionName, string sqlQueryText, Dictionary<string, object> parameters = default, bool addWait = false)
    {
        Log.Debug("Query: {sqlQueryText} Parameters: {@parameters}", sqlQueryText, parameters);

        using (IDocumentSession session = _store.OpenSession(database: collectionName))
        {
            var query = session.Advanced.RawQuery<T>(sqlQueryText);
            foreach (var p in parameters ?? new Dictionary<string, object>())
            {
                query.AddParameter(p.Key, p.Value);
            }
            if (addWait)
            {
                query.WaitForNonStaleResults();
            }

            var list = query.ToList();
            Log.Debug("Query: ({count}) documents returned", list.Count);
            return list;
        }
    }

    public List<T> GetAllDocuments<T>(string collectionName)
    {
        Log.Debug("Query: Retrieving all documents for type {type}", typeof(T).Name);
        string sqlQueryText = "from SiteArticleDefinition";

        return GetDocuments<T>(collectionName, sqlQueryText);
    }

    public List<T> GetExportDocuments<T>(string collectionName, string feedId, Guid runID)
    {
        string sqlQueryText = "from RssFeedItems where RunId = $runId and FeedAttributes.FeedId = $feedId";

        var parameters = new Dictionary<string, object>
        {
            { "runId", runID },
            { "feedId", feedId }
        };

        return GetDocuments<T>(collectionName, sqlQueryText, parameters, true);
    }

    public void UpsertDocument<T>(string collectionName, T item)
    {
        CreateDocument(collectionName, item, 0);
    }
}
