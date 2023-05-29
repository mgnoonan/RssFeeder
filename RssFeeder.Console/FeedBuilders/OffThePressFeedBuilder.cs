namespace RssFeeder.Console.FeedBuilders;

internal class OffThePressFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public OffThePressFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
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
        // body > div.elementor.elementor-14507.elementor-location-single.post-24.page.type-page.status-publish.has-post-thumbnail.hentry > div > section.elementor-section.elementor-top-section.elementor-element.elementor-element-2322729e.elementor-section-stretched.elementor-section-boxed.elementor-section-height-default.elementor-section-height-default
        var containers = document.QuerySelectorAll("section");
        _log.Information("FOUND: {count} sections", containers.Length);

        if (containers != null)
        {
            foreach (var c in containers)
            {
                var nodes = c.QuerySelectorAll("h3 > a");
                if (nodes?.Length > 0)
                {
                    count = 1;
                    foreach (var node in nodes)
                    {
                        var item = CreateNodeLinks(filters, node, "main headlines", count++, feedUrl, true);
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

        // Column 1
        var container = document.QuerySelector("#post-list");
        if (container != null)
        {
            var nodes = container.QuerySelectorAll("a");
            count = 1;
            foreach (var node in nodes)
            {
                var item = CreateNodeLinks(filters, node, "column 1", count++, feedUrl, false);
                if (item != null)
                {
                    _log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        return list;
    }
}
