
using System;
using System.Collections.Generic;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Serilog;

namespace RssFeeder.Console.Database
{
    public class RavenDbRepository : IRepository
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

        public void CreateDocument<T>(string collectionName, T item)
        {
            using (IDocumentSession session = _store.OpenSession(database: collectionName))
            {
                session.Store(item);
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
            int count = 0;
            string sqlQueryText = $"from RssFeedItems where UrlHash = \"{urlHash}\" and FeedId = \"{feedID}\"";

            using (IDocumentSession session = _store.OpenSession(database: collectionName))
            {
                count = session.Advanced.RawQuery<T>(sqlQueryText)
                    .ToList()
                    .Count;
            }

            return count == 1;
        }

        public List<T> GetDocuments<T>(string collectionName, string sqlQueryText)
        {
            Log.Information("Query: {query}", sqlQueryText);

            using (IDocumentSession session = _store.OpenSession(database: collectionName))
            {
                return session.Advanced.RawQuery<T>(sqlQueryText)
                    .ToList();
            }
        }

        public List<T> GetStaleDocuments<T>(string collectionName, string feedId, short maximumAgeInDays)
        {
            // index 'Auto/AllDocs/ByDateAddedAndFeedIdAndSiteNameAndUrl'
            string sqlQueryText = $@"from @all_docs 
                   where DateAdded <= '{DateTime.UtcNow.AddDays(-maximumAgeInDays):o}' 
                   and FeedId = '{feedId}'";

            return GetDocuments<T>(collectionName, sqlQueryText);
        }

        public List<T> GetAllDocuments<T>(string collectionName)
        {
            string sqlQueryText = $"from @all_docs";

            return GetDocuments<T>(collectionName, sqlQueryText);
        }

        public List<T> GetExportDocuments<T>(string collectionName, string feedId, int minutes)
        {
            string sqlQueryText = $@"from @all_docs 
                   where DateAdded >= '{DateTime.UtcNow.AddMinutes(-minutes):o}'
                   and FeedId = '{feedId}'";

            return GetDocuments<T>(collectionName, sqlQueryText);
        }
    }
}
