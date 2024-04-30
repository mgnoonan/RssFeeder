namespace RssFeeder.Console.FeedBuilders;

internal class RevolverNewsFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public RevolverNewsFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities) :
        base(logger, webUtilities, utilities)
    {
    }

    public List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html)
    {
        // Find out which feature flag variation we are using to log activity
        _logLevel = Serilog.Events.LogEventLevel.Debug;

        return GenerateRssFeedItemList(feed.CollectionName, feed.Url, feed.Filters, html);
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string feedCollectionName, string feedUrl, List<string> feedFilters, string html)
    {
        Initialize(feedUrl, feedFilters, html);
        var items = GenerateRssFeedItemList();
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList()
    {
        var list = new List<RssFeedItem>();

        // Stories section
        GetNodeLinks("stories", "div.list-articles", "article.item > div.text > h2.title > a", list, false);

        return list;
    }
}
