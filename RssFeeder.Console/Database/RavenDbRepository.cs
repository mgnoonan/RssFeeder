
using System;
using System.Collections.Generic;
using System.IO;
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
            int count = 0;
            string sqlQueryText = $"from RssFeedItems where FeedAttributes.UrlHash = \"{urlHash}\" and FeedAttributes.FeedId = \"{feedID}\"";

            using (IDocumentSession session = _store.OpenSession(database: collectionName))
            {
                count = session.Advanced.RawQuery<T>(sqlQueryText)
                    .ToList()
                    .Count;
            }

            return count > 0;
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
            string sqlQueryText = $@"from RssFeedItems 
                   where FeedAttributes.DateAdded <= '{DateTime.UtcNow.AddDays(-maximumAgeInDays):o}' 
                   and FeedAttributes.FeedId = '{feedId}'";

            return GetDocuments<T>(collectionName, sqlQueryText);
        }

        public List<T> GetAllDocuments<T>(string collectionName)
        {
            string sqlQueryText = $"from @all_docs";

            return GetDocuments<T>(collectionName, sqlQueryText);
        }

        public List<T> GetExportDocuments<T>(string collectionName, string feedId, DateTime startDate)
        {
            string sqlQueryText = $@"from RssFeedItems 
                   where FeedAttributes.DateAdded >= '{TimeZoneInfo.ConvertTimeToUtc(startDate):o}'
                   and FeedAttributes.FeedId = '{feedId}'";

            return GetDocuments<T>(collectionName, sqlQueryText);
        }

        public void UpsertDocument<T>(string collectionName, T item)
        {
            CreateDocument(collectionName, item, 0);
        }
    }
}
