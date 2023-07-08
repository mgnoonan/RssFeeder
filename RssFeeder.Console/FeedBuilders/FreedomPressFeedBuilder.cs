namespace RssFeeder.Console.FeedBuilders;

internal class FreedomPressFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public FreedomPressFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(logger, webUtilities, utilities, unlaunchClient)
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

        // Main headlines section
        // #fg-widget-65cf26586c253334929d032bf > div.uw-sc-mask > div.uw-sc-cardcont > div.uw-card2.uw-headline > a
        GetNodeLinks("headlines", "#fg-widget-65cf26586c253334929d032bf > div.uw-sc-mask > div.uw-sc-cardcont", "div > a", list, false);

        // Column 1 section
        // #fg-widget-fd07a65186f463b9a3ff917c1 > div.uw-sc-mask > div.uw-sc-cardcont > div.uw-card2.uw-headline > a
        GetNodeLinks("column 1", "#fg-widget-fd07a65186f463b9a3ff917c1 > div.uw-sc-mask > div.uw-sc-cardcont", "div > a", list, false);

        // Column 2 section
        // #fg-widget-f9e047dd6b8da3cdc21edad8d > div.uw-sc-mask > div.uw-sc-cardcont > div.uw-card2.uw-headline > a
        GetNodeLinks("column 2", "#fg-widget-f9e047dd6b8da3cdc21edad8d > div.uw-sc-mask > div.uw-sc-cardcont", "div > a", list, false);

        // Column 3 section
        // #fg-widget-a52b0845e1da669a0aa917645 > div.uw-sc-mask > div.uw-sc-cardcont > div.uw-card2.uw-headline > a
        GetNodeLinks("column 3", "#fg-widget-a52b0845e1da669a0aa917645 > div.uw-sc-mask > div.uw-sc-cardcont", "div > a", list, false);

        return list;
    }
}
