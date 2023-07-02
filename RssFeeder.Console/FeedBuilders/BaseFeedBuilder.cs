using AngleSharp.Html.Dom;

namespace RssFeeder.Console.FeedBuilders;

class BaseFeedBuilder
{
    protected readonly ILogger _log;
    protected readonly IUnlaunchClient _unlaunchClient;
    readonly IWebUtils _webUtils;
    readonly IUtils _utils;
    protected Serilog.Events.LogEventLevel _logLevel = Serilog.Events.LogEventLevel.Information;
    protected List<string> _feedFilters = new List<string>();
    protected string _feedUrl;
    protected IHtmlDocument _document;

    public BaseFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient)
    {
        _log = logger;
        _webUtils = webUtilities;
        _utils = utilities;
        _unlaunchClient = unlaunchClient;
    }

    protected void Initialize(string feedUrl, List<string> feedFilters, string html)
    {
        _feedFilters = feedFilters ?? new List<string>();
        _feedUrl = feedUrl ?? string.Empty;

        // Load and parse the html from the source file
        var parser = new HtmlParser();
        _document = parser.ParseDocument(html);
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

    protected void GetNodeLinks(string sectionName, string containerSelector, string linkSelector, List<RssFeedItem> list)
    {
        var containers = _document.QuerySelectorAll(containerSelector);
        if (containers is null)
        {
            _log.Warning("Containers not found {containerSelector}", containerSelector);
            return;
        }

        _log.Information("FOUND: {sectionName} section with {containerCount} containers", sectionName, containers.Length);
        foreach (var container in containers)
        {
            GetNodeLinks(container, sectionName, linkSelector, list);
        }
    }

    protected void GetNodeLinks(IElement container, string sectionName, string linkSelector, List<RssFeedItem> list)
    {
        if (container is null)
        {
            _log.Warning("Container not found for section {sectionName}", sectionName);
            return;
        }

        _log.Information("Parsing container {containerSelector}", container.GetSelector());

        var nodes = container.QuerySelectorAll(linkSelector);
        if (nodes?.Length > 0)
        {
            int count = 1;
            foreach (var node in nodes)
            {
                var item = CreateNodeLinks(_feedFilters, node, sectionName, count++, _feedUrl, true);
                if (item != null)
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }
    }
}
