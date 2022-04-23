﻿using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;

namespace RssFeeder.Console;

public class ArticleParser : IArticleParser
{
    private IContainer _container;
    private IArticleDefinitionFactory _definitionFactory;
    private IWebUtils _webUtils;

    public CrawlerConfig Config { get; set; }

    public void Initialize(IContainer container, IArticleDefinitionFactory definitionFactory, IWebUtils webUtils)
    {
        _container = container;
        _definitionFactory = definitionFactory;
        _webUtils = webUtils;
    }

    public void Parse(RssFeedItem item)
    {
        // Article failed to download for some reason, skip over meta data processing
        if (!File.Exists(item.FeedAttributes.FileName))
        {
            Log.Debug("No file to parse, skipping metadata values for '{url}'", item.FeedAttributes.Url);
            return;
        }

        // Graphics file or PDF won't have og tags
        if (item.FeedAttributes.FileName.EndsWith(".png") ||
            item.FeedAttributes.FileName.EndsWith(".jpg") ||
            item.FeedAttributes.FileName.EndsWith(".gif") ||
            item.FeedAttributes.FileName.EndsWith(".pdf"))
        {
            Log.Debug("Graphics file detected, skipping metadata values for '{url}'", item.FeedAttributes.Url);
            return;
        }

        Log.Debug("Parsing meta tags from file '{fileName}'", item.FeedAttributes.FileName);

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
            (string namedParser, string articleSelector, string paragraphSelector) = GetRouteMatchedTagParser(definition, GetRouteOnly(item));

            // Resolve the named parameter using DI
            var parser = _container.ResolveNamed<ITagParser>(namedParser);

            // Parse the content to get the article text
            item.HtmlAttributes.Add("ParserResult", parser.ParseTagsBySelector(doc.Text, articleSelector, paragraphSelector));
        }
    }

    private (string, string, string) GetRouteMatchedTagParser(SiteArticleDefinition definition, string routeToMatch)
    {
        if (definition is null)
        {
            Log.Debug("SiteDefinition is null. Falling back to adaptive parser.");
            return ("adaptive-parser", "", "");
        }

        if (definition.RouteTemplates.Length > 0)
        {
            var matcher = new RouteMatcher();
            foreach (var articleRoute in definition.RouteTemplates)
            {
                if (matcher.Match(articleRoute.Template, "/" + routeToMatch) != null)
                {
                    Log.Information("Matched {routeToMatch} on route {name} template {template} and parser {parser}", routeToMatch, articleRoute.Name, articleRoute.Template, articleRoute.Parser);
                    return (articleRoute.Parser, articleRoute.ArticleSelector, articleRoute.ParagraphSelector);
                }
            }

            // Might have forgotten to create a **catch-all template, fall back to adaptive parser
            Log.Warning("Missing **catch-all template. Falling back to adaptive parser.");
            return ("adaptive-parser", "", "p");
        }
        else
        {
            // No route templates defined, fall back to older style definition or adpative parser
            Log.Debug("No route templates defined. falling back to {parser}", definition.Parser);
            return (string.IsNullOrEmpty(definition.Parser) ? "adaptive-parser" : definition.Parser,
                definition.ArticleSelector, definition.ParagraphSelector);
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

        Uri uri = new Uri(url);
        return uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).ToLower();
    }

    private string GetHostName(RssFeedItem item)
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

        Uri uri = new Uri(url);
        return uri.GetComponents(UriComponents.Host, UriFormat.Unescaped).ToLower();
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

            if (propertyValue.StartsWith("og:"))
            {
                string contentValue = node.Attributes["content"]?.Value ?? "unspecified";

                while (contentValue.Contains("&#x"))
                {
                    contentValue = System.Web.HttpUtility.HtmlDecode(contentValue);
                    Log.Debug("Decoded content value '{contentValue}'", contentValue);
                }
                Log.Debug("Found open graph attribute '{propertyValue}':'{contentValue}'", propertyValue, contentValue);

                if (!attributes.ContainsKey(propertyValue))
                {
                    attributes.Add(propertyValue, contentValue);
                }
                else
                {
                    for (int i = 1; i < 100; i++)
                    {
                        string newPropertyValue = $"{propertyValue}:{i:0#}";
                        if (!attributes.ContainsKey(newPropertyValue))
                        {
                            attributes.Add(newPropertyValue, contentValue);
                            break;
                        }
                    }
                }
            }
        }

        return attributes;
    }

    private Dictionary<string, string> ParseHtmlAttributes(HtmlDocument doc)
    {
        var attributes = new Dictionary<string, string>();

        // Title tag
        var node = doc.DocumentNode.SelectSingleNode($"//title");
        string contentValue = node?.InnerText.Trim() ?? string.Empty;
        attributes.Add("title", contentValue);

        // H1 values for possible headline - may not exist
        node = doc.DocumentNode.SelectSingleNode($"//h1");
        contentValue = node?.InnerText.Trim() ?? string.Empty;
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
            Log.Debug("Attribute '{attribute}' from meta tag '{property}' not found", targetAttributeName, targetAttributeValue);
        }
        else
        {
            // Decode the value if it contains a coded reference
            if (sourceAttributeValue.Contains("&#x"))
            {
                sourceAttributeValue = System.Web.HttpUtility.HtmlDecode(sourceAttributeValue);
            }

            Log.Debug("Meta attribute '{attribute}':'{property}' has a decoded value of '{value}'", targetAttributeName, targetAttributeValue, sourceAttributeValue);

        }

        return sourceAttributeValue;
    }
}
