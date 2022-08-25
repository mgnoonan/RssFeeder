namespace RssFeeder.Console.TagParsers;

public class GenericTagParser : TagParserBase, ITagParser
{
    public string ParseTagsBySelector(SiteArticleDefinition options)
    {
        return ParseTagsBySelector(options.ArticleSelector, options.ParagraphSelector);
    }

    public string ParseTagsBySelector(string bodySelector, string paragraphSelector)
    {
        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(_sourceHtml);

        Log.Information("Attempting generic tag parsing using body selector '{bodySelector}' and paragraph selector '{paragraphSelector}'", bodySelector, paragraphSelector);

        // Query the document by CSS selectors to get the article text
        var container = document.QuerySelector(bodySelector);
        if (container is null)
        {
            Log.Warning("Error reading article: '{bodySelector}' article body selector not found.", bodySelector);
            return $"<p>Error reading article: '{bodySelector}' article body selector not found.</p>";
        }

        var paragraphs = container.QuerySelectorAll(paragraphSelector);
        Log.Information("Paragraph selector '{paragraphSelector}' returned {count} paragraphs", paragraphSelector, paragraphs.Length);

        return BuildArticleText(paragraphs);
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
            else if (p.TagName.ToLower() == "ul")
            {
                // Unordered list will have all the <li> elements inside
                description.AppendLine($"<p><ul>{p.InnerHtml}</ul></p>");
            }
            else
            {
                // Watch for the older style line breaks and convert to proper paragraphs
                if (p.InnerHtml.Contains("<br>"))
                {
                    Log.Information("Replacing old style line breaks with paragraph tags");
                    string value = p.InnerHtml.Replace("<br>", "</p><p>");
                    description.AppendLine($"<p>{value}</p>");
                }
                else
                {
                    description.AppendLine($"<p>{p.InnerHtml}</p>");
                }
            }
        }

        return description.ToString();
    }
}
