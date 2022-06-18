namespace RssFeeder.Console.TagParsers;

public class ScriptTagParser : TagParserBase, ITagParser
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

        Log.Information("Attempting script block parsing using body selector '{bodySelector}' and paragraph selector '{paragraphSelector}'", bodySelector, paragraphSelector);

        // Query the document by CSS selectors to get the article text
        var elements = document.QuerySelectorAll("script");
        if (elements.Count() == 0)
        {
            Log.Warning("Error reading article: '{bodySelector}' article body selector not found.", bodySelector);
            return $"<p>Error reading article: '{bodySelector}' article body selector not found.</p>";
        }

        string content = "";
        foreach (var element in elements)
        {
            if (element.TextContent.Contains(bodySelector))
            {
                var lines = element.TextContent.Replace("\\u003c", "<").Split("\n", StringSplitOptions.RemoveEmptyEntries);
                content = lines.Where(q => q.Contains(bodySelector)).FirstOrDefault();
                int pos = content.IndexOf('{');
                content = content[pos..].Trim();
                pos = content.LastIndexOf('}') + 1;
                content = content[..pos].Trim();
                break;
            }
        }

        try
        {
            if (string.IsNullOrEmpty(content))
            {
                Log.Warning("Unable to parse article data from script '{content}'", content);
                return string.Empty;
            }

            var jsonObject = JsonConvert.DeserializeObject<dynamic>(content);
            var data = (JObject)jsonObject["content"]["data"];
            var output = data.First.First["storyHTML"];

            return output.ToString();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error parsing paragraph selectors '{paragraphSelector}', '{message}'", paragraphSelector, ex.Message);
        }

        return string.Empty;
    }
}
