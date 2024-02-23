using System.Dynamic;
using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;
using RulesEngine.Models;

namespace RssFeeder.Console.TagParsers;

// Disabling the ValueTask warning because there is no alternative and so far it works
#pragma warning disable CA2012
public partial class TagParserBase
{
    protected const string _parserMessageTemplate = "Parser {parserName} using body selector '{bodySelector}' and paragraph selector '{paragraphSelector}'";

    private readonly ILogger _log;
    private readonly IWebUtils _webUtils;
    private RulesEngine.RulesEngine _bre;
    protected string _sourceHtml;
    protected RssFeedItem _item;

    private const string _sizePattern = @"-?(\d{1,4}x\d{1,4}|rawImage)";
    private const string _sizePattern2 = @"/ALTERNATES/s\d{3,4}";
    private const string _sizePattern3 = @"\/w:\d{3,4}\/p:";
    private const string _sizePattern4 = @"\/(mobile_thumb__|blog_image_\d{2}_)";
    private static readonly string[] srcAttributeArray = new string[] { "src" };
    private static readonly string[] extendedSrcAttributeArray = new string[] { "data-mm-src", "data-src", "data-lazy-src", "data-srcs", "data-srcset", "data-img-url" };

    public TagParserBase(ILogger log, IWebUtils webUtils)
    {
        _log = log;
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
            throw new InvalidOperationException("Rules not found.");

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
        FixupIframeSrc(document, baseUrl);
        FixupElementStyle(document, "figcaption", "font-size: 75%;font-style: italic;");
        FixupElementStyle(document, "blockquote", "border-left: 7px solid lightgray; padding-left: 10px;");
        RemoveDuplicateImgTag(document);
        RemoveElementPadding(document);
        RemoveAllTag(document, "noscript");

        // Check for embedded videos
        if (_item.SiteName != "youtube" && _item.SiteName != "rumble")
        {
            var elements = document.QuerySelectorAll("iframe");
            _log.Debug("IFRAME tag count {count}", elements.Length);

            if (elements.Length > 0 && TryGetVideoIFrame(elements, "(rumble|bitchute|youtube).com/embed", out IElement iframeElement))
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

        if (type.Contains("lazy") && url.Contains("youtube.com")) type = "text/html";
        _log.Information("Embedded video {type} detected {url}", type, url);

        _item.OpenGraphAttributes.Add("og:x:video", url);
        _item.OpenGraphAttributes.Add("og:x:video:type", type);
        _item.OpenGraphAttributes.Add("og:x:video:width", width);
        _item.OpenGraphAttributes.Add("og:x:video:height", height);

        iframeElement.Remove();
    }

    private void FixupRelativeUrls(IHtmlDocument document, string baseUrl)
    {
        ReplaceTagAttribute(document, baseUrl, "img", "src", true);
        ReplaceTagAttribute(document, baseUrl, "a", "href", false);
    }

    private void FixupIframeSrc(IHtmlDocument document, string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri baseUri))
        {
            _log.Warning("Invalid base url {baseUrl}, aborting relative Url fixup", baseUrl);
            return;
        }

        foreach (var element in document.QuerySelectorAll("iframe"))
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
                dataAttributeValue = element.GetAttribute(dataAttribute);
            }
            if (element.HasAttribute("data-lazy-src"))
            {
                dataAttribute = "data-lazy-src";
                dataAttributeValue = element.GetAttribute(dataAttribute);
            }
            if (element.HasAttribute("data-runner-src"))
            {
                dataAttribute = "data-runner-src";
                dataAttributeValue = element.GetAttribute(dataAttribute);
            }

            if (!string.IsNullOrEmpty(dataAttributeValue) && dataAttributeValue != attributeValue)
            {
                _log.Information("Replacing src={attributeValue} with {dataAttribute}={dataAttributeValue}", attributeValue, dataAttribute, dataAttributeValue);
                element.SetAttribute("src", dataAttributeValue);
                element.RemoveAttribute(dataAttribute);
            }
        }
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
            (_, string attributeValue) = GetAttributeValue(element, srcAttributeArray);
            (string dataAttribute, string dataAttributeValue) = GetAttributeValue(element, extendedSrcAttributeArray);

            RemoveAttribute(element, "srcset");
            RemoveAttribute(element, "data-srcset");
            RemoveAttribute(element, "data-srcs");

            if (dataAttribute == "data-srcs")
            {
                JObject obj = JObject.Parse(System.Web.HttpUtility.HtmlDecode(dataAttributeValue));
                dataAttributeValue = obj.Properties().First().Name;
            }

            if (dataAttributeValue?.Contains(' ') ?? false)
            {
                string url = System.Web.HttpUtility.UrlDecode(dataAttributeValue.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]);
                dataAttributeValue = url;
            }

            if (!string.IsNullOrEmpty(dataAttributeValue) && dataAttributeValue != attributeValue)
            {
                _log.Information("Replacing src={attributeValue} with {dataAttribute}={dataAttributeValue}", attributeValue, dataAttribute, dataAttributeValue);
                element.SetAttribute("src", dataAttributeValue);
                element.RemoveAttribute(dataAttribute);
            }
        }
    }

    private static (string, string) GetAttributeValue(IElement element, string[] names)
    {
        string value = "";
        string _name = "";

        foreach (string name in names)
        {
            if (element.HasAttribute(name))
            {
                value = element.GetAttribute(name);
                _name = name;
            }
        }

        return (_name, value);
    }

    private void RemoveAttribute(IElement element, string name)
    {
        if (element.HasAttribute(name))
        {
            _log.Information("Removing {name}={attributeValue}", name, element.GetAttribute(name));
            element.RemoveAttribute(name);
        }
    }

    private void FixupElementStyle(IHtmlDocument document, string selectors, string style)
    {
        foreach (var element in document.QuerySelectorAll(selectors))
        {
            if (element.HasAttribute("class"))
            {
                element.RemoveAttribute("class");
            }
            if (element.HasAttribute("style"))
            {
                element.RemoveAttribute("style");
            }

            _log.Debug("Setting up style {style} for {selectors}", selectors);
            element.SetAttribute("style", style);
        }
    }

    private void ReplaceTagAttribute(IHtmlDocument document, string baseUrl, string tagName, string attributeName, bool addMissing)
    {
        var elements = document.QuerySelectorAll(tagName);
        foreach (var element in elements)
        {
            if (!element.HasAttribute(attributeName) && !addMissing) continue;

            var sourceUri = element.HasAttribute(attributeName) ? element.GetAttribute(attributeName) : "";

            if (!sourceUri.IsNullOrEmptyOrData())
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
                if (ReplaceTagAttribute(element, attributeName, alternateAttrName, baseUrl))
                    continue;

                alternateAttrName = string.Concat("data-runner-", attributeName);
                if (ReplaceTagAttribute(element, attributeName, alternateAttrName, baseUrl))
                    continue;

                alternateAttrName = "data-img";
                ReplaceTagAttribute(element, attributeName, alternateAttrName, baseUrl);
            }
        }
    }

    private bool ReplaceTagAttribute(IElement element, string attributeName, string alternateAttrName, string baseUrl)
    {
        if (element.HasAttribute(alternateAttrName))
        {
            string sourceUri = element.GetAttribute(alternateAttrName);
            if (sourceUri.Contains(':'))
                sourceUri = _webUtils.RepairUrl(sourceUri, baseUrl);

            _log.Information("Element {element} using {alternateAttrName} to set {attributeName}={sourceUri}", element.GetSelector(), alternateAttrName, attributeName, sourceUri);
            element.SetAttribute(attributeName, sourceUri);

            return true;
        }

        return false;
    }

    private void RemoveAllTag(IHtmlDocument document, string tagName)
    {
        var elements = document.QuerySelectorAll(tagName);
        foreach (var element in elements)
        {
            _log.Information("Removed tag {tagName} {selector}", tagName, element.GetSelector());
            element.Remove();
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

                    // CFP also wraps the image with an anchor tag
                    if (parentElement.NodeName.ToLower() == "a")
                        parentElement.Remove();
                }
            }
        }
    }

    private bool ImageSourcesAreEqual(string value1, string value2)
    {
        if (Regex.IsMatch(value1, _sizePattern, RegexOptions.None, TimeSpan.FromMilliseconds(250)) ||
            Regex.IsMatch(value2, _sizePattern, RegexOptions.None, TimeSpan.FromMilliseconds(250)))
        {
            value1 = Regex.Replace(value1, _sizePattern, "", RegexOptions.None, TimeSpan.FromMilliseconds(250));
            value2 = Regex.Replace(value2, _sizePattern, "", RegexOptions.None, TimeSpan.FromMilliseconds(250));
        }
        if (Regex.IsMatch(value1, _sizePattern2, RegexOptions.None, TimeSpan.FromMilliseconds(250)) ||
            Regex.IsMatch(value2, _sizePattern2, RegexOptions.None, TimeSpan.FromMilliseconds(250)))
        {
            value1 = Regex.Replace(value1, _sizePattern2, "", RegexOptions.None, TimeSpan.FromMilliseconds(250));
            value2 = Regex.Replace(value2, _sizePattern2, "", RegexOptions.None, TimeSpan.FromMilliseconds(250));
        }
        if (Regex.IsMatch(value1, _sizePattern3, RegexOptions.None, TimeSpan.FromMilliseconds(250)) ||
            Regex.IsMatch(value2, _sizePattern3, RegexOptions.None, TimeSpan.FromMilliseconds(250)))
        {
            value1 = Regex.Replace(value1, _sizePattern3, "", RegexOptions.None, TimeSpan.FromMilliseconds(250));
            value2 = Regex.Replace(value2, _sizePattern3, "", RegexOptions.None, TimeSpan.FromMilliseconds(250));
        }
        if (Regex.IsMatch(value1, _sizePattern4, RegexOptions.None, TimeSpan.FromMilliseconds(250)) ||
            Regex.IsMatch(value2, _sizePattern4, RegexOptions.None, TimeSpan.FromMilliseconds(250)))
        {
            value1 = Regex.Replace(value1, _sizePattern4, "", RegexOptions.None, TimeSpan.FromMilliseconds(250));
            value2 = Regex.Replace(value2, _sizePattern4, "", RegexOptions.None, TimeSpan.FromMilliseconds(250));
        }

        // Decode any URL encoding sometimes used for CDN references
        value1 = value1.Contains("%3A") ? System.Web.HttpUtility.UrlDecode(value1) : value1;
        value2 = value2.Contains("%3A") ? System.Web.HttpUtility.UrlDecode(value2) : value2;

        // Move query string embedded url
        value1 = value1.Contains("url=") ? value1[(value1.IndexOf("url=") + 4)..] : value1;
        value2 = value2.Contains("url=") ? value2[(value2.IndexOf("url=") + 4)..] : value2;

        // Strip off the query string
        value1 = value1.Contains('?') ? value1[..value1.IndexOf('?')] : value1;
        value2 = value2.Contains('?') ? value2[..value2.IndexOf('?')] : value2;

        // Replace webp with jpg
        value1 = value1.EndsWith(".webp") ? value1.Replace(".webp", ".jpg") : value1;
        value2 = value2.EndsWith(".webp") ? value2.Replace(".webp", ".jpg") : value2;

        // WSJ has a special route for social media thumbnails
        value1 = value1.EndsWith("/social") ? value1[..value1.IndexOf("/social")] : value1;
        value2 = value2.EndsWith("/social") ? value2[..value2.IndexOf("/social")] : value2;

        // Yahoo CDN route handling
        value1 = value1.Contains("--/") ? value1[(value1.LastIndexOf("--/") + 3)..] : value1;
        value2 = value2.Contains("--/") ? value2[(value2.LastIndexOf("--/") + 3)..] : value2;

        // Substack CDN route handling
        value1 = value1.Contains("/https") ? value1[(value1.LastIndexOf("/https") + 1)..] : value1;
        value2 = value2.Contains("/https") ? value2[(value2.LastIndexOf("/https") + 1)..] : value2;

        if (value1.Contains(".jpg") && (value1.Split('/', StringSplitOptions.RemoveEmptyEntries).Last() == value2.Split('/', StringSplitOptions.RemoveEmptyEntries).Last()))
        {
            return true;
        }

        _log.Debug("value1 = {value}", value1);
        _log.Debug("value2 = {value}", value2);
        return value1 == value2;
    }

    private void RemoveElementPadding(IHtmlDocument document)
    {
        var elements = document.All.Where(m => m.HasAttribute("style") && (m.GetAttribute("style").Contains("padding") || m.GetAttribute("style").Contains("height")));
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

    private bool TryGetVideoIFrame(IHtmlCollection<IElement> elements, string pattern, out IElement iframe)
    {
        foreach (var element in elements)
        {
            var src = element.HasAttribute("src") ? element.GetAttribute("src").ToLower() : "";
            _log.Debug("Checking iframe src={src} for {pattern}", src, pattern);

            if (Regex.IsMatch(src, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(250)))
            {
                iframe = element;
                return true;
            }
        }

        iframe = null;
        return false;
    }

    protected string GetSelector(IElement element)
    {
        try
        {
            // Retrieving the selector will sometimes generate an exception
            return element.GetSelector();
        }
        catch (DomException ex)
        {
            _log.Warning(ex, "Error retrieving selector for {tagName}", element.TagName);
            return "";
        }
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
        x.selector = GetSelector(p);
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
        x.containsiframe = p.InnerHtml.Contains("iframe");
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

    protected void TryAddFigure(StringBuilder description, IElement p)
    {
        _log.Debug("InnerHtml = {html}", p.InnerHtml);
        description.AppendLine($"<figure>{p.InnerHtml}</figure>");
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
        description.AppendLine($"<blockquote>{p.InnerHtml}</blockquote>");
    }

    protected void TryAddAnchor(StringBuilder description, IElement p)
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

        List<RuleResultTree> resultList = _bre.ExecuteAllRulesAsync("ExcludeAnchor", input).Result;

        //Check success for rule
        foreach (var result in resultList)
        {
            if (result.IsSuccess)
            {
                _log.Information("Skipped tag: {tag} Reason: {reason}", p.TagName, result.Rule.RuleName);
                return;
            }
        }

        description.AppendLine($"<p>{p.OuterHtml}</p>");
    }
}
