namespace RssFeeder.Console.FeedBuilders;

internal class ParkinsonsNewsTodayFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public ParkinsonsNewsTodayFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) 
        : base(log, webUtilities, utilities)
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

        // Featured headline
        // #content > div > div:nth-child(1) > div > div:nth-child(1) > div:nth-child(1) > div.bio-post-preview.bio-post-preview--large.bio-post-preview--vertical > div > a.bio-link.bio-link--title
        GetNodeLinks("featured headline",
            "#content > div > div:nth-child(1) > div > div:nth-child(1)", 
            "div > a.bio-link.bio-link--title", 
            list, 
            false);

        return list;
    }
}
