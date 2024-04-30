namespace RssFeeder.Console.FeedBuilders;

internal class OffThePressFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public OffThePressFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
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
        // #main > div.section.features > div > div > div.col-md-offset-1.col-sm-offset-1.col-xs-offset-0.col-md-10.col-sm-10.col-xs-12.featured-post > a:nth-child(3)
        GetNodeLinks("headlines", "div.featured-post", "a", list, false);

        // Posts section
        GetNodeLinks("posts", "#post-list", "a", list, false);

        return list;
    }
}
