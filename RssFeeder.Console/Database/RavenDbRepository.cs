
using System;
using System.Collections.Generic;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
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
            string sqlQueryText = $@"from index 'Auto/AllDocs/ByDateAddedAndFeedIdAndSiteNameAndUrl'
                   where DateAdded <= '{DateTime.UtcNow.AddDays(-maximumAgeInDays):o}' 
                   and FeedId = '{feedId}'";

            return GetDocuments<T>(collectionName, sqlQueryText);
        }

        public List<T> GetAllDocuments<T>(string collectionName)
        {
            string sqlQueryText = $"from @all_docs";

            return GetDocuments<T>(collectionName, sqlQueryText);
        }
    }
}
