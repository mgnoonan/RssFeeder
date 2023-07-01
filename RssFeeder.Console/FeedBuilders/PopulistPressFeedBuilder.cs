namespace RssFeeder.Console.FeedBuilders;

class PopulistPressFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private readonly IUnlaunchClient _unlaunchClient;

    public PopulistPressFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities)
    {
        _unlaunchClient = unlaunchClient;
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

        // Above the Fold section
        var container = document.QuerySelector("#home_page_breaking");
        if (container != null)
        {
            var nodes = container.QuerySelectorAll("a");
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
        container = document.QuerySelector("#home_page_featured");
        if (container != null)
        {
            var nodes = container.QuerySelectorAll("a");
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
        container = document.QuerySelector("#column_1");
        if (container != null)
        {
            var pairedContainers = container.QuerySelectorAll("ul > li > h2");
            count = 1;
            foreach (var pairedContainer in pairedContainers)
            {
                var nodeTitle = pairedContainer.QuerySelector("span.mf-headline > a");
                var nodeLink = pairedContainer.QuerySelector("span.iconbox > a");

                if (nodeLink == null)
                    continue;

                var item = CreatePairedNodeLinks(_feedFilters, nodeTitle, nodeLink, "column 1", count++, _feedUrl, false);

                // Unfortunately the reference site links are included in the column links, so the
                // AMERICAN THINKER link signals the end of the article list in column 1
                if (item.FeedAttributes.Url.Contains("americanthinker.com"))
                    break;

                if (item != null && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 2
        container = document.QuerySelector("#column_2");
        if (container != null)
        {
            var pairedContainers = container.QuerySelectorAll("ul > li > h2");
            count = 1;
            foreach (var pairedContainer in pairedContainers)
            {
                var nodeTitle = pairedContainer.QuerySelector("span.mf-headline > a");
                var nodeLink = pairedContainer.QuerySelector("span.iconbox > a");

                if (nodeLink == null)
                    continue;

                var item = CreatePairedNodeLinks(_feedFilters, nodeTitle, nodeLink, "column 2", count++, _feedUrl, false);

                // Unfortunately the reference site links are included in the column links, so the
                // CINDY ADAMS link signals the end of the article list in column 2
                if (item.FeedAttributes.Url.Contains("cindy-adams"))
                    break;

                if (item != null && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 3
        container = document.QuerySelector("#column_3");
        if (container != null)
        {
            var pairedContainers = container.QuerySelectorAll("ul > li > h2");
            count = 1;
            foreach (var pairedContainer in pairedContainers)
            {
                var nodeTitle = pairedContainer.QuerySelector("span.mf-headline > a");
                var nodeLink = pairedContainer.QuerySelector("span.iconbox > a");

                if (nodeLink == null)
                    continue;

                var item = CreatePairedNodeLinks(_feedFilters, nodeTitle, nodeLink, "column 3", count++, _feedUrl, false);

                // Unfortunately the reference site links are included in the column links, so the
                // PRIVACY POLICY link signals the end of the article list in column 2
                if (item.FeedAttributes.Url.Contains("privacy-policy-2"))
                    break;

                if (item != null && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        return list;
    }
}