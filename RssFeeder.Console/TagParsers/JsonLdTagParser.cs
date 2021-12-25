namespace RssFeeder.Console.TagParsers;

public class JsonLdTagParser : ITagParser
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

        Log.Information("Attempting json+ld tag parsing using body selector '{bodySelector}' and paragraph selector '{paragraphSelector}'", bodySelector, paragraphSelector);

        // Query the document by CSS selectors to get the article text
        var elements = document.QuerySelectorAll("script");
        if (elements.Length == 0)
        {
            Log.Warning("Error finding json+ld script block: Falling back to HtmlTagTagParse on '{bodySelector}' and paragraph selector '{paragraphSelector}'", bodySelector, paragraphSelector);

            // Query the document by CSS selectors to get the article text
            var container = document.QuerySelector(bodySelector);
            if (container == null)
            {
                Log.Warning("Error reading article: '{bodySelector}' article body selector not found.", bodySelector);
                return $"<p>Error reading article: '{bodySelector}' article body selector not found.</p>";
            }

            return container.QuerySelector(paragraphSelector).OuterHtml;
        }

        foreach (var element in elements)
        {
            if (element.HasAttribute("type") && element.GetAttribute("type") == "application/ld+json")
            {
                return element.TextContent;
            }
        }

        Log.Warning("Error reading article: No ld+json selector not found.");
        return string.Empty;
    }
}
