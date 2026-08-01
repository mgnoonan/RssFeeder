namespace RssFeeder.Console.FeedBuilders;

internal class RubinReportFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public RubinReportFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
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
        // #rt-tpg-container-450400301 > div.rt-row.rt-content-loader.grid-layout1.grid-behaviour.tpg-full-height.grid_layout_wrapper > div.rt-col-md-4.rt-col-sm-6.rt-col-xs-12.default.rt-grid-item.post-2462.post.type-post.status-publish.format-standard.has-post-thumbnail.hentry.category-uncategorized > div > div > div.post-footer > div > div > a
        GetNodeLinks("articles", "body", "h3.entry-title > a", list, false);

        return list;
    }
}
