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
        var items = GenerateRssFeedItemList(html);
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string html)
    {
        var list = new List<RssFeedItem>();
        int count;

        // Above the Fold section
        var container = _document.QuerySelector("ul.wpd-top-links");
        var nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                var item = CreateNodeLinks(_feedFilters, node, "above the fold", count++, _feedUrl, true);
                if (item != null)
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Main Headlines section
        container = _document.QuerySelector("#featured");
        if (container != null)
        {
            nodes = container.QuerySelectorAll("a");
            if (nodes != null)
            {
                count = 1;
                foreach (var node in nodes)
                {
                    var item = CreateNodeLinks(_feedFilters, node, "main headlines", count++, _feedUrl, true);
                    if (item != null && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                    {
                        _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                        list.Add(item);
                    }
                }
            }
        }

        // Column 1
        container = _document.QuerySelector("#column-1");
        nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                var item = CreateNodeLinks(_feedFilters, node, "column 1", count++, _feedUrl, false);
                if (item != null && !item.FeedAttributes.Url.Contains("/column-1/") && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 2
        container = _document.QuerySelector("#column-2");
        nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                var item = CreateNodeLinks(_feedFilters, node, "column 2", count++, _feedUrl, false);
                if (item != null && !item.FeedAttributes.Url.Contains("/column-2/") && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 3
        container = _document.QuerySelector("#column-3");
        nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                var item = CreateNodeLinks(_feedFilters, node, "column 3", count++, _feedUrl, false);
                if (item != null && !item.FeedAttributes.Url.Contains("/column-3/") && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        return list;
    }
}
