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

        Log.Information("Attempting generic parsing using body selector '{bodySelector}' and paragraph selector '{paragraphSelector}'", bodySelector, paragraphSelector);

        // Query the document by CSS selectors to get the article text
        var container = document.QuerySelector(bodySelector);
        if (container == null)
        {
            Log.Warning("Error reading article: '{bodySelector}' article body selector not found.", bodySelector);
            return $"<p>Error reading article: '{bodySelector}' article body selector not found.</p>";
        }

        var paragraphs = container.QuerySelectorAll(paragraphSelector);

        return BuildArticleText(paragraphs);
    }

    protected virtual string BuildArticleText(IHtmlCollection<IElement> paragraphs)
    {
        StringBuilder description = new StringBuilder();

        foreach (var p in paragraphs)
        {
            string value = p.TextContent.Trim();

            if (p.TagName.ToLower().StartsWith("h"))
            {
                description.AppendLine($"<h4>{value}</h4>");
            }
            else
            {
                // Replace any <br> tags and empty paragraphs
                description.AppendLine($"<p>{value}</p>");
            }
        }

        return description.ToString();
    }
}
