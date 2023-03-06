namespace RssFeeder.Console.TagParsers;

public partial class GenericTagParser : TagParserBase, ITagParser
{
    private readonly ILogger _log;

    public GenericTagParser(ILogger log, IUnlaunchClient client, IWebUtils webUtils) : base(log, client, webUtils)
    {
        _log = log;
    }

    public string ParseTagsBySelector(ArticleRouteTemplate template)
    {
        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(_sourceHtml);

        string paragraphSelector = template.ParagraphSelector;
        if (paragraphSelector == "p")
        {
            paragraphSelector = "p,ol,ul,blockquote,h2,h3,h4,h5,figure";
        }
        _log.Information("Attempting generic tag parsing using body selector '{bodySelector}' and paragraph selector '{paragraphSelector}'", template.ArticleSelector, paragraphSelector);

        // Query the document by CSS selectors to get the article text
        var container = document.QuerySelector(template.ArticleSelector);
        if (container is null)
        {
            _log.Warning("Error reading article: '{bodySelector}' article body selector not found.", template.ArticleSelector);
            return $"<p>Error reading article: '{template.ArticleSelector}' article body selector not found.</p>";
        }

        var paragraphs = container.QuerySelectorAll(paragraphSelector);
        _log.Information("Paragraph selector '{paragraphSelector}' returned {count} paragraphs", paragraphSelector, paragraphs.Length);

        string text = BuildArticleText(paragraphs);

        if (!string.IsNullOrEmpty(template.EmbeddedArticleUrlSelector))
        {
            var link = container.QuerySelector(template.EmbeddedArticleUrlSelector);
            if (link is not null)
            {
                _log.Information("Detected embedded article url using selector {embeddedArticleUrlSelector}", template.EmbeddedArticleUrlSelector);
                text += $"<p>{link.OuterHtml}</p>";
            }
        }

        return text;
    }

    protected virtual string BuildArticleText(IHtmlCollection<IElement> paragraphs)
    {
        StringBuilder description = new StringBuilder();

        foreach (var p in paragraphs)
        {
            if (p.TagName.ToLower().StartsWith("h"))
            {
                TryAddHeaderParagraph(description, p);
            }
            else if (p.TagName.ToLower() == "ul" || p.TagName.ToLower() == "ol")
            {
                TryAddUlParagraph(description, p);
            }
            else if (p.TagName.ToLower() == "pre")
            {
                // Pre tag is for formatted monospaced text
                var lines = p.TextContent.Split('\n', StringSplitOptions.TrimEntries);
                description.AppendLine($"<pre style=\"padding-left: 20px; font-size: 14px; display: block; font-family: monospace; white-space: pre; margin: 1em 0px;\">");
                foreach (var line in lines)
                {
                    description.Append(line);
                    description.AppendLine("<br />");
                }
                description.AppendLine("</pre>");
            }
            else if (p.TagName.ToLower().StartsWith("blockquote"))
            {
                TryAddBlockquote(description, p);
            }
            else
            {
                TryAddParagraph(description, p);
            }
        }

        return EmptyParagraphRegex().Replace(description.ToString(), "");
    }
}
