using MongoDB.Bson;
using MongoDB.Driver;
using RssFeeder.Models;

namespace RssFeeder.Admin.Services;

public interface IFeedItemRepository
{
    Task<IReadOnlyList<RssFeedItem>> GetFeedItemsAsync(int take = 200, CancellationToken cancellationToken = default);

    Task<RssFeedItem?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<RssFeedItem> SaveAsync(RssFeedItem item, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class MongoDbFeedItemRepository : IFeedItemRepository
{
    private readonly IMongoCollection<RssFeedItem> _items;

    public MongoDbFeedItemRepository(IMongoDatabase database)
    {
        _items = database.GetCollection<RssFeedItem>("rss-feed-items");
    }

    public async Task<IReadOnlyList<RssFeedItem>> GetFeedItemsAsync(int take = 200, CancellationToken cancellationToken = default)
    {
        var cursor = await _items.FindAsync(
            FilterDefinition<RssFeedItem>.Empty,
            new FindOptions<RssFeedItem, RssFeedItem>
            {
                Sort = Builders<RssFeedItem>.Sort.Descending(item => item.FeedAttributes.DateAdded),
                Limit = Math.Max(1, take)
            },
            cancellationToken);

        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<RssFeedItem?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RssFeedItem>.Filter.Eq(item => item.Id, id);
        return await _items.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RssFeedItem> SaveAsync(RssFeedItem item, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(item.Id))
        {
            item.Id = ObjectId.GenerateNewId().ToString();
        }

        var filter = Builders<RssFeedItem>.Filter.Eq(existing => existing.Id, item.Id);
        await _items.ReplaceOneAsync(filter, item, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        return item;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RssFeedItem>.Filter.Eq(item => item.Id, id);
        await _items.DeleteOneAsync(filter, cancellationToken);
    }
}
