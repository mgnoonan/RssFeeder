namespace RssFeeder.Console.FeedBuilders;

class DrudgeReportFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public DrudgeReportFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities, unlaunchClient)
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

        // Centered main headline(s)
        // body > tt > b > tt > b > center
        GetNodeLinks("headlines", "body > tt > b > tt > b > center", "a", list, false);

        // Above the fold top headlines
        // body > tt > b > tt > b > a:nth-child(5)
        GetNodeLinks("above the fold", "body > tt > b > tt", "b > a", list, false);

        // Left column articles
        // body > font > font > center > table > tbody > tr > td:nth-child(1) > tt > b > a:nth-child(1)
        GetNodeLinks("column 1", "table > tbody > tr > td:nth-child(1) > tt", "b > a", list, false, "1dc7f1c814187b538e82d9d56fd4f66d");

        // Middle column articles
        GetNodeLinks("column 2", "table > tbody > tr > td:nth-child(3) > tt", "b > a", list, false, "4099925931f0e142ca280f22343236e3");

        // Right column articles
        GetNodeLinks("column 3", "table > tbody > tr > td:nth-child(5) > tt", "b > a", list, false, "cb1d91b100c1732694cf8a55e39ad2d2");

        return list;
    }
}
