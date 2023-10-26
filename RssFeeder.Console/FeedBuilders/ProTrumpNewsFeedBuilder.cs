namespace RssFeeder.Console.FeedBuilders;

internal class ProTrumpNewsFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public ProTrumpNewsFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(logger, webUtilities, utilities, unlaunchClient)
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
