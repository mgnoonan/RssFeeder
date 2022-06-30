namespace RssFeeder.Console.FeedBuilders;

internal class RevolverNewsFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public RevolverNewsFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities) :
        base(logger, webUtilities, utilities)
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

        //// Featured links section
        //var container = document.QuerySelector("div.revolver > div.column.center > div.post-listing");
        //var nodes = container.QuerySelectorAll("div.title > a");
        //if (nodes != null)
        //{
        //    count = 1;
        //    foreach (var node in nodes)
        //    {
        //        string title = WebUtility.HtmlDecode(node.Text().Trim());

        //        var item = CreateNodeLinks(filters, node, "feature links", count++, feedUrl, true);
        //        if (item != null)
        //        {
        //            log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
        //            list.Add(item);
        //        }
        //    }
        //}

        // Stories section
        var container = document.QuerySelector("div.list-articles");
        var nodes = container.QuerySelectorAll("article.item > div.text > h2.title > a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes.Take(25))
            {
                string title = WebUtility.HtmlDecode(node.Text().Trim());

                var item = CreateNodeLinks(filters, node, "news feed", count++, feedUrl, false);
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
