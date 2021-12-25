namespace RssFeeder.Console.FeedBuilders;

class CitizenFreePressFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public CitizenFreePressFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
    { }

    public List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html)
    {
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

        // Above the Fold section
        var container = document.QuerySelector("ul.wpd-top-links");
        var nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                string title = WebUtility.HtmlDecode(node.Text().Trim());

                var item = CreateNodeLinks(filters, node, "above the fold", count++, feedUrl);
                if (item != null)
                {
                    log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Main Headlines section
        container = document.QuerySelector("#featured");
        if (container != null)
        {
            nodes = container.QuerySelectorAll("a");
            if (nodes != null)
            {
                count = 1;
                foreach (var node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.Text().Trim());

                    var item = CreateNodeLinks(filters, node, "main headlines", count++, feedUrl);
                    if (item != null && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                    {
                        log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                        list.Add(item);
                    }
                }
            }
        }

        // Column 1
        container = document.QuerySelector("#column-1");
        nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                string title = WebUtility.HtmlDecode(node.Text().Trim());

                var item = CreateNodeLinks(filters, node, "column 1", count++, feedUrl);
                if (item != null && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                {
                    log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 2
        container = document.QuerySelector("#column-2");
        nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                string title = WebUtility.HtmlDecode(node.Text().Trim());

                var item = CreateNodeLinks(filters, node, "column 2", count++, feedUrl);
                if (item != null && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                {
                    log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 3
        container = document.QuerySelector("#column-3");
        nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                string title = WebUtility.HtmlDecode(node.Text().Trim());

                var item = CreateNodeLinks(filters, node, "column 3", count++, feedUrl);
                if (item != null && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                {
                    log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        return list;
    }
}
