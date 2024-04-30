namespace RssFeeder.Console.FeedBuilders;

internal class WhatFingerFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public WhatFingerFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities, unlaunchClient)
    {
    }

    public List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html)
    {
        // Find out which feature flag variation we are using to crawl articles
        _articleMaxCount = 20;

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
        // div.creative-link.wpb_column.vc_column_container.vc_col-sm-8 > div > div > div:nth-child(6) > div > h4:nth-child(1) > span > a
        GetNodeLinks("headlines", "div.creative-link.wpb_column.vc_column_container.vc_col-sm-8 > div > div > div:nth-child(6) > div", "ul li a", list, true);

        // No articles found, try the next container down the stack
        if (list.Count == 0)
        {
            GetNodeLinks("headlines", "div.creative-link.wpb_column.vc_column_container.vc_col-sm-8 > div > div > div:nth-child(7) > div", "ul li a", list, true);
        }

        // No articles found, try the next container down the stack
        if (list.Count == 0)
        {
            GetNodeLinks("headlines", "div.creative-link.wpb_column.vc_column_container.vc_col-sm-8 > div > div > div:nth-child(8) > div", "ul li a", list, true);
        }

        return list;
    }
}
