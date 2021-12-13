using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using RssFeeder.Mvc.Models;

namespace RssFeeder.Mvc.Services
{
    public class RavenDbService : IDatabaseService
    {
        readonly IDocumentStore _store;

        public RavenDbService(IDocumentStore store)
        {
            _store = store;
        }

        public Task AddItemAsync(RssFeedItem item)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteItemAsync(string id)
        {
            throw new System.NotImplementedException();
        }

        public Task<RssFeedItem> GetItemAsync(string id)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<RssFeedItem>> GetItemsAsync(QueryDefinition queryDef)
        {
            string sqlQueryText = $@"from ExportFeedItems 
                   where FeedId = 'drudge-report'";

            using (IDocumentSession session = _store.OpenSession(database: "drudge-report"))
            {
                var result = session.Advanced.RawQuery<ExportFeedItems>(sqlQueryText);
                return Task.FromResult(result.ToList().AsEnumerable<RssFeedItem>());
            }
        }

        public Task UpdateItemAsync(string id, RssFeedItem item)
        {
            throw new System.NotImplementedException();
        }
    }
}
