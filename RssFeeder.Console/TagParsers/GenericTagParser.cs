namespace RssFeeder.Console.TagParsers;

public class GenericTagParser : TagParserBase, ITagParser
{
    public string ParseTagsBySelector(ArticleRouteTemplate template)
    {
        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(_sourceHtml);

        Log.Information("Attempting generic tag parsing using body selector '{bodySelector}' and paragraph selector '{paragraphSelector}'", template.ArticleSelector, template.ParagraphSelector);

        // Query the document by CSS selectors to get the article text
        var container = document.QuerySelector(template.ArticleSelector);
        if (container is null)
        {
            Log.Warning("Error reading article: '{bodySelector}' article body selector not found.", template.ArticleSelector);
            return $"<p>Error reading article: '{template.ArticleSelector}' article body selector not found.</p>";
        }

        var paragraphs = container.QuerySelectorAll(template.ParagraphSelector);
        Log.Information("Paragraph selector '{paragraphSelector}' returned {count} paragraphs", template.ParagraphSelector, paragraphs.Length);

        string text = BuildArticleText(paragraphs);

        if (!string.IsNullOrEmpty(template.EmbeddedArticleUrlSelector))
        {
            var link = container.QuerySelector(template.EmbeddedArticleUrlSelector);
            if (link is not null)
            {
                Log.Information("Detected embedded article url using selector {embeddedArticleUrlSelector}", template.EmbeddedArticleUrlSelector);
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
                description.AppendLine($"<h4>{p.TextContent.Trim()}</h4>");
            }
            else if (p.TagName.ToLower() == "ul")
            {
                // Unordered list will have all the <li> elements inside
                description.AppendLine($"<p><ul>{p.InnerHtml}</ul></p>");
            }
            else if (p.TagName.ToLower() == "pre")
            {
                // Unordered list will have all the <li> elements inside
                description.AppendLine($"<pre>{p.TextContent.Trim()}</pre><hr>");
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
