namespace RssFeeder.Console.Exporters;

public class ArticleExporter : BaseArticleExporter, IArticleExporter
{
    public ExportFeedItem FormatItem(RssFeedItem item, RssFeed feed)
    {
        // The UrlHash is a hash of the feed source, not the ultimate target URL. This is to avoid
        // over-crawling with link shortening services such as bit.ly and t.co. Once we detect a hash
        // has been crawled from the source, there is no need to crawl again. It means the hash does
        // not truly reflect the target URL, but that's ok as there are duplicate crawls across the 
        // different feeds anyway.
        var exportFeedItem = new ExportFeedItem
        {
            Id = Guid.NewGuid().ToString(),
            FeedId = item.FeedAttributes.FeedId,
            Url = GetCanonicalUrl(item),
            UrlHash = item.FeedAttributes.UrlHash,
            DateAdded = item.FeedAttributes.DateAdded,
            LinkLocation = item.FeedAttributes.LinkLocation,
            Title = item.FeedAttributes.Title
        };

        Uri uri = new Uri(exportFeedItem.Url);
        string hostName = uri.GetComponents(UriComponents.Host, UriFormat.Unescaped).ToLower();

        var fileName = item.FeedAttributes.FileName ?? "";
        if (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".gif"))
        {
            SetGraphicMetaData(item, exportFeedItem);
            exportFeedItem.ArticleText = ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.GraphicTemplate);
            return exportFeedItem;
        }

        string videoType = item.OpenGraphAttributes.GetValueOrDefault("og:video:type") ?? "";
        bool hasSupportedVideoFormat = (item.OpenGraphAttributes.ContainsKey("og:video:secure_url") ||
            item.OpenGraphAttributes.ContainsKey("og:video:url") ||
            item.OpenGraphAttributes.ContainsKey("og:video") ||
            item.SiteName == "rumble") && 
            (videoType == "text/html" || videoType == "video/mp4");

        if (hasSupportedVideoFormat)
        {
            Log.Debug("Applying video metadata values for '{hostname}'", hostName);
            SetVideoMetaData(exportFeedItem, item, hostName);
            if (exportFeedItem.VideoHeight > 0)
            {
                if (videoType == "video/mp4")
                {
                    exportFeedItem.ArticleText = ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.Mp4VideoTemplate);
                }
                else
                {
                    exportFeedItem.ArticleText = ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.HtmlVideoTemplate);
                }
            }
            else
            {
                exportFeedItem.ArticleText = ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.ExtendedTemplate);
            }
        }
        else
        {
            var result = item.HtmlAttributes.GetValueOrDefault("ParserResult") ?? "";
            if (string.IsNullOrEmpty(result))
            {
                Log.Debug("No parsed result, applying basic metadata values for '{hostname}'", hostName);

                // Article failed to download, display minimal basic meta data
                SetBasicArticleMetaData(exportFeedItem, item, hostName);
                exportFeedItem.ArticleText = ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.BasicTemplate);
            }
            else
            {
                Log.Debug("Applying extended metadata values for '{hostname}'", hostName);

                SetExtendedArticleMetaData(exportFeedItem, item, hostName);
                exportFeedItem.ArticleText = ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.ExtendedTemplate);
            }
        }

        return exportFeedItem;
    }
}
