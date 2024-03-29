﻿namespace RssFeeder.Mvc.Services;

public interface IDatabaseService
{
    Task<IEnumerable<RssFeedItem>> GetItemsAsync(QueryDefinition queryDef);
    Task<RssFeedItem> GetItemAsync(string id);
    Task AddItemAsync(RssFeedItem item);
    Task UpdateItemAsync(string id, RssFeedItem item);
    Task DeleteItemAsync(string id);
}