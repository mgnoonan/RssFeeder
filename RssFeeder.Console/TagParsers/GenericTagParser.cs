﻿namespace RssFeeder.Console.TagParsers;

public class GenericTagParser : ITagParser
{
    public string ParseTagsBySelector(string html, SiteArticleDefinition options)
    {
        return ParseTagsBySelector(html, options.ArticleSelector, options.ParagraphSelector);
    }

    public string ParseTagsBySelector(string html, string bodySelector, string paragraphSelector)
    {
        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

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
                    string value = p.TextContent.Trim();
                    description.AppendLine($"<p>{value}</p>");
                }
            }
        }

        return description.ToString();
    }
}
