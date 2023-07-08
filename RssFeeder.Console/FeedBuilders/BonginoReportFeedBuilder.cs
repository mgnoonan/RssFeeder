namespace RssFeeder.Console.FeedBuilders;

class BonginoReportFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public BonginoReportFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities, unlaunchClient)
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
        var items = GenerateRssFeedItemList();
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList()
    {
        var list = new List<RssFeedItem>();

        // Top Stories section
        // //section.banner > div > div > div.col-12.col-sm-8 > div > div > div > h5 > a
        GetNodeLinks("headlines", "section.banner", "a", list, false);

        // Top Stories section
        // //section.top-stories > div > div > div.col-12.col-sm-8 > div > div > div > h5 > a
        GetNodeLinks("top stories", "section.top-stories", "a", list, false);

        // All Stories section
        // //section.all-stories > div > div > div.col-12.col-sm-8 > div > div > div > h5 > a
        GetNodeLinks("all stories", "section.all-stories", "a", list, false);

        // Video Stories section
        // //section.stories-video > div > div > div.col-12.col-sm-8 > div > div > div > h5 > a
        GetNodeLinks("video stories", "section.stories-video", "a", list, false);

        return list;
    }
}
