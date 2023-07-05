namespace RssFeeder.Console.FeedBuilders;

class LibertyDailyFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public LibertyDailyFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities, unlaunchClient)
    {
    }

    public List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html)
    {
        // Find out which feature flag variation we are using to log activity
        string key = "feed-log-level";
        string identity = feed.CollectionName;
        string variation = _unlaunchClient.GetVariation(key, identity);
        _log.Information("Unlaunch {key} returned variation {variation} for identity {identity}", key, variation, identity);

        _logLevel = variation switch
        {
            "debug" => Serilog.Events.LogEventLevel.Debug,
            "information" => Serilog.Events.LogEventLevel.Information,
            _ => throw new ArgumentException("Unexpected variation")
        };

        return GenerateRssFeedItemList(feed.CollectionName, feed.Url, feed.Filters, html);
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string feedCollectionName, string feedUrl, List<string> feedFilters, string html)
    {
        Initialize(feedUrl, feedFilters, html);
        var items = GenerateRssFeedItemList(html);
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string html)
    {
        var list = new List<RssFeedItem>();

        // Featured headline(s)
        // div[@class=\"drudgery-featured\"]/div/a
        GetNodeLinks("headlines", "div.drudgery-featured", "div.drudgery-featured-link > a", list, false);

        // Above the fold headline(s)
        // div[@class=\"drudgery-top-links\"]/div/a
        GetNodeLinks("above the fold", "div.drudgery-top-links", "div.drudgery-top-link > a", list, false);

        // Column 1 Articles
        // #main > div.drudgery-column-1 > div:nth-child(2) > div:nth-child(1) > a
        GetNodeLinks("column 1", "div.drudgery-column-1 > div.drudgery-articles", "div.drudgery-link > a", list, false);

        // Column 2 Articles
        // #main > div.drudgery-column-2 > div:nth-child(2) > div:nth-child(1) > a
        GetNodeLinks("column 2", "div.drudgery-column-2 > div.drudgery-articles", "div.drudgery-link > a", list, false);

        // Column 3 Articles
        // #main > div.drudgery-column-3 > div:nth-child(2) > div:nth-child(1) > a
        GetNodeLinks("column 3", "div.drudgery-column-3 > div.drudgery-articles", "div.drudgery-link > a", list, false);

        return list;
    }
}
