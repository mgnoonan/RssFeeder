namespace RssFeeder.Console.TagParsers;

public class AllTagsParser : TagParserBase, ITagParser
{
    private readonly ILogger _log;

    public AllTagsParser(ILogger log) : base(log)
    {
        _log = log;
    }

    public string ParseTagsBySelector(ArticleRouteTemplate template)
    {
        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(_sourceHtml);
        string paragraphSelector = template.ParagraphSelector;

        _log.Information("Attempting alltags parsing using paragraph selector '{paragraphSelector}'", paragraphSelector);

        // Query the document by CSS selectors to get the article text
        var paragraphs = document.QuerySelectorAll(paragraphSelector);
        if (!paragraphs.Any())
        {
            _log.Warning("Paragraph selector '{paragraphSelector}' not found", paragraphSelector);
            return string.Empty;
        }

        var dict = new Dictionary<string, int>();
        foreach (var p in paragraphs)
        {
            var parent = p.ParentElement;
            var key = parent.GetSelector();

            if (dict.ContainsKey(key))
            {
                dict[key]++;
            }
            else
            {
                dict.Add(key, 1);
            }
        }

        _log.Information("Found {totalCount} paragraph selectors '{paragraphSelector}' in html body", paragraphs.Count(), paragraphSelector);

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
            if (p.TagName.ToLower().StartsWith("h"))
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
