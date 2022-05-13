namespace RssFeeder.Console.FeedBuilders;

class BaseFeedBuilder
{
    protected readonly ILogger log;
    readonly IWebUtils webUtils;
    readonly IUtils utils;

    public BaseFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities)
    {
        log = logger;
        webUtils = webUtilities;
        utils = utilities;
    }

    protected void PostProcessing(string feedCollectionName, string feedUrl, List<RssFeedItem> items)
    {
        log.Information("FOUND {count} articles in {url}", items.Count, feedUrl);

        // Replace any relative paths and add the feed id
        foreach (var item in items)
        {
            item.FeedAttributes.FeedId = feedCollectionName;

            // if (item.FeedAttributes.Url.StartsWith("/"))
            // {
            //     item.FeedAttributes.Url = feedUrl + item.FeedAttributes.Url;
            // }
        }
    }

    protected RssFeedItem CreateNodeLinks(List<string> filters, IElement node, string location, int count, string feedUrl)
    {
        string title = WebUtility.HtmlDecode(node.Text().Trim());

        // Replace all errant spaces, which sometimes creep into Drudge's URLs
        var attr = node.Attributes.GetNamedItem("href");
        string linkUrl = attr.Value.Trim().Replace(" ", string.Empty);

        return CreateNodeLinks(filters, location, count, title, linkUrl, feedUrl);
    }

    protected RssFeedItem CreatePairedNodeLinks(List<string> filters, IElement nodeTitleOnly, IElement nodeLinkOnly, string location, int count, string feedUrl)
    {
        string title = WebUtility.HtmlDecode(nodeTitleOnly.Text().Trim());

        // Replace all errant spaces, which sometimes creep into Drudge's URLs
        var attr = nodeLinkOnly.Attributes.GetNamedItem("href");
        string linkUrl = attr.Value.Trim().Replace(" ", string.Empty);

        return CreateNodeLinks(filters, location, count, title, linkUrl, feedUrl);
    }

    protected RssFeedItem CreateNodeLinks(List<string> filters, HtmlNode node, string location, int count, string feedUrl)
    {
        string title = WebUtility.HtmlDecode(node.InnerText.Trim());

        try
        {
            HtmlAttribute attr = node.Attributes["href"];

            // Replace all errant spaces, which sometimes creep into Drudge's URLs
            string linkUrl = attr.Value.Trim().Replace(" ", string.Empty);

            return CreateNodeLinks(filters, location, count, title, linkUrl, feedUrl);
        }
        catch (NullReferenceException)
        {
            log.Warning("Unable to resolve reference for location '{location}':'{title}'", location, title);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Error encountered reading location '{location}':{count}", location, count);
        }

        return null;
    }

    private RssFeedItem CreateNodeLinks(List<string> filters, string location, int count, string title, string linkUrl, string feedUrl)
    {
        // Sometimes Drudge has completely empty links, ignore them
        if (string.IsNullOrEmpty(linkUrl))
        {
            return null;
        }

        // Repair any protocol typos if possible
        if (!linkUrl.ToLower().StartsWith("http"))
        {
            linkUrl = webUtils.RepairUrl(linkUrl, feedUrl);
            //log.Information("Invalid link url {url}", linkUrl);
            //return null;
        }

        var uri = new Uri(linkUrl);

        // Calculate the MD5 hash for the link so we can be sure of uniqueness
        string hash = utils.CreateMD5Hash(uri.AbsoluteUri.ToLower());
        if (filters.Contains(hash))
        {
            log.Information("Hash '{hash}':'{uri}' found in filter list", hash, uri);
            return null;
        }

        if (linkUrl.Length > 0 && title.Length > 0)
        {
            return new RssFeedItem()
            {
                FeedAttributes = new FeedAttributes()
                {
                    Title = WebUtility.HtmlDecode(title),
                    Url = uri.AbsoluteUri,
                    UrlHash = hash,
                    DateAdded = DateTime.Now.ToUniversalTime(),
                    LinkLocation = $"{location}, article {count}",
                    IsUrlShortened = uri.Host.ToLower() == "t.co"
                }
            };
        }

        return null;
    }
}
