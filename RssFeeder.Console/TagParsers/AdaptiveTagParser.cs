using AngleSharp.Html.Dom;

namespace RssFeeder.Console.TagParsers;

public partial class AdaptiveTagParser : TagParserBase, ITagParser
{
    private readonly ILogger _log;

    public AdaptiveTagParser(ILogger log) : base(log)
    {
        _log = log;
    }

    public string ParseTagsBySelector(ArticleRouteTemplate template)
    {
        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(_sourceHtml);
        string paragraphSelector = template.ParagraphSelector;

        if (string.IsNullOrEmpty(paragraphSelector))
            paragraphSelector = "p";

        _log.Debug("Attempting adaptive parsing using paragraph selector '{paragraphSelector}'", paragraphSelector);
        string bodySelector = GetHighestParagraphCountSelector(document, paragraphSelector, true);

        if (string.IsNullOrEmpty(bodySelector))
        {
            paragraphSelector = "br";
            _log.Debug("Attempting adaptive parsing using paragraph selector '{paragraphSelector}'", paragraphSelector);
            bodySelector = GetHighestParagraphCountSelector(document, paragraphSelector, false);
        }

        if (string.IsNullOrEmpty(bodySelector))
            return string.Empty;

        return GetArticleText(document, bodySelector, paragraphSelector);
    }

    private string GetArticleText(IHtmlDocument document, string bodySelector, string paragraphSelector)
    {
        try
        {
            // Query the document by CSS selectors to get the article text
            var container = document.QuerySelector(bodySelector);

            // Get only the paragraphs under the parent
            switch (paragraphSelector)
            {
                case "p":
                    var paragraphs = container.QuerySelectorAll("p,ol,ul,blockquote");
                    return BuildArticleText(paragraphs);
                case "br":
                    return BuildArticleText(container.InnerHtml);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error parsing paragraph selectors '{paragraphSelector}', '{message}'", paragraphSelector, ex.Message);
        }

        return string.Empty;
    }

    private string GetHighestParagraphCountSelector(IHtmlDocument document, string paragraphSelector, bool skipIfEmptyContent)
    {
        // Query the document by CSS selectors to get the article text
        var paragraphs = document.QuerySelectorAll(paragraphSelector);
        if (!paragraphs.Any())
        {
            _log.Warning("Paragraph selector '{paragraphSelector}' not found", paragraphSelector);
            return string.Empty;
        }

        // Build up the counts for each parent selector
        var dict = new Dictionary<string, int>();
        foreach (var p in paragraphs)
        {
            if (skipIfEmptyContent && string.IsNullOrWhiteSpace(System.Web.HttpUtility.HtmlDecode(p.TextContent)))
            {
                continue;
            }

            var parent = p.ParentElement;
            if (parent.TagName.ToLower() == "blockquote")
                parent = parent.ParentElement;

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

        // Get the parent with the most paragraphs, it should be the article content
        int highCount = default;
        string bodySelector = "";
        foreach (var key in dict.Keys)
        {
            if (dict[key] > highCount)
            {
                bodySelector = key;
                highCount = dict[key];
            }
        }

        _log.Debug("Found {totalCount} paragraph selectors '{paragraphSelector}' in html body", paragraphs.Count(), paragraphSelector);
        _log.Information("Parent with the most paragraph selectors is '{bodySelector}':{highCount}", bodySelector, highCount);

        if (highCount <= 1)
        {
            _log.Warning("Only {highCount} paragraph selector found, that doesn't count", highCount);
            return string.Empty;
        }

        return bodySelector;
    }

    protected virtual string BuildArticleText(IHtmlCollection<IElement> paragraphs)
    {
        var description = new StringBuilder();

        foreach (var p in paragraphs)
        {
            if (p.TagName.ToLower().StartsWith("h"))
            {
                description.AppendLine($"<h4>{p.TextContent.Trim()}</h4>");
            }
            else if (p.TagName.ToLower() == "ul" || p.TagName.ToLower() == "ol")
            {
                TryAddUlParagraph(description, p);
            }
            else if (p.TagName.ToLower() == "blockquote")
            {
                description.AppendLine($"<blockquote style=\"border-left: 7px solid lightgray; padding-left: 10px;\">{p.InnerHtml}</blockquote>");
            }
            else
            {
                TryAddParagraph(description, p);
            }
        }

        return EmptyParagraphRegex().Replace(description.ToString(), "");
    }

    protected virtual string BuildArticleText(string innerHtml)
    {
        return string.Concat("<p>", LineBreakRegex().Replace(innerHtml, "</p><p>"), "</p>");
    }
}
