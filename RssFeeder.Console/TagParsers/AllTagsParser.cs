using AngleSharp.Html.Dom;

namespace RssFeeder.Console.TagParsers;

public class AllTagsParser : TagParserBase, ITagParser
{
    private readonly ILogger _log;

    public AllTagsParser(ILogger log, IWebUtils webUtils) : base(log, webUtils)
    {
        _log = log;
    }

    public string ParseTagsBySelector(ArticleRouteTemplate template)
    {
        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(_sourceHtml);

        string paragraphSelector = template.ParagraphSelector;

        _log.Information(_parserMessageTemplate, nameof(AllTagsParser), string.Empty, paragraphSelector);
        return BuildArticleText(document, paragraphSelector);
    }

    private string BuildArticleText(IHtmlDocument document, string paragraphSelector)
    {
        // Query the document by CSS selectors to get the article text
        var paragraphs = document.QuerySelectorAll(paragraphSelector);
        if (paragraphs.Length == 0)
        {
            _log.Warning("Paragraph selector '{paragraphSelector}' not found", paragraphSelector);
            return string.Empty;
        }

        var dict = new Dictionary<string, int>();
        foreach (var parent in paragraphs.Select(p => p.ParentElement))
        {
            var key = parent.GetSelector();

            if (dict.TryGetValue(key, out int value))
            {
                dict[key] = ++value;
            }
            else
            {
                dict.Add(key, 1);
            }
        }

        _log.Information("Found {totalCount} paragraph selectors '{paragraphSelector}' in html body", paragraphs.Length, paragraphSelector);

        try
        {
            return BuildArticleText(paragraphs);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error parsing paragraph selectors '{paragraphSelector}', '{message}'", paragraphSelector, ex.Message);
        }

        return string.Empty;
    }

    protected virtual string BuildArticleText(IHtmlCollection<IElement> paragraphs)
    {
        StringBuilder description = new StringBuilder();

        foreach (var p in paragraphs)
        {
            if (p.TagName.StartsWith("h", StringComparison.CurrentCultureIgnoreCase))
            {
                description.AppendLine($"<h4>{p.TextContent.Trim()}</h4>");
            }
            else
            {
                TryAddParagraph(description, p);
            }
        }

        return EmptyParagraphRegex().Replace(description.ToString(), "");
    }
}
