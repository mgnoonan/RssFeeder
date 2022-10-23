using System.Text.RegularExpressions;

namespace RssFeeder.Console.TagParsers;

public partial class TagParserBase
{
    private readonly ILogger _log;
    protected string _sourceHtml;
    protected RssFeedItem _item;

    public TagParserBase(ILogger log)
    {
        _log = log;
    }

    [GeneratedRegex("<br\\s?\\/?>")]
    protected static partial Regex LineBreakRegex();

    [GeneratedRegex("<p>(&nbsp;)?<\\/p>")]
    protected static partial Regex EmptyParagraphRegex();

    public void Initialize(string sourceHtml, RssFeedItem item)
    {
        _sourceHtml = sourceHtml;
        _item = item ?? new RssFeedItem();
    }

    public virtual void PostParse()
    {
        var result = _item.HtmlAttributes.GetValueOrDefault("ParserResult") ?? "";
        var imgUrl = _item.OpenGraphAttributes.GetValueOrDefault("og:image") ?? "";

        if (imgUrl.Length > 0)
        {
            _log.Debug("Attempting removal of image {url}", imgUrl);
            result = RemoveHtmlTag(result, "img", "src", imgUrl);
        }

        // Check for embedded videos
        if (_item.SiteName != "youtube" || _item.SiteName != "rumble")
        {
            if (TryGetVideoIFrame(result, "rumble.com/embed", out IElement iframeElement))
            {
                string url = iframeElement.Attributes["src"].Value;
                string type = iframeElement.HasAttribute("type") ? iframeElement.Attributes["type"].Value : "text/html";
                string width = iframeElement.Attributes["width"].Value;
                string height = iframeElement.Attributes["height"].Value;
                _log.Information("Embedded video {type} detected {url}", type, url);

                _item.OpenGraphAttributes.Add("og:x:video", url);
                _item.OpenGraphAttributes.Add("og:x:video:type", type);
                _item.OpenGraphAttributes.Add("og:x:video:width", width);
                _item.OpenGraphAttributes.Add("og:x:video:height", height);

                result = RemoveHtmlTag(result, "iframe", "src", url);
            }
            else if (TryGetVideoIFrame(result, "youtube.com/embed", out iframeElement))
            {
                string url = iframeElement.Attributes["src"].Value;
                string type = iframeElement.HasAttribute("type") ? iframeElement.Attributes["type"].Value : "text/html";
                string width = iframeElement.Attributes["width"].Value;
                string height = iframeElement.Attributes["height"].Value;
                _log.Information("Embedded video {type} detected {url}", type, url);

                _item.OpenGraphAttributes.Add("og:x:video", url);
                _item.OpenGraphAttributes.Add("og:x:video:type", type);
                _item.OpenGraphAttributes.Add("og:x:video:width", width);
                _item.OpenGraphAttributes.Add("og:x:video:height", height);

                result = RemoveHtmlTag(result, "iframe", "src", url);
            }
        }

        _item.HtmlAttributes["ParserResult"] = result;
    }

    public virtual void PreParse()
    { }

    private string RemoveHtmlTag(string html, string tagName, string attributeName, string pattern)
    {
        var startPos = html.IndexOf($"{attributeName}=\"{pattern}\"");

        if (startPos > 0)
        {
            startPos = html[..startPos].LastIndexOf($"<{tagName} ");

            if (startPos > 0)
            {
                string endTag = $"</{tagName}>";
                int endPos = html.IndexOf(endTag, startPos);
                if (endPos == -1)
                {
                    endTag = ">";
                    endPos = html.IndexOf(endTag, startPos);
                }

                var length = endPos - startPos + endTag.Length;
                _log.Information("Removed tag {tagName} with {attributeName}={pattern} starting from {start} for length {length}", tagName, attributeName, pattern, startPos, length);
                return html.Remove(startPos, length);
            }

        }

        return html;
    }

    private bool TryGetVideoIFrame(string html, string pattern, out IElement iframe)
    {
        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var elements = document.QuerySelectorAll("iframe");

        foreach (var element in elements)
        {
            if (element.HasAttribute("src") && element.Attributes["src"].Value.Contains(pattern))
            {
                iframe = element;
                return true;
            }
        }

        iframe = null;
        return false;
    }
}