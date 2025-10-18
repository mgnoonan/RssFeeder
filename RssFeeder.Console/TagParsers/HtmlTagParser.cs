namespace RssFeeder.Console.TagParsers;

public class HtmlTagParser : TagParserBase, ITagParser
{
    private readonly ILogger _log;

    public HtmlTagParser(ILogger log, IWebUtils webUtils) : base(log, webUtils)
    {
        _log = log;
    }

    public string ParseTagsBySelector(ArticleRouteTemplate template)
    {
        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(_sourceHtml);
        string bodySelector = template.ArticleSelector;
        string paragraphSelector = template.ParagraphSelector;

        _log.Information(_parserMessageTemplate, nameof(HtmlTagParser), bodySelector, paragraphSelector);

        // Query the document by CSS selectors to get the article text
        var container = document.QuerySelector(bodySelector);
        if (container is null)
        {
            _log.Warning("Error reading {siteName} article: '{bodySelector}' article body selector not found.", _item.SiteName, bodySelector);
            return $"<p>Error reading article: '{bodySelector}' article body selector not found.</p>";
        }

        return container.QuerySelector(paragraphSelector).OuterHtml;
    }
}
