namespace RssFeeder.Console.FeedBuilders;

internal class PoliticalSignalFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public PoliticalSignalFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities, unlaunchClient)
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

        // Main Headlines section
        // #featured_post_1 > h2 > a
        GetNodeLinks("headlines", "#home_page_featured", "li > h2 > a", list, false);

        // Column 1 section
        // #main_feed_post_1 > h2 > span.iconbox > a
        GetNodeLinks("column 1", "#column_1", "span.mf-headline > a", "h2 > span.iconbox > a", list, false);

        //// Column 2 section
        GetNodeLinks("column 2", "#column_2", "span.mf-headline > a", "h2 > span.iconbox > a", list, false);

        //// Column 3 section
        GetNodeLinks("column 3", "#column_3", "span.mf-headline > a", "h2 > span.iconbox > a", list, false);

        return list;
    }
}
