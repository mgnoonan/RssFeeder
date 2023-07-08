﻿namespace RssFeeder.Console.FeedBuilders;

class CitizenFreePressFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public CitizenFreePressFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities, unlaunchClient)
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

        // Main Headlines section
        GetNodeLinks("headlines", "#featured", "a.headline-link", list, false);

        // Above the Fold section
        GetNodeLinks("above the fold", "ul.wpd-top-links", "a.headline-link", list, false);

        // Column 1
        GetNodeLinks("column 1", "#column-1", "a.headline-link", list, false);

        // Column 2
        GetNodeLinks("column 2", "#column-2", "a.headline-link", list, false);

        // Column 3
        GetNodeLinks("column 3", "#column-3", "a.headline-link", list, false);

        return list;
    }
}
