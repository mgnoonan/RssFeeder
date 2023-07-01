namespace RssFeeder.Console.FeedBuilders;

class DrudgeReportFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private readonly IUnlaunchClient _unlaunchClient;
    private Serilog.Events.LogEventLevel _logLevel = Serilog.Events.LogEventLevel.Information;

    public DrudgeReportFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities)
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
        var items = GenerateRssFeedItemList(html, feedFilters ?? new List<string>(), feedUrl);
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string html, List<string> filters, string feedUrl)
    {
        var list = new List<RssFeedItem>();

        var doc = new HtmlDocument();
        doc.Load(new StringReader(html));

        // Centered main headline(s)

        var nodes = doc.DocumentNode.SelectNodes("//center");
        int count = 1;
        foreach (HtmlNode link in nodes)
        {
            if (!link.InnerHtml.Contains("MAIN HEADLINE"))
            {
                continue;
            }

            var nodeList = link.Descendants("a").ToList();
            foreach (var node in nodeList)
            {
                var item = CreateNodeLinks(filters, node, "main headlines", count++, feedUrl, true);
                if (item != null)
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }

            // Get out of the loop, there are no more headlines
            break;
        }


        // Above the fold top headlines

        nodes = doc.DocumentNode.SelectNodes("/html/body/tt/b/tt/b/a[@href]");
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                var item = CreateNodeLinks(filters, node, "above the fold", count++, feedUrl, true);
                if (item != null)
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }


        // Left column articles

        nodes = doc.DocumentNode.SelectNodes("//table/tr/td[1]/tt/b/a[@href]");
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                if (title == "FRONT PAGES UK" || title == "BOXOFFICE")
                {
                    break;
                }

                var item = CreateNodeLinks(filters, node, "left column", count++, feedUrl, false);
                if (item != null)
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }


        // Middle column articles

        nodes = doc.DocumentNode.SelectNodes("//table/tr/td[3]/tt/b/a[@href]");
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                if (title == "WORLD SICK MAP..." || title == "3 AM GIRLS")
                {
                    break;
                }

                var item = CreateNodeLinks(filters, node, "middle column", count++, feedUrl, false);
                if (item != null)
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }


        // Right column articles

        nodes = doc.DocumentNode.SelectNodes("//table/tr/td[5]/tt/b/a[@href]");
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                if (title == "UPDATE:  DRUDGE APP IPHONE, IPAD..." || title == "ANDROID...")
                {
                    break;
                }

                var item = CreateNodeLinks(filters, node, "right column", count++, feedUrl, false);
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
