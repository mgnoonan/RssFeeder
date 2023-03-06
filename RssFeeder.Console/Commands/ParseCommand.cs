namespace RssFeeder.Console.Commands;

[Description("Parse article data from the specified URL")]
public class ParseCommand : OaktonCommand<ParseInput>
{
    private readonly IContainer _container;
    private readonly ILogger _log;

    public ParseCommand(IContainer container, ILogger log)
    {
        _container = container;
        _log = log;

        // The usage pattern definition here is completely
        // optional
        Usage("Parse URL using default tag parser").Arguments(x => x.Url);
        Usage("Parse URL using specified tag parser").Arguments(x => x.Url, x => x.Parser);
        Usage("Parse URL using specified tag parser and selectors").Arguments(x => x.Url, x => x.Parser, x => x.BodySelector, x => x.ParagraphSelector);
    }

    public override bool Execute(ParseInput input)
    {
        _log.Information("PARSE_START: Machine: {machineName}", Environment.MachineName);

        var utils = _container.Resolve<IUtils>();
        var webUtils = _container.Resolve<IWebUtils>();
        var parser = _container.ResolveNamed<ITagParser>(input.Parser);
        var template = new ArticleRouteTemplate
        {
            ArticleSelector = input.BodySelector,
            ParagraphSelector = input.ParagraphSelector,
            Name = input.Parser
        };

        string urlHash = utils.CreateMD5Hash(input.Url);
        (_, string html, _, _) = webUtils.DownloadString(input.Url);

        var doc = new HtmlDocument();
        doc.Load(new StringReader(html));

        var ogImage = ParseMetaTagAttributes(doc, "og:image", "content");
        var siteName = ParseMetaTagAttributes(doc, "og:site_name", "content").ToLower();

        _log.Information("MD5 Hash = '{UrlHash}'", urlHash);
        _log.Information("og:site_name = '{SiteName}'", siteName);
        _log.Information("og:title = '{Title}'", ParseMetaTagAttributes(doc, "og:title", "content"));
        _log.Information("og:description = '{Description}'", ParseMetaTagAttributes(doc, "og:description", "content"));
        _log.Information("og:image = '{Image}'", ogImage);

        var item = new RssFeedItem();
        item.SiteName = siteName;
        item.FeedAttributes.FeedId = "parse-cmd";
        item.FeedAttributes.Url = input.Url;
        item.FeedAttributes.UrlHash = urlHash;
        item.OpenGraphAttributes.Add("og:image", ogImage);

        parser.Initialize(doc.Text, item);
        parser.PreParse();
        item.HtmlAttributes.Add("ParserResult", parser.ParseTagsBySelector(template));
        parser.PostParse();
        _log.Information("Parser result = '{parserResult}'", item.HtmlAttributes["ParserResult"]);

        _log.Information("PARSE_END: Completed successfully");

        // Just telling the OS that the command
        // finished up okay
        return true;
    }

    private string ParseMetaTagAttributes(HtmlDocument doc, string property, string attribute)
    {
        // Retrieve the requested meta tag by property name
        var node = doc.DocumentNode.SelectSingleNode($"//meta[@property='{property}']");

        // Node can come back null if the meta tag is not present in the DOM
        // Attribute can come back null as well if not present on the meta tag
        string value = node?.Attributes[attribute]?.Value.Trim() ?? string.Empty;

        return value;
    }
}
