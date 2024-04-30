namespace RssFeeder.Console.FeedBuilders;

class TwitchyFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public TwitchyFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities) : base(logger, webUtilities, utilities)
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

        // Main Headlines section
        GetNodeLinks("headlines", "section.pt-2", "div.wp-card-huge__title > a", list, false);

        // Above the fold section
        GetNodeLinks("above the fold", "body>main>section:nth-child(2)", "div.wp-card__title > a", list, false);

        // Posts section
        GetNodeLinks("posts", "#post-list", "div.wp-card__title > a", list, false);

        return list;
    }
}
