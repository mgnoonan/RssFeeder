using System.Linq;

namespace RssFeeder.Console.FeedBuilders;

internal class FreedomPressFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public FreedomPressFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities) : base(logger, webUtilities, utilities)
    {
    }

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

        // Featured links section
        var container = document.QuerySelector("#home-section > ul");
        var nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                string title = WebUtility.HtmlDecode(node.Text().Trim());

                var item = CreateNodeLinks(filters, node, "headlines", count++, feedUrl);
                if (item != null)
                {
                    log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Stories section
        var containers = document.QuerySelectorAll("#home-section > div.columns");
        string[] sectionName = new string[] { "first", "previous banner", "second", "third" };
        int sectionCounter = 0;
        foreach (var element in containers.Take(3))
        {
            nodes = element.QuerySelectorAll("a");
            if (nodes != null)
            {
                count = 1;
                foreach (var node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.Text().Trim());

                    var item = CreateNodeLinks(filters, node, $"{sectionName[sectionCounter]} section", count++, feedUrl);
                    if (item != null)
                    {
                        log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                        list.Add(item);
                    }
                }
            }

            sectionCounter++;
        }

        return list;
    }
}
