namespace RssFeeder.Console.FeedBuilders;

class BonginoReportFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public BonginoReportFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
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

        // Top Stories section
        // //section.banner > div > div > div.col-12.col-sm-8 > div > div > div > h5 > a
        var container = document.QuerySelector("section.banner");
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
                    log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Top Stories section
        // //section.top-stories > div > div > div.col-12.col-sm-8 > div > div > div > h5 > a
        container = document.QuerySelector("section.top-stories");
        nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                string title = WebUtility.HtmlDecode(node.Text().Trim());

                var item = CreateNodeLinks(filters, node, "top stories", count++, feedUrl);
                if (item != null)
                {
                    log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // All Stories section
        // //section.all-stories > div > div > div.col-12.col-sm-8 > div > div > div > h5 > a
        container = document.QuerySelector("section.all-stories");
        nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                string title = WebUtility.HtmlDecode(node.Text().Trim());

                var item = CreateNodeLinks(filters, node, "all stories", count++, feedUrl);
                if (item != null)
                {
                    log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Video Stories section
        // //section.stories-video > div > div > div.col-12.col-sm-8 > div > div > div > h5 > a
        container = document.QuerySelector("section.stories-video");
        nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                string title = WebUtility.HtmlDecode(node.Text().Trim());

                var item = CreateNodeLinks(filters, node, "video stories", count++, feedUrl);
                if (item != null)
                {
                    log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        return list;
    }
}
