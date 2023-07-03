namespace RssFeeder.Console.FeedBuilders;

internal class NoahReportFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public NoahReportFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities, unlaunchClient)
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
        // #link-70828 > a
        GetNodeLinks("headlines", "div.widget-area-top-2 > div.sl-links-main", "a", list, false);

        // Above the Fold section
        GetNodeLinks("above the fold", "div.widget-area-top-1 > div.sl-links-main", "a", list, false);

        // Column 1
        // #link-70828 > a
        GetNodeLinks("column 1", "div.homepage-column-1 > div.sl-links-main", "a", list, false);

        // Column 2
        // #link-70828 > a
        GetNodeLinks("column 2", "div.homepage-column-2 > div.sl-links-main", "a", list, false);

        // Column 3
        // #link-70828 > a
        GetNodeLinks("column 3", "div.homepage-column-3 > div.sl-links-main", "a", list, false);

        return list;
    }
}
