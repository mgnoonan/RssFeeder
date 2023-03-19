using System.Dynamic;
using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;
using RulesEngine.Models;

namespace RssFeeder.Console.TagParsers;

public partial class TagParserBase
{
    private readonly ILogger _log;
    private readonly IUnlaunchClient _client;
    private readonly IWebUtils _webUtils;
    private RulesEngine.RulesEngine _bre;
    protected string _sourceHtml;
    protected RssFeedItem _item;

    private const string _sizePattern = @"\d{3,4}x\d{3,4}";
    private const string _sizePattern2 = @"/ALTERNATES/s\d{3,4}";

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

        InitializeRulesEngine();
    }

    private void InitializeRulesEngine()
    {
        var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "ExcludeContentRules.json", SearchOption.AllDirectories);
        if (files == null || files.Length == 0)
            throw new Exception("Rules not found.");

        var fileData = File.ReadAllText(files[0]);
        var workflow = JsonConvert.DeserializeObject<List<Workflow>>(fileData) ?? new List<Workflow>();

        _bre = new RulesEngine.RulesEngine(workflow.ToArray(), null);
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

        FixupRelativeUrls(document, baseUrl);
        FixupImageSrc(document, baseUrl);
        RemoveDuplicateImgTag(document);
        RemoveElementPadding(document);

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
            string attributeValue = "";
            string dataAttribute = "";
            string dataAttributeValue = "";

            if (element.HasAttribute("src"))
            {
                attributeValue = element.GetAttribute("src");
            }
            if (element.HasAttribute("data-src"))
            {
                dataAttribute = "data-src";
                dataAttributeValue = element.GetAttribute(dataAttributeValue);
            }
            if (element.HasAttribute("data-lazy-src"))
            {
                dataAttribute = "data-lazy-src";
                dataAttributeValue = element.GetAttribute(dataAttributeValue);
            }

            if (!string.IsNullOrEmpty(dataAttributeValue) && dataAttributeValue != attributeValue)
            {
                _log.Information("Replacing src={attributeValue} with {dataAttribute}={dataAttributeValue}", attributeValue, dataAttribute, dataAttributeValue);
                element.SetAttribute("src", dataAttributeValue);
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

                if (element.HasAttribute("src") && ImageSourcesAreEqual(element.GetAttribute("src"), imgUrl))
                {
                    _log.Information("Removed duplicate image {imageUrl}", imgUrl);
                    element.Remove();
                }

                // CFP also wraps the image with an anchor tag
                if (parentElement.NodeName.ToLower() == "a")
                    parentElement.Remove();
            }
        }
    }

    private bool ImageSourcesAreEqual(string value1, string value2)
	{        
        if (Regex.IsMatch(value1, _sizePattern))
        {
            value1 = Regex.Replace(value1, _sizePattern, "");
            value2 = Regex.Replace(value2, _sizePattern, "");
        }
        if (Regex.IsMatch(value1, _sizePattern2))
        {
            value1 = Regex.Replace(value1, _sizePattern2, "");
            value2 = Regex.Replace(value2, _sizePattern2, "");
        }

        // Decode any URL encoding sometimes used for CDN references
		value1 = value1.Contains("%3A") ? System.Web.HttpUtility.UrlDecode(value1) : value1;
		value2 = value2.Contains("%3A") ? System.Web.HttpUtility.UrlDecode(value2) : value2;

		// Strip off the query string
        value1 = value1.Contains("?") ? value1.Substring(0, value1.IndexOf('?')) : value1;
		value2 = value2.Contains("?") ? value2.Substring(0, value2.IndexOf('?')) : value2;

		// WSJ has a special route for social media thumbnails
 		value1 = value1.EndsWith("/social") ? value1.Substring(0, value1.IndexOf("/social")) : value1;
		value2 = value2.EndsWith("/social") ? value2.Substring(0, value2.IndexOf("/social")) : value2;

		// Yahoo CDN route handling
        value1 = value1.Contains("--/") ? value1.Substring(value1.LastIndexOf("--/") + 3) : value1;
		value2 = value2.Contains("--/") ? value2.Substring(value2.LastIndexOf("--/") + 3) : value2;
		
        // Substack CDN route handling
		value1 = value1.Contains("/https") ? value1.Substring(value1.LastIndexOf("/https") + 1) : value1;
		value2 = value2.Contains("/https") ? value2.Substring(value2.LastIndexOf("/https") + 1) : value2;

		return value1 == value2;
	}

    private void RemoveElementPadding(IHtmlDocument document)
    {
        var elements = document.All.Where(m => m.HasAttribute("style") && m.GetAttribute("style").Contains("padding"));
        _log.Debug("{count} elements with a style attribute", elements.Count());

        foreach (var element in elements)
        {
            if (element.TagName.ToLower() == "blockquote")
                continue;

            _log.Information("Replacing {tagName} style attribute {style}", element.TagName, element.GetAttribute("style"));
            element.RemoveAttribute("style");
        }
    }

    public virtual void PreParse() { }

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

    protected void TryAddHeaderParagraph(StringBuilder description, IElement p)
    {
        dynamic x = new ExpandoObject();
        x.name = _item.SiteName;
        x.text = p.Text().Trim();
        x.id = p.Id ?? "";
        x.tagname = p.TagName.ToLower();
        x.style = p.Attributes["style"];
        x.classlist = String.Join(' ', p.ClassList);
        x.selector = p.GetSelector();
        x.parentclasslist = String.Join(' ', p.ParentElement.ClassList);
        x.parenttagname = p.ParentElement?.TagName.ToLower() ?? "";
        var input = new dynamic[] { x };

        _log.Debug("Input = {@input}", input);

        List<RuleResultTree> resultList = _bre.ExecuteAllRulesAsync("ExcludeHeader", input).Result;

        //Check success for rule
        foreach (var result in resultList)
        {
            if (result.IsSuccess)
            {
                _log.Information("Skipped tag: {tag} Reason: {reason}", p.TagName, result.Rule.RuleName);
                return;
            }
        }

        description.AppendLine($"<{p.TagName.ToLower()}>{p.TextContent.Trim()}</{p.TagName.ToLower()}>");
    }

    protected void TryAddUlParagraph(StringBuilder description, IElement p)
    {
        _log.Debug("Child elements = {children}", p.Children.Length);

        dynamic x = new ExpandoObject();
        x.name = _item.SiteName;
        x.text = p.Text().Trim();
        x.id = p.Id ?? "";
        x.tagname = p.TagName.ToLower();
        x.classlist = String.Join(' ', p.ClassList);
        x.selector = p.GetSelector();
        x.parentclasslist = String.Join(' ', p.ParentElement.ClassList);
        x.parenttagname = p.ParentElement?.TagName.ToLower() ?? "";
        x.childcount = p.Children.Length;
        x.childtagname = p.Children.Length == 0 ? "" : p.Children.First().TagName.ToLower();
        x.childclasslist = p.Children.Length == 0 ? "" : String.Join(' ', p.Children.First().ClassList);
        var input = new dynamic[] { x };

        _log.Debug("Input = {@input}", input);

        List<RuleResultTree> resultList = _bre.ExecuteAllRulesAsync("ExcludeUL", input).Result;

        //Check success for rule
        foreach (var result in resultList)
        {
            if (result.IsSuccess)
            {
                _log.Information("Skipped tag: {tag} Reason: {reason}", p.TagName, result.Rule.RuleName);
                return;
            }
        }

        description.AppendLine($"<p><{p.TagName.ToLower()}>{p.InnerHtml}</{p.TagName.ToLower()}></p>");
    }

    protected void TryAddParagraph(StringBuilder description, IElement p)
    {
        dynamic x = new ExpandoObject();
        x.name = _item.SiteName;
        x.text = p.Text().Trim();
        x.id = p.Id ?? "";
        x.tagname = p.TagName.ToLower();
        x.classlist = String.Join(' ', p.ClassList);
        x.selector = p.GetSelector();
        x.parentclasslist = String.Join(' ', p.ParentElement.ClassList);
        x.parenttagname = p.ParentElement?.TagName.ToLower() ?? "";
        var input = new dynamic[] { x };

        _log.Debug("Input = {@input}", input);

        List<RuleResultTree> resultList = _bre.ExecuteAllRulesAsync("ExcludeParagraph", input).Result;

        //Check success for rule
        foreach (var result in resultList)
        {
            if (result.IsSuccess)
            {
                _log.Information("Skipped tag: {tag} Reason: {reason}", p.TagName, result.Rule.RuleName);
                return;
            }
        }

        // Watch for the older style line breaks and convert to proper paragraphs
        string innerHtml = LineBreakRegex().Replace(p.InnerHtml, "</p><p>");
        description.AppendLine($"<p>{innerHtml}</p>");
    }

    protected void TryAddBlockquote(StringBuilder description, IElement p)
    {
        dynamic x = new ExpandoObject();
        x.name = _item.SiteName;
        x.text = p.Text().Trim();
        x.id = p.Id ?? "";
        x.tagname = p.TagName.ToLower();
        x.classlist = String.Join(' ', p.ClassList);
        x.selector = p.GetSelector();
        x.parentclasslist = String.Join(' ', p.ParentElement.ClassList);
        x.parenttagname = p.ParentElement?.TagName.ToLower() ?? "";
        var input = new dynamic[] { x };

        _log.Debug("Input = {@input}", input);

        List<RuleResultTree> resultList = _bre.ExecuteAllRulesAsync("ExcludeBlockquote", input).Result;

        //Check success for rule
        foreach (var result in resultList)
        {
            if (result.IsSuccess)
            {
                _log.Information("Skipped tag: {tag} Reason: {reason}", p.TagName, result.Rule.RuleName);
                return;
            }
        }

        // Add blockquote with some padding and a left side border
        description.AppendLine($"<blockquote style=\"border-left: 7px solid lightgray; padding-left: 10px;\">{p.InnerHtml}</blockquote>");
    }
}
