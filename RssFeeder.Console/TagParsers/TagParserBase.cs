using System.Text.RegularExpressions;

namespace RssFeeder.Console.TagParsers;

public partial class TagParserBase
{
    protected string _sourceHtml;
    protected RssFeedItem _item;

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
        if (_item.SiteName == "youtube")
            return;

        var result = _item.HtmlAttributes.GetValueOrDefault("ParserResult") ?? "";

        // Check for embedded youtube videos
        if (TryGetVideoIFrame(result, "youtube.com/embed", out IElement iframeElement))
        {
            string url = iframeElement.Attributes["src"].Value;
            string type = iframeElement.HasAttribute("type") ? iframeElement.Attributes["type"].Value : "text/html";
            string width = iframeElement.Attributes["width"].Value;
            string height = iframeElement.Attributes["height"].Value;
            Log.Information("Embedded video {type} detected {url}", type, url);

            _item.OpenGraphAttributes.Add("og:x:video", url);
            _item.OpenGraphAttributes.Add("og:x:video:type", type);
            _item.OpenGraphAttributes.Add("og:x:video:width", width);
            _item.OpenGraphAttributes.Add("og:x:video:height", height);

            _item.HtmlAttributes["ParserResult"] = RemoveVideoIFrame(result, url);
            return;
        }

        // Check for embedded rumble videos
        if (TryGetVideoIFrame(result, "rumble.com/embed", out iframeElement))
        {
            string url = iframeElement.Attributes["src"].Value;
            string type = iframeElement.HasAttribute("type") ? iframeElement.Attributes["type"].Value : "text/html";
            string width = iframeElement.Attributes["width"].Value;
            string height = iframeElement.Attributes["height"].Value;
            Log.Information("Embedded video {type} detected {url}", type, url);

            _item.OpenGraphAttributes.Add("og:x:video", url);
            _item.OpenGraphAttributes.Add("og:x:video:type", type);
            _item.OpenGraphAttributes.Add("og:x:video:width", width);
            _item.OpenGraphAttributes.Add("og:x:video:height", height);

            _item.HtmlAttributes["ParserResult"] = RemoveVideoIFrame(result, url);
            return;
        }
    }

    public virtual void PreParse()
    { }

    private string RemoveVideoIFrame(string html, string pattern)
    {
        var pos = html.IndexOf($"src=\"{pattern}\"");
        pos = html[..pos].LastIndexOf("<iframe ");

        if (pos > 0)
        {
            string end = "</iframe>";
            var len = html.IndexOf(end, pos) - pos + end.Length;
            return html.Remove(pos, len);
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
            if (element.Attributes["src"].Value.Contains(pattern))
            {
                iframe = element;
                return true;
            }
        }

        iframe = null;
        return false;
    }
}