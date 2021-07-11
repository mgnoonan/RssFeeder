using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using RssFeeder.Mvc.Models;

namespace RssFeeder.Mvc.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly Container _container;

        public CosmosDbService(
            CosmosClient dbClient,
            string databaseName,
            string containerName)
        {
            this._container = dbClient.GetContainer(databaseName, containerName);
        }

        public async Task AddItemAsync(RssFeedItem item)
        {
            await this._container.CreateItemAsync<RssFeedItem>(item, new PartitionKey(item.Id));
        }

        public async Task DeleteItemAsync(string id)
        {
            await this._container.DeleteItemAsync<RssFeedItem>(id, new PartitionKey(id));
        }

        public async Task<RssFeedItem> GetItemAsync(string id)
        {
            try
            {
                ItemResponse<RssFeedItem> response = await this._container.ReadItemAsync<RssFeedItem>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

        }

        public async Task<IEnumerable<RssFeedItem>> GetItemsAsync(QueryDefinition queryDef)
        {
            var query = this._container.GetItemQueryIterator<RssFeedItem>(queryDef);

            List<RssFeedItem> results = new List<RssFeedItem>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task UpdateItemAsync(string id, RssFeedItem item)
        {
            await this._container.UpsertItemAsync<RssFeedItem>(item, new PartitionKey(id));
        }
    }
}
