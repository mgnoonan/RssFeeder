namespace RssFeeder.Console.Database;

public class RavenDbRepository : IRepository, IExportRepository
{
    private const string _databaseName = "site-parsers";
    readonly ILogger _log;
    readonly IDocumentStore _store;
    readonly CrawlerConfig _crawlerConfig;

    public CrawlerConfig Config => _crawlerConfig;

    public RavenDbRepository(ILogger log)
    {
        // Setup RavenDb
        // docker run --rm -d -p 8080:8080 -p 38888:38888 ravendb/ravendb:latest
        IDocumentStore store = new DocumentStore
        {
            Urls = new[] { "http://127.0.0.1:8080/" }
            // Default database is not set
        }.Initialize();

        _store = store;
        _log = log;

        EnsureDatabaseExists(_databaseName, true);

#if DEBUG
        // Read the options in JSON format
        using StreamReader sr = new StreamReader("crawlerConfig.json");
        _crawlerConfig = JsonConvert.DeserializeObject<CrawlerConfig>(sr.ReadToEnd());
#else
        using (IDocumentSession session = store.OpenSession(database: _databaseName))
        {
            _crawlerConfig = session.Advanced.RawQuery<CrawlerConfig>("from CrawlerConfig").First();
        }
#endif

        _log.Debug("Crawler config: {@config}", _crawlerConfig);
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
            if (!createDatabaseIfNotExists)
                throw;

            try
            {
                _log.Information("Creating missing database {databaseName}", database);
                _store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(database)));
            }
            catch (ConcurrencyException)
            {
                // The database was already created before calling CreateDatabaseOperation
            }
        }
    }

    public void SaveDocument<T>(string collectionName, T item, int expirationDays)
    {
        SaveDocument<T>(collectionName, item, expirationDays, null, null, null);
    }

    public void SaveDocument<T>(string collectionName, T item, int expirationDays, string filename, Stream stream, string contentType)
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
            foreach (var p in parameters)
            {
                query.AddParameter(p.Key, p.Value);
            }

            var list = query.ToList();
            return list.Count > 0;
        }
    }

    public List<T> GetDocuments<T>(string collectionName, string sqlQueryText, Dictionary<string, object> parameters, bool addWait)
    {
        _log.Debug("Query: {sqlQueryText} Parameters: {@parameters}", sqlQueryText, parameters);

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
            _log.Debug("Query: ({count}) documents returned", list.Count);
            return list;
        }
    }

    public List<T> GetAllDocuments<T>(string collectionName)
    {
        _log.Debug("Query: Retrieving all documents for type {type}", typeof(T).Name);
        string sqlQueryText = "from SiteArticleDefinition";

        return GetDocuments<T>(collectionName, sqlQueryText, null, false);
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
        SaveDocument(collectionName, item, 0);
    }

    public List<T> GetStaleDocuments<T>(string collectionName, string feedId, short maximumAgeInDays)
    {
        string sqlQueryText = "from RssFeedItems where FeedAttributes.FeedId = $feedId and HtmlAttributes.ParserResult.length > 0 and FeedAttributes.DateAdded <= $ts";

        var parameters = new Dictionary<string, object>
        {
            { "feedId", feedId },
            { "ts", DateTime.UtcNow.AddDays(-maximumAgeInDays) }
        };

        return GetDocuments<T>(collectionName, sqlQueryText, parameters, false);
    }
}
