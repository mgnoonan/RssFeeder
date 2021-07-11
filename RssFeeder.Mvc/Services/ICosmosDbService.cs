using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using RssFeeder.Mvc.Models;

namespace RssFeeder.Mvc.Services
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<RssFeedItem>> GetItemsAsync(QueryDefinition queryDef);
        Task<RssFeedItem> GetItemAsync(string id);
        Task AddItemAsync(RssFeedItem item);
        Task UpdateItemAsync(string id, RssFeedItem item);
        Task DeleteItemAsync(string id);
    }
}