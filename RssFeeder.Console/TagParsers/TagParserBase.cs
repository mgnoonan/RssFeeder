namespace RssFeeder.Console.TagParsers;

public class TagParserBase
{
    protected string _sourceHtml;
    protected RssFeedItem _item;

    public void Initialize(string sourceHtml, RssFeedItem item)
    {
        _sourceHtml = sourceHtml;
        _item = item;
    }

    public virtual void PostParse()
    {
        if (_item.SiteName == "youtube")
            return;

        var result = _item.HtmlAttributes.GetValueOrDefault("ParserResult") ?? "";

        // Check for embedded youtube videos
        string iframe = GetYoutubeVideoIFrame(result);
        if (iframe.Length > 0)
        {
            Log.Information("Embedded video detected");

            string url = GetAttributeValue(iframe, "src");
            string type = GetAttributeValue(iframe, "type");
            string width = GetAttributeValue(iframe, "width");
            string height = GetAttributeValue(iframe, "height");

            _item.OpenGraphAttributes.Add("og:x:video", url);
            _item.OpenGraphAttributes.Add("og:x:video:type", "text/html");
            _item.OpenGraphAttributes.Add("og:x:video:width", width);
            _item.OpenGraphAttributes.Add("og:x:video:height", height);

            _item.HtmlAttributes["ParserResult"] = result.Replace(iframe, "");
        }
    }

    public virtual void PreParse()
    {
    }

    private string GetAttributeValue(string html, string attributeName)
    {
        string format = attributeName + "=\"";
        var pos = html.ToLowerInvariant().IndexOf(format) + format.Length;

        if (pos > format.Length)
        {
            var len = html.IndexOf("\"", pos) - pos;
            return html.Substring(pos, len);
        }

        return "";
    }

    private string GetYoutubeVideoIFrame(string html)
    {
        bool hasVideo = html.ToLowerInvariant().Contains("youtube.com/embed");
        var pos = html.ToLowerInvariant().IndexOf("<iframe ");

        if (hasVideo && pos > 0)
        {
            string end = "</iframe>";
            var len = html.IndexOf(end, pos) - pos + end.Length;
            return html.Substring(pos, len);
        }

        return "";
    }
}