using RssFeeder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssFeeder.Console.Exporters
{
    internal class RavenDbArticleExporter : BaseArticleExporter, IArticleExporter
    {
        public ExportFeedItem FormatItem(RssFeedItem item, RssFeed feed)
        {
            var exportFeedItem = new ExportFeedItem
            {
                FeedId = item.FeedAttributes.FeedId,
                Url = item.FeedAttributes.Url,
                UrlHash = item.FeedAttributes.UrlHash
            };

            if (item.FeedAttributes.FileName.EndsWith(".png") || item.FeedAttributes.FileName.EndsWith(".jpg") || item.FeedAttributes.FileName.EndsWith(".gif"))
            {
                SetGraphicMetaData(item, exportFeedItem);
                exportFeedItem.Description = ApplyTemplateToDescription(item, feed, ExportTemplates.GraphicTemplate);
            }

            if (Config.VideoHosts.Contains(item.HostName))
            {
                //SetVideoMetaData(item, doc, item.HostName);
                if (exportFeedItem.VideoHeight > 0)
                {
                    exportFeedItem.Description = ApplyTemplateToDescription(item, feed, ExportTemplates.VideoTemplate);
                }
                else
                {
                    exportFeedItem.Description = ApplyTemplateToDescription(item, feed, ExportTemplates.ExtendedTemplate);
                }
            }

            return exportFeedItem;
        }
    }
}
