namespace RssFeeder.Mvc.Services;

public interface IFeedDefinitionProvider
{
    IReadOnlyList<FeedModel> GetFeeds();
    FeedModel GetFeed(string collectionName);
    bool FeedExists(string collectionName);
}