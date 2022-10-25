namespace RssFeeder.Console.FeedBuilders;

internal class WhatFingerFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private readonly IUnlaunchClient _client;
    private int _articleMaxCount;

    public WhatFingerFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient client) : base(log, webUtilities, utilities)
    {
        _client = client;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html)
    {
        // Find out which feature flag variation we are using to crawl articles
        string key = "article-count-limit";
        string identity = feed.CollectionName;
        string variation = _client.GetVariation(key, identity);
        _log.Information("Unlaunch {key} returned variation {variation} for identity {identity}", key, variation, identity);

        _articleMaxCount = variation switch
        {
            "high" => 50,
            "medium" => 30,
            "low" => 20,
            "unlimited" => 1000,
            _ => throw new ArgumentException("Unexpected variation")
        };
        _log.Information("Processing a maximum of {articleMaxCount} articles", _articleMaxCount);

        return GenerateRssFeedItemList(feed.CollectionName, feed.Url, feed.Filters, html);
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string feedCollectionName, string feedUrl, List<string> feedFilters, string html)
    {
        var items = GenerateRssFeedItemList(html, feedFilters ?? new List<string>(), feedUrl);
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string html, List<string> filters, string feedUrl)
    {
        var list = new List<RssFeedItem>();
        int count;

        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Main Headlines section
        // div.creative-link.wpb_column.vc_column_container.vc_col-sm-8 > div > div > div
        var containers = document.QuerySelectorAll("div.creative-link.wpb_column.vc_column_container.vc_col-sm-8 > div > div");
        _log.Information("FOUND: {count} sections", containers.Count());

        if (containers != null)
        {
            foreach (var c in containers)
            {
                var nodes = c.QuerySelectorAll("ul li a");
                if (nodes?.Length > 0)
                {
                    count = 1;
                    foreach (var node in nodes.Take(_articleMaxCount))
                    {
                        var item = CreateNodeLinks(filters, node, "main headlines", count++, feedUrl, false);
                        if (item != null)
                        {
                            _log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                            list.Add(item);
                        }
                    }

                    break;
                }
            }
        }

        return list;
    }
}
