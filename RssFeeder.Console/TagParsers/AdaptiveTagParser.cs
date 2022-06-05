using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;

namespace RssFeeder.Console.TagParsers;

public class AdaptiveTagParser : ITagParser
{
    public string ParseTagsBySelector(string html, SiteArticleDefinition options)
    {
        return ParseTagsBySelector(html, "", "p");
    }

    public string ParseTagsBySelector(string html, string bodySelector, string paragraphSelector)
    {
        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        if (string.IsNullOrEmpty(paragraphSelector))
            paragraphSelector = "p";

        Log.Debug("Attempting adaptive parsing using paragraph selector '{paragraphSelector}'", paragraphSelector);
        bodySelector = GetHighestParagraphCountSelector(document, paragraphSelector, true);

        if (string.IsNullOrEmpty(bodySelector))
        {
            paragraphSelector = "br";
            Log.Debug("Attempting adaptive parsing using paragraph selector '{paragraphSelector}'", paragraphSelector);
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
                    var paragraphs2 = container.QuerySelectorAll(paragraphSelector);
                    return BuildArticleText(paragraphs2);
                case "br":
                    return BuildArticleText(container.InnerHtml);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error parsing paragraph selectors '{paragraphSelector}', '{message}'", paragraphSelector, ex.Message);
        }

        return string.Empty;
    }

    private static string GetHighestParagraphCountSelector(IHtmlDocument document, string paragraphSelector, bool skipIfEmptyContent)
    {
        // Query the document by CSS selectors to get the article text
        var paragraphs = document.QuerySelectorAll(paragraphSelector);
        if (!paragraphs.Any())
        {
            Log.Warning("Paragraph selector '{paragraphSelector}' not found", paragraphSelector);
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

        Log.Debug("Found {totalCount} paragraph selectors '{paragraphSelector}' in html body", paragraphs.Count(), paragraphSelector);
        Log.Information("Parent with the most paragraph selectors is '{bodySelector}':{highCount}", bodySelector, highCount);

        if (highCount <= 1)
        {
            Log.Warning("Only {highCount} paragraph selector found, that doesn't count", highCount);
            return string.Empty;
        }

        return bodySelector;
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
                description.AppendLine($"<p>{p.InnerHtml}</p>");
            }
        }

        return description.ToString();
    }

    protected virtual string BuildArticleText(string innerHtml)
    {
        return String.Concat("<p>", Regex.Replace(innerHtml, "<br\\s?\\/?>", "</p><p>"), "</p>");
    }
}
