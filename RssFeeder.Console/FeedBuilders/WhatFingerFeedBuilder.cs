namespace RssFeeder.Console.FeedBuilders;

internal class WhatFingerFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private int _articleMaxCount;
    private readonly IUnlaunchClient _unlaunchClient;

    public WhatFingerFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities)
    {
        _unlaunchClient = unlaunchClient;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html)
    {
        // Find out which feature flag variation we are using to crawl articles
        string key = "article-count-limit";
        string identity = feed.CollectionName;
        string variation = _unlaunchClient.GetVariation(key, identity, new List<UnlaunchAttribute>
        {
            UnlaunchAttribute.NewBoolean("weekend", DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
        });
        _log.Information("Unlaunch {key} returned variation {variation} for identity {identity}", key, variation, identity);

        _articleMaxCount = variation switch
        {
            "high" => 25,
            "medium" => 20,
            "low" => 15,
            "unlimited" => 1000,
            _ => throw new ArgumentException("Unexpected variation")
        };
        _log.Information("Processing a maximum of {articleMaxCount} articles", _articleMaxCount);

        // Find out which feature flag variation we are using to log activity
        key = "feed-log-level";
        identity = feed.CollectionName;
        variation = _unlaunchClient.GetVariation(key, identity);
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
        _feedFilters = feedFilters ?? new List<string>();
        _feedUrl = feedUrl ?? string.Empty;

        var items = GenerateRssFeedItemList(html);
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string html)
    {
        var list = new List<RssFeedItem>();
        int count;

        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Main Headlines section
        // div.creative-link.wpb_column.vc_column_container.vc_col-sm-8 > div > div > div
        var containers = document.QuerySelectorAll("div.creative-link.wpb_column.vc_column_container.vc_col-sm-8 > div > div");
        _log.Information("FOUND: {count} sections", containers.Length);

        if (containers != null)
        {
            foreach (var c in containers)
            {
                var nodes = c.QuerySelectorAll("ul li a");
                if (nodes?.Length > 0)
                {
                    count = 1;
                    string previousHash = "";
                    foreach (var node in nodes.Take(_articleMaxCount))
                    {
                        var item = CreateNodeLinks(_feedFilters, node, "main headlines", count, _feedUrl, false);
                        if (item != null && item.FeedAttributes.UrlHash != previousHash)
                        {
                            _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                            list.Add(item);
                            count++;
                        }

                        previousHash = item?.FeedAttributes.UrlHash ?? "";
                    }

                    break;
                }
            }
        }

        return list;
    }
}
