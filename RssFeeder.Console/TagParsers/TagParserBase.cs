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
        _item = item;
    }

    public virtual void PostParse()
    {
        var result = _item.HtmlAttributes?.GetValueOrDefault("ParserResult") ?? "";
        var imgUrl = _item.OpenGraphAttributes.GetValueOrDefault("og:image") ?? "";

        if (imgUrl.Length > 0)
        {
            _log.Debug("Attempting removal of image {url}", imgUrl);
            result = RemoveHtmlTag(result, "img", GetHostAndPathOnly(imgUrl));
        }

        // Check for embedded videos
        if (_item.SiteName != "youtube" || _item.SiteName != "rumble")
        {
            if (TryGetVideoIFrame(result, "rumble.com/embed", out IElement iframeElement))
            {
                string url = iframeElement.Attributes["src"].Value;
                string type = iframeElement.HasAttribute("type") ? iframeElement.Attributes["type"].Value : "text/html";
                string width = iframeElement.HasAttribute("width") ? iframeElement.Attributes["width"].Value : "640";
                string height = iframeElement.HasAttribute("height") ? iframeElement.Attributes["height"].Value : "480";
                _log.Information("Embedded video {type} detected {url}", type, url);

                _item.OpenGraphAttributes.Add("og:x:video", url);
                _item.OpenGraphAttributes.Add("og:x:video:type", type);
                _item.OpenGraphAttributes.Add("og:x:video:width", width);
                _item.OpenGraphAttributes.Add("og:x:video:height", height);

                result = RemoveHtmlTag(result, "iframe", GetHostAndPathOnly(url));
            }
            else if (TryGetVideoIFrame(result, "youtube.com/embed", out iframeElement))
            {
                string url = iframeElement.Attributes["src"].Value;
                string type = iframeElement.HasAttribute("type") ? iframeElement.Attributes["type"].Value : "text/html";
                string width = iframeElement.HasAttribute("width") ? iframeElement.Attributes["width"].Value : "640";
                string height = iframeElement.HasAttribute("height") ? iframeElement.Attributes["height"].Value : "480";
                _log.Information("Embedded video {type} detected {url}", type, url);

                _item.OpenGraphAttributes.Add("og:x:video", url);
                _item.OpenGraphAttributes.Add("og:x:video:type", type);
                _item.OpenGraphAttributes.Add("og:x:video:width", width);
                _item.OpenGraphAttributes.Add("og:x:video:height", height);

                result = RemoveHtmlTag(result, "iframe", GetHostAndPathOnly(url));
            }
        }

        _item.HtmlAttributes["ParserResult"] = result;
    }

    public virtual void PreParse()
    { }
	
	private List<int> GetCountSubstring(string source, string pattern)
	{
		List<int> positions = new List<int>();
		int pos = 0;
		
		while ((pos < source.Length) && (pos = source.IndexOf(pattern, pos)) != -1)
		{
			positions.Add(pos);
			pos += pattern.Length;
		}
		
		return positions;
	}

    private string RemoveHtmlTag(string html, string tagName, string pattern)
    {
		var positions = GetCountSubstring(html, pattern);

		foreach (int pos in positions)
		{
			int startPos = html[..pos].LastIndexOf($"<{tagName} ");
			if (startPos == -1) continue;
			
        	_log.Debug("Removing {pattern}, starting position = {startPos}", pattern, startPos);
			string endTag = $"</{tagName}>";
			int endPos = html.IndexOf(endTag, startPos);
			if (endPos == -1)
			{
				endTag = ">";
				endPos = html.IndexOf(endTag, startPos);
			}

			var length = endPos - startPos + endTag.Length;
            _log.Information("Removed tag {tagName} by {pattern} starting from {start} for length {length}", tagName, pattern, startPos, length);
			return html.Remove(startPos, length);
		}

        _log.Debug("Search pattern not found. Nothing replaced.");
        return html;
    }

    private string GetHostAndPathOnly(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Path, UriFormat.Unescaped);
        }

        throw new UriFormatException("No valid Url was found");
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
