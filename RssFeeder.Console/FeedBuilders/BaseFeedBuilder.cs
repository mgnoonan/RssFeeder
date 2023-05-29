namespace RssFeeder.Console.FeedBuilders;

class BaseFeedBuilder
{
    protected readonly ILogger _log;
    readonly IWebUtils _webUtils;
    readonly IUtils _utils;

    public BaseFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities)
    {
        _log = logger;
        _webUtils = webUtilities;
        _utils = utilities;
    }

    protected void PostProcessing(string feedCollectionName, string feedUrl, List<RssFeedItem> items)
    {
        if (items.Count > 0)
        {
            _log.Information("FOUND {count} articles in {url}", items.Count, feedUrl);
        }
        else
        {
            _log.Error("FOUND {count} articles in {url}", items.Count, feedUrl);
        }

        // Replace any relative paths and add the feed id
        foreach (var item in items)
        {
            item.FeedAttributes.FeedId = feedCollectionName;
        }
    }

    protected RssFeedItem CreatePairedNodeLinks(List<string> filters, IElement nodeTitleOnly, IElement nodeLinkOnly, string location, int count, string feedUrl, bool isHeadline)
    {
        string title = WebUtility.HtmlDecode(nodeTitleOnly.Text().Trim());

        // Replace all errant spaces, which sometimes creep into Drudge's URLs
        var attr = nodeLinkOnly.Attributes.GetNamedItem("href");
        string linkUrl = attr.Value.Trim().Replace(" ", string.Empty);

        return CreateNodeLinks(filters, location, count, title, linkUrl, feedUrl, isHeadline);
    }

    protected RssFeedItem CreateNodeLinks(List<string> filters, IElement node, string location, int count, string feedUrl, bool isHeadline)
    {
        string title = WebUtility.HtmlDecode(node.Text().Trim());

        // Replace all errant spaces, which sometimes creep into Drudge's URLs
        var attr = node.Attributes.GetNamedItem("href");
        string linkUrl = attr.Value.Trim().Replace(" ", string.Empty);

        return CreateNodeLinks(filters, location, count, title, linkUrl, feedUrl, isHeadline);
    }

    protected RssFeedItem CreateNodeLinks(List<string> filters, HtmlNode node, string location, int count, string feedUrl, bool isHeadline)
    {
        string title = WebUtility.HtmlDecode(node.InnerText.Trim());

        try
        {
            HtmlAttribute attr = node.Attributes["href"];

            // Replace all errant spaces, which sometimes creep into Drudge's URLs
            string linkUrl = attr.Value.Trim().Replace(" ", string.Empty);

            return CreateNodeLinks(filters, location, count, title, linkUrl, feedUrl, isHeadline);
        }
        catch (NullReferenceException)
        {
            _log.Warning("Unable to resolve reference for location '{location}':'{title}'", location, title);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error encountered reading location '{location}':{count}", location, count);
        }

        return null;
    }

    private RssFeedItem CreateNodeLinks(List<string> filters, string location, int count, string title, string linkUrl, string feedUrl, bool isHeadline)
    {
        // Sometimes Drudge has completely empty links, ignore them
        if (string.IsNullOrEmpty(linkUrl))
        {
            return null;
        }

        // Repair any protocol typos if possible
        if (!linkUrl.ToLower().StartsWith("http"))
        {
            linkUrl = _webUtils.RepairUrl(linkUrl, feedUrl);
        }

        if (!Uri.TryCreate(linkUrl, UriKind.Absolute, out Uri uri))
        {
            _log.Warning("Unable to parse Uri {linkUrl}", linkUrl);
            return null;
        }

        // Calculate the MD5 hash for the link so we can be sure of uniqueness
        string hash = _utils.CreateMD5Hash(uri.AbsoluteUri.ToLower());
        if (filters.Contains(hash))
        {
            _log.Information("Hash '{urlHash}':'{url}' found in filter list", hash, uri.AbsoluteUri.ToLower());
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
                    IsUrlShortened = uri.Host.ToLower() == "t.co",
                    IsHeadline = isHeadline
                }
            };
        }

        return null;
    }
}
