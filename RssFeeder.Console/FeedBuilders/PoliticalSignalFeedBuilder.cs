namespace RssFeeder.Console.FeedBuilders;

internal class PoliticalSignalFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public PoliticalSignalFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
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

        // Main Headlines section
        // #featured_post_1 > h2 > a
        var containers = document.QuerySelectorAll("#home_page_featured");
        _log.Information("FOUND: {count} sections", containers.Count());

        if (containers != null)
        {
            foreach (var c in containers)
            {
                var nodes = c.QuerySelectorAll("li > h2 > a");
                if (nodes?.Length > 0)
                {
                    count = 1;
                    foreach (var node in nodes)
                    {
                        var item = CreateNodeLinks(filters, node, "main headlines", count++, feedUrl, false);
                        if (item != null)
                        {
                            _log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                            list.Add(item);
                        }
                    }
                }
            }
        }

        // Column 1 section
        containers = document.QuerySelectorAll("#column_1");
        _log.Information("FOUND: {count} sections", containers.Count());

        if (containers != null)
        {
            foreach (var c in containers)
            {
                var nodes = c.QuerySelectorAll("a");
                if (nodes?.Length > 0)
                {
                    count = 1;
                    string text = "";
                    foreach (var node in nodes)
                    {
                        if (String.Join(' ', node.ParentElement.ClassList).Contains("mf-headline"))
                        {
                            text = node.Text();
                        }
                        else
                        {
                            node.TextContent = text;
                            var item = CreateNodeLinks(filters, node, "column 1", count++, feedUrl, false);
                            if (item != null)
                            {
                                _log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                                list.Add(item);
                            }
                        }
                    }
                }
            }
        }

        return list;
    }
}
