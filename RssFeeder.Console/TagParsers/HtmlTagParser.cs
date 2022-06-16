﻿namespace RssFeeder.Console.TagParsers;

public class HtmlTagParser : TagParserBase, ITagParser
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

        Log.Information("Attempting HTML tag parsing using body selector '{bodySelector}' and paragraph selector '{paragraphSelector}'", bodySelector, paragraphSelector);

        // Query the document by CSS selectors to get the article text
        var container = document.QuerySelector(bodySelector);
        if (container is null)
        {
            Log.Warning("Error reading article: '{bodySelector}' article body selector not found.", bodySelector);
            return $"<p>Error reading article: '{bodySelector}' article body selector not found.</p>";
        }

        return container.QuerySelector(paragraphSelector).OuterHtml;
    }
}
