namespace RssFeeder.Console.Exporters;

public class ArticleExporter : BaseArticleExporter, IArticleExporter
{
    private readonly ILogger _log;

    public ArticleExporter(ILogger log) : base(log)
    {
        _log = log;
    }

    public ExportFeedItem FormatItem(RssFeedItem item, RssFeed feed)
    {
        var exportFeedItem = new ExportFeedItem
        {
            Id = Guid.NewGuid().ToString(),
            FeedId = item.FeedAttributes.FeedId,
            Url = GetCanonicalUrl(item),
            UrlHash = item.FeedAttributes.UrlHash,
            DateAdded = item.FeedAttributes.DateAdded,
            LinkLocation = item.FeedAttributes.LinkLocation,
            Title = item.FeedAttributes.Title.Replace("\n", " | "),
            Description = item.OpenGraphAttributes.GetValueOrDefault("og:description")
        };

        Uri uri = new Uri(exportFeedItem.Url);
        string hostName = uri.GetComponents(UriComponents.Host, UriFormat.Unescaped).ToLower();

        var fileName = item.FeedAttributes.FileName ?? "";
        if (IsGraphicFile(fileName))
        {
            SetGraphicMetaData(item, exportFeedItem);
            exportFeedItem.ArticleText = ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.GraphicTemplate);
            return exportFeedItem;
        }

        bool hasJsonLdVideoObject = HasJsonLdVideoObject(item);
        string videoUrl = GetVideoUrl(item);
        string videoType = GetVideoType(item, videoUrl, hasJsonLdVideoObject);

        if (HasSupportedVideoFormat(item, videoUrl, videoType, hasJsonLdVideoObject))
        {
            _log.Debug("Applying video metadata values for '{hostname}'", hostName);
            SetVideoMetaData(exportFeedItem, item, hostName);
            if (exportFeedItem.VideoHeight > 0)
            {
                exportFeedItem.ArticleText = GetVideoTemplate(exportFeedItem, feed, videoType);
            }
            else
            {
                exportFeedItem.ArticleText = ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.ExtendedTemplate);
            }
        }
        else
        {
            bool useTitle = item.HostName == "twitter.com";
            string result = GetParsedResult(item, useTitle);
            string imageUrl = item.OpenGraphAttributes.GetValueOrDefault("og:image") ?? null;
            if (string.IsNullOrEmpty(result))
            {
                _log.Information("No parsed result, applying basic metadata values for '{hostname}'", hostName);
                SetBasicArticleMetaData(exportFeedItem, item, hostName);
                exportFeedItem.ArticleText = GetBasicTemplate(exportFeedItem, feed, imageUrl);
            }
            else
            {
                _log.Information("Applying extended metadata values for '{hostname}'", hostName);
                SetExtendedArticleMetaData(exportFeedItem, item, hostName);
                exportFeedItem.ArticleText = ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.ExtendedTemplate);
            }
        }

        return exportFeedItem;
    }

    private bool IsGraphicFile(string fileName)
    {
        return fileName.EndsWith(".png") ||
            fileName.EndsWith(".jpg") ||
            fileName.EndsWith(".gif") ||
            fileName.EndsWith(".pdf") ||
            fileName.EndsWith(".mp3") ||
            fileName.EndsWith(".webp");
    }

    private string GetVideoUrl(RssFeedItem item)
    {
        return item.OpenGraphAttributes.GetValueOrDefault("og:video:secure_url") ??
            item.OpenGraphAttributes.GetValueOrDefault("og:video:url") ??
            item.OpenGraphAttributes.GetValueOrDefault("og:video") ??
            item.OpenGraphAttributes.GetValueOrDefault("og:x:video") ??
            "";
    }

    private string GetVideoType(RssFeedItem item, string videoUrl, bool hasJsonLdVideoObject)
    {
        string videoType;
        if (item.OpenGraphAttributes.TryGetValue("og:video:type", out string value))
        {
            videoType = value;
        }
        else if (item.OpenGraphAttributes.TryGetValue("og:x:video:type", out value))
        {
            videoType = value;
        }
        else if (videoUrl.EndsWith(".mp4") || item.SiteName == "bitchute")
        {
            videoType = "video/mp4";
        }
        else if (videoUrl.Contains("youtube.com") || item.SiteName == "rumble")
        {
            videoType = "text/html";
        }
        else if (hasJsonLdVideoObject)
        {
            if (videoUrl.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
            {
                videoType = "application/x-mpegURL";
            }
            else if (videoUrl.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                videoType = "video/mp4";
            }
            else
            {
                // Most JSON-LD video references are embed pages.
                videoType = "text/html";
            }
        }
        else
        {
            videoType = "";
        }

        return videoType;
    }

    private bool HasSupportedVideoFormat(RssFeedItem item, string videoUrl, string videoType, bool hasJsonLdVideoObject)
    {
        return (videoUrl.Length > 0 || hasJsonLdVideoObject || item.SiteName == "rumble" || item.SiteName == "bitchute") &&
            (videoType == "text/html" || videoType == "video/mp4" || videoType == "application/x-mpegURL");
    }

    private bool HasJsonLdVideoObject(RssFeedItem item)
    {
        if (item.JsonLdObjects == null || item.JsonLdObjects.Count == 0)
        {
            return false;
        }

        foreach (var obj in item.JsonLdObjects)
        {
            if (obj == null)
            {
                continue;
            }

            var typeToken = obj["@type"] ?? obj["type"];
            if (typeToken == null)
            {
                continue;
            }

            if (typeToken.Type == JTokenType.String && IsVideoTypeName(typeToken.Value<string>()))
            {
                return true;
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

    private string GetParsedResult(RssFeedItem item, bool useTitle)
    {
        return useTitle ? item.OpenGraphAttributes.GetValueOrDefault("og:title") : item.HtmlAttributes.GetValueOrDefault("ParserResult") ?? "";
    }

    private string GetBasicTemplate(ExportFeedItem exportFeedItem, RssFeed feed, string imageUrl)
    {
        return ApplyTemplateToDescription(exportFeedItem, feed, string.IsNullOrEmpty(imageUrl) ? ExportTemplates.BasicTemplate : ExportTemplates.BasicPlusTemplate);
    }

    private string GetVideoTemplate(ExportFeedItem exportFeedItem, RssFeed feed, string videoType)
    {
        if (videoType == "video/mp4" || videoType == "application/x-mpegURL")
        {
            return ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.Mp4VideoTemplate);
        }
        else
        {
            return ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.HtmlVideoTemplate);
        }
    }
}
