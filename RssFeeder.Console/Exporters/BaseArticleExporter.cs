namespace RssFeeder.Console.Exporters;

using RssFeeder.Console.Utility;

public class BaseArticleExporter
{
    private readonly ILogger _log;

    public BaseArticleExporter(ILogger log)
    {
        _log = log;
    }

    protected virtual string ApplyTemplateToDescription(ExportFeedItem item, RssFeed feed, string template)
    {
        switch (item.SiteName.ToLower())
        {
            case "youtube":
            case "youtu.be":
                template = template.Replace("{class}", "");
                template = template.Replace("{allow}", "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share");
                break;
            case "rumble":
                template = template.Replace("{class}", "rumble");
                template = template.Replace("{allow}", "");
                break;
            case "bitchute":
                template = template.Replace("{class}", "bitchute");
                template = template.Replace("{allow}", "");
                break;
            case "gab tv":
                template = template.Replace("{class}", "studio-video");
                template = template.Replace("{allow}", "");
                break;
        }

        var t = new Template(template, '$', '$');
        t.Add("item", item);
        t.Add("feed", feed);
        t.Add("ArticleText", item.ArticleText);

        return t.Render();
    }

    protected virtual string GetCanonicalUrl(RssFeedItem item)
    {
        // The best reference URL is usually from the OpenGraph tags, however they are NOT
        // always set to a full canonical URL (looking at you, frontpagemag.com)
        string url = item.OpenGraphAttributes.GetValueOrDefault("og:url") ?? "";

        // If the URL doesn't have a protocol assigned (not canonical) fall back to the URL
        // we crawled (which also might be null)
        if (!url.StartsWith("http"))
        {
            url = item.HtmlAttributes.GetValueOrDefault("Url");
        }

        // Last but not least, fall back to the URL we detected in the feed
        return url ?? item.FeedAttributes.Url;
    }

    protected virtual void SetExtendedArticleMetaData(ExportFeedItem exportFeedItem, RssFeedItem item, string hostName)
    {
        // Extract the meta data from the Open Graph tags helpfully provided with almost every article
        exportFeedItem.Url = item.OpenGraphAttributes.GetValueOrDefault("og:url") ?? "";

        // Make sure the Url is complete
        if (!exportFeedItem.Url.StartsWith("http"))
        {
            exportFeedItem.Url = item.HtmlAttributes.GetValueOrDefault("Url") ?? item.FeedAttributes.Url;
        }

        // Extract the meta data from the Open Graph tags
        exportFeedItem.ArticleText = item.HtmlAttributes.GetValueOrDefault("ParserResult") ?? "";
        exportFeedItem.Subtitle = item.OpenGraphAttributes.GetValueOrDefault("og:title") ?? "";
        exportFeedItem.ImageUrl = item.OpenGraphAttributes.GetValueOrDefault("og:image") ?? "";
        exportFeedItem.SiteName = item.OpenGraphAttributes.GetValueOrDefault("og:site_name")?.ToLower() ?? "";
        exportFeedItem.HostName = hostName;

        // Fixup empty sitename
        if (string.IsNullOrWhiteSpace(exportFeedItem.SiteName))
        {
            exportFeedItem.SiteName = exportFeedItem.HostName;
        }

        // Remove the protocol portion if there is one, i.e. 'https://'
        if (exportFeedItem.SiteName.StartsWith("http") && exportFeedItem.SiteName.Contains("://"))
        {
            _log.Information("Replacing site_name {siteName} with {hostName} for protocol violation", exportFeedItem.SiteName, exportFeedItem.HostName);
            exportFeedItem.SiteName = exportFeedItem.HostName;
        }
    }

    protected virtual void SetBasicArticleMetaData(ExportFeedItem exportFeedItem, RssFeedItem item, string hostName)
    {
        exportFeedItem.HostName = hostName;
        exportFeedItem.SiteName = hostName;
        exportFeedItem.ImageUrl = item.OpenGraphAttributes.GetValueOrDefault("og:image") ?? "";
        exportFeedItem.ArticleText = $"<p>Unable to crawl article content. Click the link below to view in your browser.</p>";
    }

    protected virtual void SetVideoMetaData(ExportFeedItem exportFeedItem, RssFeedItem item, string hostName)
    {
        // Extract the meta data from the Open Graph tags
        exportFeedItem.Subtitle = item.OpenGraphAttributes.GetValueOrDefault("og:title") ?? "";
        exportFeedItem.ImageUrl = item.OpenGraphAttributes.GetValueOrDefault("og:image") ?? "";
        exportFeedItem.SiteName = item.OpenGraphAttributes.GetValueOrDefault("og:site_name")?.ToLower() ?? "";
        exportFeedItem.HostName = hostName;
        var description = item.OpenGraphAttributes.GetValueOrDefault("og:description") ?? "";

        if (string.IsNullOrWhiteSpace(exportFeedItem.SiteName))
        {
            exportFeedItem.SiteName = hostName;
        }

        var jsonLdVideo = GetPrimaryJsonLdVideoObject(item);
        if (jsonLdVideo is not null)
        {
            // Prefer VideoObject references when present, then fill any gaps from OG metadata.
            exportFeedItem.VideoUrl = FirstNonEmpty(
                ReadJsonLdString(jsonLdVideo, "embedUrl"),
                ReadJsonLdString(jsonLdVideo, "contentUrl"),
                ReadJsonLdString(jsonLdVideo, "url"),
                exportFeedItem.VideoUrl);

            if (exportFeedItem.VideoHeight == 0)
            {
                exportFeedItem.VideoHeight = ReadJsonLdInt(jsonLdVideo, "height");
            }

            if (exportFeedItem.VideoWidth == 0)
            {
                exportFeedItem.VideoWidth = ReadJsonLdInt(jsonLdVideo, "width");
            }

            if (string.IsNullOrWhiteSpace(exportFeedItem.Subtitle))
            {
                exportFeedItem.Subtitle = FirstNonEmpty(
                    ReadJsonLdString(jsonLdVideo, "name"),
                    ReadJsonLdString(jsonLdVideo, "headline"),
                    exportFeedItem.Subtitle);
            }

            if (string.IsNullOrWhiteSpace(exportFeedItem.ImageUrl))
            {
                exportFeedItem.ImageUrl = ReadJsonLdString(jsonLdVideo, "thumbnailUrl");
            }

            description = FirstNonEmpty(
                description,
                ReadJsonLdString(jsonLdVideo, "description"));
        }

        if (item.SiteName == "rumble")
        {
            if (string.IsNullOrWhiteSpace(exportFeedItem.VideoUrl))
            {
                var text = item.HtmlAttributes.GetValueOrDefault("ParserResult") ?? "";
                if (!string.IsNullOrEmpty(text) && !text.StartsWith('<'))
                {
                    _log.Debug("EXPORT: Processing rumble.com ld+json metadata");

                    // application/ld+json parser result
                    List<JsonLdVideoObject> list = default;
                    try
                    {
                        list = JsonConvert.DeserializeObject<List<JsonLdVideoObject>>(text);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Error deserialzing json+ld values");
                    }

                    if (list is null)
                    {
                        _log.Error("Error deserializing json+ld values for {urlHash}", exportFeedItem.UrlHash);
                    }

                    var value = list?.FirstOrDefault();
                    if (value is not null)
                    {
                        exportFeedItem.VideoUrl = string.IsNullOrWhiteSpace(value.embedUrl) ? value.url : value.embedUrl;
                        exportFeedItem.VideoHeight = int.TryParse(Convert.ToString(value.height), out int height) ? height : 0;
                        exportFeedItem.VideoWidth = int.TryParse(Convert.ToString(value.width), out int width) ? width : 0;
                    }
                }
            }
        }
        else if (item.SiteName == "bitchute")
        {
            _log.Information("EXPORT: Processing bitchute.com metadata");

            // Bitchute logic is a little convoluted, they don't provide much metadata
            var result = item.HtmlAttributes.GetValueOrDefault("ParserResult") ?? "";
            var start = result.IndexOf("https://");
            var length = result.IndexOf('\"', start) - start;

            exportFeedItem.VideoUrl = result.Substring(start, length);
            exportFeedItem.VideoHeight = 1080;
            exportFeedItem.VideoWidth = 1920;

        }
        else if (string.IsNullOrWhiteSpace(exportFeedItem.VideoUrl))
        {
            _log.Debug("EXPORT: Processing open graph video metadata");

            // Sites that provide video metadata via open graph tags
            exportFeedItem.VideoUrl = item.OpenGraphAttributes.GetValueOrDefault("og:video:secure_url") ??
                item.OpenGraphAttributes.GetValueOrDefault("og:video:url") ??
                item.OpenGraphAttributes.GetValueOrDefault("og:video") ??
                item.OpenGraphAttributes.GetValueOrDefault("og:x:video") ??
                "";
            exportFeedItem.VideoHeight = int.TryParse(item.OpenGraphAttributes.GetValueOrDefault("og:video:height") ??
                item.OpenGraphAttributes.GetValueOrDefault("og:x:video:height") ??
                item.OpenGraphAttributes.GetValueOrDefault("og:image:height"), out int height) ? height : 0;
            exportFeedItem.VideoWidth = int.TryParse(item.OpenGraphAttributes.GetValueOrDefault("og:video:width") ??
                item.OpenGraphAttributes.GetValueOrDefault("og:x:video:width") ??
                item.OpenGraphAttributes.GetValueOrDefault("og:image:width"), out int width) ? width : 0;

            string result = item.HtmlAttributes.GetValueOrDefault("ParserResult") ?? "";
            if (!string.IsNullOrEmpty(result))
                description = result;
        }
        else
        {
            _log.Debug("EXPORT: Using JSON-LD VideoObject defaults for video metadata");
        }

        using (LogContext.PushProperty("hostName", hostName))
        {
            _log.Information("Video URL: '{url}' ({height}x{width})", exportFeedItem.VideoUrl, exportFeedItem.VideoHeight, exportFeedItem.VideoWidth);
        }

        // There's no article text for most video sites, so just use the meta description
        exportFeedItem.ArticleText = $"<p>{description}</p>";
    }

    private static JObject GetPrimaryJsonLdVideoObject(RssFeedItem item)
    {
        if (item.JsonLdObjects == null || item.JsonLdObjects.Count == 0)
        {
            return null;
        }

        foreach (var rootObj in item.JsonLdObjects)
        {
            foreach (var obj in EnumerateJsonObjects(rootObj))
            {
                if (IsJsonLdVideoObject(obj))
                {
                    return obj;
                }
            }
        }

        return null;
    }

    private static IEnumerable<JObject> EnumerateJsonObjects(JToken token)
    {
        if (token is JObject obj)
        {
            yield return obj;
            foreach (var property in obj.Properties())
            {
                foreach (var nested in EnumerateJsonObjects(property.Value))
                {
                    yield return nested;
                }
            }
            yield break;
        }

        if (token is JArray arr)
        {
            foreach (var child in arr)
            {
                foreach (var nested in EnumerateJsonObjects(child))
                {
                    yield return nested;
                }
            }
        }
    }

    private static bool IsJsonLdVideoObject(JObject obj)
    {
        var typeToken = obj["@type"] ?? obj["type"];
        if (typeToken is null)
        {
            return false;
        }

        if (typeToken.Type == JTokenType.String)
        {
            return IsVideoTypeName(typeToken.Value<string>());
        }

        if (typeToken is JArray typeArray)
        {
            foreach (var token in typeArray)
            {
                if (IsVideoTypeName(token.Value<string>()))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsVideoTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        return string.Equals(typeName, "VideoObject", StringComparison.OrdinalIgnoreCase)
            || typeName.EndsWith("/VideoObject", StringComparison.OrdinalIgnoreCase)
            || typeName.EndsWith("#VideoObject", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadJsonLdString(JObject obj, string fieldName)
    {
        if (obj[fieldName] is not JToken token)
        {
            return string.Empty;
        }

        if (token.Type == JTokenType.String)
        {
            return token.Value<string>() ?? string.Empty;
        }

        if (token is JArray arr && arr.Count > 0)
        {
            return arr[0].Type == JTokenType.String ? arr[0].Value<string>() ?? string.Empty : string.Empty;
        }

        return string.Empty;
    }

    private static int ReadJsonLdInt(JObject obj, string fieldName)
    {
        if (obj[fieldName] is not JToken token)
        {
            return 0;
        }

        if (token.Type == JTokenType.Integer)
        {
            return token.Value<int>();
        }

        if (token.Type == JTokenType.String && int.TryParse(token.Value<string>(), out int parsed))
        {
            return parsed;
        }

        return 0;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    protected virtual T GetJsonDynamic<T>(string html, string tagName, string keyName)
    {
        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Query the document by CSS selectors to get the article text
        var blocks = document.QuerySelectorAll(tagName);
        var block = blocks.First(b => b.TextContent.Contains(keyName));

        return JsonConvert.DeserializeObject<T>(block.TextContent);
    }

    protected virtual void SetGraphicMetaData(RssFeedItem item, ExportFeedItem exportFeedItem)
    {
        exportFeedItem.ImageUrl = item.FeedAttributes.Url;
        exportFeedItem.HostName = item.HostName;
        exportFeedItem.SiteName = item.HostName;
    }

    protected virtual string ParseOpenGraphMetaTagAttributes(HtmlDocument doc, string targetAttributeValue)
    {
        string value = ParseMetaTagAttributes(doc, "property", targetAttributeValue, "content");

        if (string.IsNullOrEmpty(value))
        {
            value = ParseMetaTagAttributes(doc, "name", targetAttributeValue, "content");
        }

        return value;
    }

    protected virtual string ParseMetaTagAttributes(HtmlDocument doc, string targetAttributeName, string targetAttributeValue, string sourceAttributeName)
    {
        // Retrieve the requested meta tag by property name
        var node = doc.DocumentNode.SelectSingleNode($"//meta[@{targetAttributeName}='{targetAttributeValue}']");

        // Node can come back null if the meta tag is not present in the DOM
        // Attribute can come back null as well if not present on the meta tag
        string sourceAttributeValue = node?.Attributes[sourceAttributeName]?.Value.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sourceAttributeValue))
        {
            _log.Warning("Error reading attribute '{attribute}' from meta tag '{property}'", targetAttributeName, targetAttributeValue);
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
