namespace RssFeeder.Console.FeedBuilders;

class LibertyDailyFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public LibertyDailyFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities, unlaunchClient)
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
        var doc = new HtmlDocument();
        doc.Load(new StringReader(html));

        // Above the fold headline(s)
        // //div[@class=\"drudgery-top-links\"]/div/a
        var nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-top-links\"]/div/a");
        int count;
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                var item = CreateNodeLinks(_feedFilters, node, "above the fold", count++, _feedUrl, true);
                if (item != null)
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Featured headline(s)
        // //div[@class=\"drudgery-featured\"]/div/a
        nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-featured\"]/div/a");
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                var item = CreateNodeLinks(_feedFilters, node, "main headlines", count++, _feedUrl, true);
                if (item != null)
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 1 Articles
        // //div[@class=\"drudgery-column-1\"]/div[@class=\"drudgery-articles\"]/div/a
        // #main > div.drudgery-column-1 > div:nth-child(2) > div:nth-child(1) > a
        nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-column-1\"]/div[@class=\"drudgery-articles\"]/div/a");
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                var item = CreateNodeLinks(_feedFilters, node, "left column", count++, _feedUrl, false);
                if (item != null)
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 2 Articles
        // //div[@class=\"drudgery-column-2\"]/div[@class=\"drudgery-articles\"]/div/a
        // #main > div.drudgery-column-2 > div:nth-child(2) > div:nth-child(1) > a
        nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-column-2\"]/div[@class=\"drudgery-articles\"]/div/a");
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                var item = CreateNodeLinks(_feedFilters, node, "middle column", count++, _feedUrl, false);
                if (item != null)
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 3 Articles
        // //div[@class=\"drudgery-column-3\"]/div[@class=\"drudgery-articles\"]/div/a
        // #main > div.drudgery-column-3 > div:nth-child(2) > div:nth-child(1) > a
        nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-column-3\"]/div[@class=\"drudgery-articles\"]/div/a");
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                var item = CreateNodeLinks(_feedFilters, node, "right column", count++, _feedUrl, false);
                if (item != null)
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        return list;
    }
}
