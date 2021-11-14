using System;
using System.Collections.Generic;
using System.Linq;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.Exporters;

public class ArticleExporter : BaseArticleExporter, IArticleExporter
{
    public ExportFeedItem FormatItem(RssFeedItem item, RssFeed feed)
    {
        var exportFeedItem = new ExportFeedItem
        {
            Id = Guid.NewGuid().ToString(),
            FeedId = item.FeedAttributes.FeedId,
            Url = item.FeedAttributes.Url,
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

        if (Config.VideoHosts.Contains(hostName))
        {
            Log.Information("Applying video metadata values for '{hostname}'", hostName);
            SetVideoMetaData(exportFeedItem, item, hostName);
            if (exportFeedItem.VideoHeight > 0)
            {
                exportFeedItem.ArticleText = ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.VideoTemplate);
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
                Log.Information("No parsed result, applying basic metadata values for '{hostname}'", hostName);

                // Article failed to download, display minimal basic meta data
                SetBasicArticleMetaData(exportFeedItem, item, hostName);
                exportFeedItem.ArticleText = ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.BasicTemplate);
            }
            else
            {
                Log.Information("Applying extended metadata values for '{hostname}'", hostName);

                SetExtendedArticleMetaData(exportFeedItem, item, hostName);
                exportFeedItem.ArticleText = ApplyTemplateToDescription(exportFeedItem, feed, ExportTemplates.ExtendedTemplate);
            }
        }

        return exportFeedItem;
    }
}
