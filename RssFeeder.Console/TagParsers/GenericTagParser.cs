using AngleSharp.Html.Dom;

namespace RssFeeder.Console.TagParsers;

public partial class GenericTagParser : TagParserBase, ITagParser
{
    private readonly ILogger _log;

    public GenericTagParser(ILogger log, IWebUtils webUtils) : base(log, webUtils)
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
        if (paragraphSelector == "p")
        {
            paragraphSelector = "p,ol,ul,blockquote,h2,h3,h4,h5,figure";
        }

        _log.Information(_parserMessageTemplate, nameof(GenericTagParser), bodySelector, paragraphSelector);
        return BuildArticleText(document, bodySelector, paragraphSelector, template.EmbeddedArticleUrlSelector);
    }

    private string BuildArticleText(IHtmlDocument document, string bodySelector, string paragraphSelector, string embeddedArticleUrlSelector)
    {
        // Query the document by CSS selectors to get the article text
        var container = document.QuerySelector(bodySelector);
        if (container is null)
        {
            _log.Warning("Error reading article: '{bodySelector}' article body selector not found.", bodySelector);
            return $"<p>Error reading article: '{bodySelector}' article body selector not found.</p>";
        }

        var paragraphs = container.QuerySelectorAll(paragraphSelector);
        _log.Information("Paragraph selector '{paragraphSelector}' returned {count} paragraphs", paragraphSelector, paragraphs.Length);

        string text = BuildArticleText(paragraphs);

        if (!string.IsNullOrEmpty(embeddedArticleUrlSelector))
        {
            var link = container.QuerySelector(embeddedArticleUrlSelector);
            if (link is not null)
            {
                _log.Information("Detected embedded article url using selector {embeddedArticleUrlSelector}", embeddedArticleUrlSelector);
                text += $"<p>{link.OuterHtml}</p>";
            }
        }

        return text;
    }

    protected virtual string BuildArticleText(IHtmlCollection<IElement> paragraphs)
    {
        StringBuilder description = new();

        foreach (var p in paragraphs)
        {
            if (p.TagName.StartsWith("h", StringComparison.CurrentCultureIgnoreCase))
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
            else if (p.TagName.ToLower().StartsWith("figure"))
            {
                TryAddFigure(description, p);
            }
            else if (p.TagName.ToLower() == "a")
            {
                TryAddAnchor(description, p);
            }
            else
            {
                TryAddParagraph(description, p);
            }
        }

        return EmptyParagraphRegex().Replace(description.ToString(), "");
    }
}
