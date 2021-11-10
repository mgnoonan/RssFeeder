using AngleSharp.Html.Parser;
using Antlr4.StringTemplate;
using HtmlAgilityPack;
using Newtonsoft.Json;
using RssFeeder.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssFeeder.Console.Exporters
{
    internal class CosmosDbArticleExporter : BaseArticleExporter, IArticleExporter
    {
        public ExportFeedItem FormatItem(RssFeedItem item, RssFeed feed)
        {
            var exportFeedItem = new ExportFeedItem
            {
                Id = Guid.NewGuid().ToString(),
                FeedId = item.FeedAttributes.FeedId,
                Url = item.FeedAttributes.Url,
                UrlHash = item.FeedAttributes.UrlHash
            };

            if (item.FeedAttributes.FileName.EndsWith(".png") || item.FeedAttributes.FileName.EndsWith(".jpg") || item.FeedAttributes.FileName.EndsWith(".gif"))
            {
                SetGraphicMetaData(item, exportFeedItem);
                exportFeedItem.Description = ApplyTemplateToDescription(item, feed, ExportTemplates.GraphicTemplate);
            }

            return exportFeedItem;
        }
    }
}
