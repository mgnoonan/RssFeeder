namespace RssFeeder.Console.FeedBuilders;

class BadBlueFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public BadBlueFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
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

        // Main headlines section
        var container = document.QuerySelector("div.storyh1 > span");
        var nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                string title = WebUtility.HtmlDecode(node.Text().Trim());

                var item = CreateNodeLinks(filters, node, "main headlines", count++, feedUrl);
                if (item != null)
                {
                    log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Stories section
        container = document.QuerySelector("div.grid");
        nodes = container.QuerySelectorAll("div.headlines > p > a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                string title = WebUtility.HtmlDecode(node.Text().Trim());

                var item = CreateNodeLinks(filters, node, "all stories", count++, feedUrl);
                if (item != null)
                {
                    log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        return list;
    }
}
