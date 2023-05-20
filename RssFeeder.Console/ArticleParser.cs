namespace RssFeeder.Console;

public class ArticleParser : IArticleParser
{
    private IContainer _container;
    private IArticleDefinitionFactory _definitionFactory;
    private IWebUtils _webUtils;
    private ILogger _log;

    public void Initialize(IContainer container, IArticleDefinitionFactory definitionFactory, IWebUtils webUtils, ILogger log)
    {
        _container = container;
        _definitionFactory = definitionFactory;
        _webUtils = webUtils;
        _log = log;
    }

    public void Parse(RssFeedItem item)
    {
        // Article failed to download for some reason, skip over meta data processing
        if (!File.Exists(item.FeedAttributes.FileName))
        {
            _log.Debug("No file to parse, skipping metadata values for '{url}'", item.FeedAttributes.Url);
            return;
        }

        // Text files that are not HTML can't be parsed
        if (item.FeedAttributes.FileName.EndsWith(".json") ||
            item.FeedAttributes.FileName.EndsWith(".txt"))
        {
            _log.Information("Text file detected, skipping metadata values for '{url}'", item.FeedAttributes.Url);
            return;
        }

        // Graphics file or PDF won't have og tags
        if (item.FeedAttributes.FileName.EndsWith(".png") ||
            item.FeedAttributes.FileName.EndsWith(".jpg") ||
            item.FeedAttributes.FileName.EndsWith(".gif") ||
            item.FeedAttributes.FileName.EndsWith(".pdf"))
        {
            _log.Information("Binary file detected, skipping metadata values for '{url}'", item.FeedAttributes.Url);
            return;
        }

        _log.Debug("Parsing meta tags from file '{fileName}'", item.FeedAttributes.FileName);

        var doc = new HtmlDocument();
        doc.Load(item.FeedAttributes.FileName);

        // Parse the meta data from the raw HTML document
        item.OpenGraphAttributes.Add(ParseOpenGraphAttributes(doc));
        item.HtmlAttributes.Add(ParseHtmlAttributes(doc));
        item.HostName = GetHostName(item);
        item.SiteName = GetSiteName(item);

        // Check if we have a site parser defined for the site name
        var definition = _definitionFactory.Get(item.SiteName);

        using (LogContext.PushProperty("siteName", item.SiteName))
        {
            // Determine the named parser to use along with article and para selectors
            var template = GetRouteMatchedTagParser(definition, GetRouteOnly(item));

            // Resolve the named parameter using DI
            var parser = _container.ResolveNamed<ITagParser>(template.Parser);
            parser.Initialize(doc.Text, item);

            // Parse the content to get the article text
            parser.PreParse();
            item.HtmlAttributes.Add("ParserResult", parser.ParseTagsBySelector(template));
            parser.PostParse();
        }
    }

    private ArticleRouteTemplate GetRouteMatchedTagParser(SiteArticleDefinition definition, string routeToMatch)
    {
        if (definition is null)
        {
            _log.Debug("SiteDefinition is null. Falling back to adaptive parser.");
            return new ArticleRouteTemplate
            {
                Name = "_fallback_no_sitedef",
                Parser = "adaptive-parser"
            };
        }

        if (definition.RouteTemplates?.Length > 0)
        {
            var matcher = new RouteMatcher();
            foreach (var articleRoute in definition.RouteTemplates)
            {
                if (matcher.Match(articleRoute.Template, "/" + routeToMatch) != null)
                {
                    _log.Information("Matched route {routeName} on template {template}", articleRoute.Name, articleRoute.Template);
                    return articleRoute;
                }
            }

            // Might have forgotten to create a **catch-all template, fall back to adaptive parser
            _log.Warning("Missing **catch-all template. Falling back to adaptive parser.");
            return new ArticleRouteTemplate
            {
                Name = "_fallback_no_catchall",
                Parser = "adaptive-parser"
            };
        }
        else
        {
            // No route templates defined, fall back to older style definition or adpative parser
            _log.Debug("No route templates defined. falling back to {@parser}", definition);
            return new ArticleRouteTemplate
            {
                Name = "_fallback_no_route",
                Parser = string.IsNullOrEmpty(definition.Parser) ? "adaptive-parser" : definition.Parser,
                ArticleSelector = definition.ArticleSelector,
                ParagraphSelector = definition.ParagraphSelector
            };
        }
    }

    private string GetRouteOnly(RssFeedItem item)
    {
        string url = item.OpenGraphAttributes.GetValueOrDefault("og:url") ?? "";

        // Make sure the Url is complete
        if (!url.StartsWith("http"))
        {
            url = item.HtmlAttributes.GetValueOrDefault("Url") ?? item.FeedAttributes.Url;
        }

        if (!url.StartsWith("http"))
        {
            url = _webUtils.RepairUrl(url, item.FeedAttributes.Url);
        }

        try
        {
            Uri uri = new Uri(url);
            return uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).ToLower();
        }
        catch (UriFormatException ex)
        {
            _log.Error(ex, "Invalid URL cannot convert {invalidUrl}", url);
            throw;
        }
    }

    private static string GetHostName(RssFeedItem item)
    {
        return GetHostName(item.OpenGraphAttributes.GetValueOrDefault("og:url"),
                           item.HtmlAttributes.GetValueOrDefault("Url"),
                           item.FeedAttributes.Url);
    }

    private static string GetHostName(params string[] urls)
    {
        foreach (string url in urls)
        {
            if (string.IsNullOrEmpty(url)) continue;

            if (Uri.TryCreate(url.ToLower(), UriKind.Absolute, out var uri))
            {
                return uri.GetComponents(UriComponents.Host, UriFormat.Unescaped);
            }
        }

        throw new UriFormatException("No valid Url was found");
    }

    private string GetSiteName(RssFeedItem item)
    {
        string siteName = item.OpenGraphAttributes.GetValueOrDefault("og:site_name")?.ToLower() ?? item.HostName;

        return siteName;
    }

    private Dictionary<string, string> ParseOpenGraphAttributes(HtmlDocument doc)
    {
        var attributes = new Dictionary<string, string>();
        var nodes = doc.DocumentNode.SelectNodes($"//meta");
        if (nodes is null)
        {
            return attributes;
        }

        foreach (var node in nodes)
        {
            string propertyValue = node.Attributes["property"]?.Value ??
                node.Attributes["name"]?.Value ?? "";

            if (!propertyValue.StartsWith("og:"))
                continue;

            string contentValue = ParseOpenGraphAttributeValue(node, propertyValue);

            if (attributes.ContainsKey(propertyValue))
            {
                propertyValue = GetPropertyValueIndex(attributes, propertyValue);
            }

            attributes.Add(propertyValue, contentValue);
        }

        return attributes;
    }

    private static string GetPropertyValueIndex(Dictionary<string, string> attributes, string propertyValue)
    {
        for (int i = 1; i < 100; i++)
        {
            string newPropertyValue = $"{propertyValue}:{i:0#}";
            if (!attributes.ContainsKey(newPropertyValue))
            {
                return newPropertyValue;
            }
        }

        return "og:unknown";
    }

    private string ParseOpenGraphAttributeValue(HtmlNode node, string propertyValue)
    {
        string contentValue = node.Attributes["content"]?.Value ?? "";

        if (contentValue.Contains("&#x") || contentValue.Contains("&#32;"))
        {
            contentValue = System.Web.HttpUtility.HtmlDecode(contentValue);
            _log.Information("Decoded {propertyValue} content value '{contentValue}'", propertyValue, contentValue);
        }
        else if (contentValue.Contains("%3A") && !Uri.TryCreate(contentValue, UriKind.Absolute, out Uri _))
        {
            contentValue = System.Web.HttpUtility.UrlDecode(contentValue);
            _log.Information("Decoded {propertyValue} content value '{contentValue}'", propertyValue, contentValue);
        }

        _log.Debug("Found open graph attribute '{propertyValue}':'{contentValue}'", propertyValue, contentValue);
        return contentValue;
    }

    private Dictionary<string, string> ParseHtmlAttributes(HtmlDocument doc)
    {
        var attributes = new Dictionary<string, string>();

        // Title tag
        var node = doc.DocumentNode.SelectSingleNode($"//title");
        string contentValue = node?.InnerText.Trim() ?? string.Empty;
        if (contentValue.Contains("&#x") || contentValue.Contains("&#32;"))
        {
            contentValue = System.Web.HttpUtility.HtmlDecode(contentValue);
            _log.Information("Decoded {propertyValue} content value '{contentValue}'", node.Name, contentValue);
        }
        attributes.Add("title", contentValue);

        // H1 values for possible headline - may not exist
        node = doc.DocumentNode.SelectSingleNode($"//h1");
        contentValue = node?.InnerText.Trim() ?? string.Empty;
        if (contentValue.Contains("&#x") || contentValue.Contains("&#32;"))
        {
            contentValue = System.Web.HttpUtility.HtmlDecode(contentValue);
            _log.Information("Decoded {propertyValue} content value '{contentValue}'", node.Name, contentValue);
        }
        attributes.Add("h1", contentValue);

        // Description meta tag - may not exist
        attributes.Add("description", ParseMetaTagAttributes(doc, "name", "description", "content"));

        return attributes;
    }

    private string ParseMetaTagAttributes(HtmlDocument doc, string targetAttributeName, string targetAttributeValue, string sourceAttributeName)
    {
        // Retrieve the requested meta tag by property name
        var node = doc.DocumentNode.SelectSingleNode($"//meta[@{targetAttributeName}='{targetAttributeValue}']");

        // Node can come back null if the meta tag is not present in the DOM
        // Attribute can come back null as well if not present on the meta tag
        string sourceAttributeValue = node?.Attributes[sourceAttributeName]?.Value.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sourceAttributeValue))
        {
            _log.Debug("Attribute '{attribute}' from meta tag '{property}' not found", targetAttributeName, targetAttributeValue);
        }
        else
        {
            // Decode the value if it contains a coded reference
            if (sourceAttributeValue.Contains("&#x"))
            {
                sourceAttributeValue = System.Web.HttpUtility.HtmlDecode(sourceAttributeValue);
            }

            _log.Debug("Meta attribute '{attribute}':'{property}' has a decoded value of '{value}'", targetAttributeName, targetAttributeValue, sourceAttributeValue);

        }

        return sourceAttributeValue;
    }
}
