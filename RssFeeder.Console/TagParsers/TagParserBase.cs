using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;

namespace RssFeeder.Console.TagParsers;

public partial class TagParserBase
{
    private readonly ILogger _log;
    private readonly IUnlaunchClient _client;
    private readonly IWebUtils _webUtils;
    protected string _sourceHtml;
    protected RssFeedItem _item;

    public TagParserBase(ILogger log, IUnlaunchClient client, IWebUtils webUtils)
    {
        _log = log;
        _client = client;
        _webUtils = webUtils;
    }

    [GeneratedRegex("<br\\s?\\/?>")]
    protected static partial Regex LineBreakRegex();

    [GeneratedRegex("<p>(&nbsp;)?<\\/p>")]
    protected static partial Regex EmptyParagraphRegex();

    public void Initialize(string sourceHtml, RssFeedItem item)
    {
        _sourceHtml = sourceHtml;
        _item = item;
    }

    public virtual void PostParse()
    {
        var result = _item.HtmlAttributes?.GetValueOrDefault("ParserResult") ?? "";
        var baseUrl = _item.OpenGraphAttributes.GetValueOrDefault("og:url") ??
            _item.FeedAttributes.Url ??
            "";

        var parser = new HtmlParser();
        var document = parser.ParseDocument(result);

        // Some sites do not correctly construct their cannonical url for og:url,
        // so use the feed url as a fallback
        // NOTE: the original feed URL might be from a different site, i.e. a url shortening site
        // so using that for the baseUrl may not correctly resolve all relative references
        if (!baseUrl.StartsWith("http"))
        {
            _log.Warning("Base URL {baseUrl} is still relative, falling back to {feedUrl}", baseUrl, _item.FeedAttributes.Url);
            baseUrl = _item.FeedAttributes.Url;
        }

        if (GetVariationByKey("article-fixup-urls", _item.FeedAttributes.FeedId) == "on")
        {
            _log.Debug("Base url = {baseUrl}", baseUrl);
            FixupRelativeUrls(document, baseUrl);
        }

        if (GetVariationByKey("image-data-src-override", _item.FeedAttributes.FeedId) == "on")
        {
            FixupImageSrc(document, baseUrl);
        }

        RemoveDuplicateImgTag(document);

        // Check for embedded videos
        if (_item.SiteName != "youtube" && _item.SiteName != "rumble")
        {
            if (TryGetVideoIFrame(document, "rumble.com/embed", out IElement iframeElement))
            {
                ExtractIFrameMetadata(iframeElement);
            }
            else if (TryGetVideoIFrame(document, "bitchute.com/embed", out iframeElement))
            {
                ExtractIFrameMetadata(iframeElement);
            }
            else if (TryGetVideoIFrame(document, "youtube.com/embed", out iframeElement))
            {
                ExtractIFrameMetadata(iframeElement);
            }
        }

        _item.HtmlAttributes["ParserResult"] = document.Body.InnerHtml.Trim();
    }

    private void ExtractIFrameMetadata(IElement iframeElement)
    {
        string url = iframeElement.GetAttribute("src");
        string type = iframeElement.HasAttribute("type") ? iframeElement.GetAttribute("type") : "text/html";
        string width = iframeElement.HasAttribute("width") ? iframeElement.GetAttribute("width") : "640";
        string height = iframeElement.HasAttribute("height") ? iframeElement.GetAttribute("height") : "480";
        _log.Information("Embedded video {type} detected {url}", type, url);

        _item.OpenGraphAttributes.Add("og:x:video", url);
        _item.OpenGraphAttributes.Add("og:x:video:type", type);
        _item.OpenGraphAttributes.Add("og:x:video:width", width);
        _item.OpenGraphAttributes.Add("og:x:video:height", height);

        iframeElement.Remove();
    }

    private string GetVariationByKey(string key, string identity)
    {
        // Find out which feature flag variation we are using to crawl articles
        string variation = _client.GetVariation(key, identity);
        _log.Information("Unlaunch {key} returned variation {variation} for identity {identity}", key, variation, identity);

        return variation;
    }

    private void FixupRelativeUrls(IHtmlDocument document, string baseUrl)
    {
        ReplaceTagAttribute(document, baseUrl, "img", "src", true);
        ReplaceTagAttribute(document, baseUrl, "a", "href", false);
    }

    private void FixupImageSrc(IHtmlDocument document, string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri baseUri))
        {
            _log.Warning("Invalid base url {baseUrl}, aborting relative Url fixup", baseUrl);
            return;
        }

        foreach (var element in document.QuerySelectorAll("img"))
        {
            string src = "";
            string datasrc = "";

            if (element.HasAttribute("src"))
            {
                src = element.GetAttribute("src");
            }
            if (element.HasAttribute("data-src"))
            {
                datasrc = element.GetAttribute("data-src");
            }

            if (!string.IsNullOrEmpty(datasrc))
            {
                _log.Information("Replacing src={src} with data-src={datasrc}", src, datasrc);
                element.SetAttribute("src", datasrc);
            }
        }
    }

    private void ReplaceTagAttribute(IHtmlDocument document, string baseUrl, string tagName, string attributeName, bool addMissing)
    {
        var elements = document.QuerySelectorAll(tagName);
        foreach (var element in elements)
        {
            if (!element.HasAttribute(attributeName) && !addMissing) continue;

            var sourceUri = element.HasAttribute(attributeName) ? element.GetAttribute(attributeName) : "";

            if (!sourceUri.IsNullOrEmptyOrData() || sourceUri.StartsWith("#"))
            {
                if (sourceUri.StartsWith("http") || sourceUri.StartsWith("mailto"))
                    continue;

                sourceUri = _webUtils.RepairUrl(sourceUri, baseUrl);
                _log.Information("Element {element} set to {attributeName}={sourceUri}", element.GetSelector(), attributeName, sourceUri);
                element.SetAttribute(attributeName, sourceUri);
            }
            else
            {
                var alternateAttrName = string.Concat("data-", attributeName);
                if (element.HasAttribute(alternateAttrName))
                {
                    sourceUri = element.GetAttribute(alternateAttrName);
                    if (sourceUri.IndexOf(':') == -1)
                        sourceUri = _webUtils.RepairUrl(sourceUri, baseUrl);
                    _log.Information("Element {element} using {alternateAttrName} to set {attributeName}={sourceUri}", element.GetSelector(), alternateAttrName, attributeName, sourceUri);
                    element.SetAttribute(attributeName, sourceUri);
                    continue;
                }

                alternateAttrName = string.Concat("data-runner-", attributeName);
                if (element.HasAttribute(alternateAttrName))
                {
                    sourceUri = element.GetAttribute(alternateAttrName);
                    if (sourceUri.IndexOf(':') == -1)
                        sourceUri = _webUtils.RepairUrl(sourceUri, baseUrl);
                    _log.Information("Element {element} using {alternateAttrName} to set {attributeName}={sourceUri}", element.GetSelector(), alternateAttrName, attributeName, sourceUri);
                    element.SetAttribute(attributeName, sourceUri);
                    continue;
                }

                alternateAttrName = "data-img";
                if (element.HasAttribute(alternateAttrName))
                {
                    sourceUri = element.GetAttribute(alternateAttrName);
                    if (sourceUri.IndexOf(':') == -1)
                        sourceUri = _webUtils.RepairUrl(sourceUri, baseUrl);
                    _log.Information("Element {element} using {alternateAttrName} to set {attributeName}={sourceUri}", element.GetSelector(), alternateAttrName, attributeName, sourceUri);
                    element.SetAttribute(attributeName, sourceUri);
                    continue;
                }
            }
        }
    }

    private void RemoveDuplicateImgTag(IHtmlDocument document)
    {
        var imgUrl = _item.OpenGraphAttributes.GetValueOrDefault("og:image:secure_url") ??
            _item.OpenGraphAttributes.GetValueOrDefault("og:image:url") ??
            _item.OpenGraphAttributes.GetValueOrDefault("og:image") ??
            "";

        if (imgUrl.Length > 0)
        {
            var elements = document.QuerySelectorAll("img");
            foreach (var element in elements)
            {
                var parentElement = element.ParentElement;

                if (element.HasAttribute("src") && element.GetAttribute("src") == imgUrl)
                    element.Remove();

                // CFP also wraps the image with an anchor tag
                if (parentElement.NodeName.ToLower() == "a")
                    parentElement.Remove();
            }
        }
    }

    public virtual void PreParse()
    { }

    private bool TryGetVideoIFrame(IHtmlDocument document, string pattern, out IElement iframe)
    {
        var elements = document.QuerySelectorAll("iframe");

        foreach (var element in elements)
        {
            if (element.HasAttribute("src") && element.GetAttribute("src").Contains(pattern))
            {
                iframe = element;
                return true;
            }
        }

        iframe = null;
        return false;
    }

    protected void TryAddUlParagraph(StringBuilder description, IElement p)
    {
        if (p.Text().Trim().Length == 0)
        {
            _log.Information("Skipped tag: {tag} Reason: {reason}", p.TagName, "Empty");
            return;
        }
        if (p.ParentElement?.TagName.ToLower() == "blockquote" || p.GetSelector().Contains(">blockquote"))
        {
            _log.Information("Skipped tag: {tag} Reason: {reason}", p.TagName, "Embedded blockquote");
            return;
        }
        if (p.ParentElement?.TagName.ToLower() == "li" || p.GetSelector().Contains(">li"))
        {
            _log.Information("Skipped tag: {tag} Reason: {reason}", p.TagName, "Embedded listitem");
            return;
        }
        if (p.Text().Contains("Bookmark") ||
            p.Text().Contains("Share on") ||
            p.Text().Contains("Share Article") ||
            p.Id == "post_meta" ||
            (p.Id?.StartsWith("sharebar") ?? false) ||
            p.Text().Contains("Share This Story", StringComparison.InvariantCultureIgnoreCase) ||
            p.Text().Contains("Click to Share", StringComparison.InvariantCultureIgnoreCase) ||
            p.ClassList.Contains("rotator-panels") ||
            p.ClassList.Contains("rotator-pages") ||
            p.ClassList.Contains("playlist") ||
            p.ClassList.Contains("article-social") ||
            p.ClassList.Contains("xwv-rotator") ||
            p.ClassList.Contains("a-social-share-spacing") ||
            p.ClassList.Contains("socialShare") ||
            p.ClassList.Contains("heateor_sssp_sharing_ul") ||
            p.ClassList.Contains("list-none") ||
            p.ClassList.Contains("essb_links_list") ||
            p.ClassList.Contains("simple-list") ||
            p.ClassList.Contains("td-category") ||
            p.ClassList.Contains("social-icons__list") ||
            p.ClassList.Contains("authors") ||
            p.ParentElement.ClassList.Contains("sd-content") ||
            p.ParentElement.ClassList.Contains("editorial"))
        {
            _log.Information("Skipped tag: {tag} Reason: {reason}", p.TagName, "Excluded");
            return;
        }

        description.AppendLine($"<p><{p.TagName.ToLower()}>{p.InnerHtml}</{p.TagName.ToLower()}></p>");
    }

    protected void TryAddParagraph(StringBuilder description, IElement p)
    {
        if (p.ParentElement?.TagName.ToLower() == "blockquote" || p.GetSelector().Contains(">blockquote"))
        {
            _log.Debug("Skipped tag: {tag} Reason: {reason}", p.TagName, "Embedded blockquote");
            return;
        }
        if (p.ParentElement?.TagName.ToLower() == "li" || p.GetSelector().Contains(">li"))
        {
            _log.Debug("Skipped tag: {tag} Reason: {reason}", p.TagName, "Embedded listitem");
            return;
        }
        if (p.Text().Trim().Length == 0)
        {
            _log.Debug("Skipped tag: {tag} Reason: {reason}", p.TagName, "Empty");
            return;
        }

        // Watch for the older style line breaks and convert to proper paragraphs
        string innerHtml = LineBreakRegex().Replace(p.InnerHtml, "</p><p>");
        description.AppendLine($"<p>{innerHtml}</p>");
    }
}
