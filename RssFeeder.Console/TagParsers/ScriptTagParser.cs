namespace RssFeeder.Console.TagParsers;

public class ScriptTagParser : TagParserBase, ITagParser
{
    private readonly ILogger _log;

    public ScriptTagParser(ILogger log, IWebUtils webUtils) : base(log, webUtils)
    {
        _log = log;
    }

    public string ParseTagsBySelector(ArticleRouteTemplate template)
    {
        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(_sourceHtml);
        string bodySelector = template.ArticleSelector;
        string paragraphSelector = template.ParagraphSelector;

        _log.Information("Attempting script block parsing using body selector '{bodySelector}' and paragraph selector '{paragraphSelector}'", bodySelector, paragraphSelector);

        // Query the document by CSS selectors to get the article text
        var elements = document.QuerySelectorAll("script");
        if (elements.Length == 0)
        {
            _log.Warning("Error reading article: '{bodySelector}' article body selector not found.", bodySelector);
            return $"<p>Error reading article: '{bodySelector}' article body selector not found.</p>";
        }

        string content = "";
        foreach (var element in elements)
        {
            if (element.TextContent.Contains(bodySelector))
            {
                var lines = element.TextContent.Replace("\\u003c", "<").Split("\n", StringSplitOptions.RemoveEmptyEntries);
                content = lines.First(q => q.Contains(bodySelector));
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
            _log.Error(ex, "Error parsing paragraph selectors '{paragraphSelector}', '{message}'", paragraphSelector, ex.Message);
        }

        return string.Empty;
    }
}
