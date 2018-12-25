using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RssFeederFunctionApp.Models;

namespace RssFeederFunctionApp
{
    public static class ArticleCrawler
    {
        [FunctionName("ArticleCrawler")]
        public static void Run(
            [QueueTrigger("feed-items")]string jsonContent,
            //[Blob("incontainer/{name}", FileAccess.Write)] Stream workingBlob,
            ILogger log)
        {
            var items = JsonConvert.DeserializeObject<List<RssFeedItem>>(jsonContent);

            //log.LogInformation($"{jsonContent}");
            log.LogInformation($"ArticleCrawler Queue trigger function received: {items.Count} feed items to crawl");

            foreach (var item in items)
            {
                log.LogInformation($"Processing: {item.UrlHash}");
            }
        }

        //private static void ParseArticleMetaTags(ILogger log, RssFeedItem item)
        //{
        //    if (File.Exists(item.FileName))
        //    {
        //        // Article was successfully downloaded from the target site
        //        log.LogInformation($"Parsing meta tags from file '{item.FileName}'");

        //        var doc = new HtmlDocument();
        //        doc.Load(item.FileName);

        //        if (!doc.DocumentNode.HasChildNodes)
        //        {
        //            log.LogWarning("No file content found, skipping.");
        //            SetBasicArticleMetaData(item);
        //            return;
        //        }

        //        // Meta tags provide extended data about the item, display as much as possible
        //        SetExtendedArticleMetaData(item, doc);
        //        item.Description = ApplyTemplateToDescription(item, ExtendedTemplate);
        //    }
        //    else
        //    {
        //        // Article failed to download, display minimal basic meta data
        //        SetBasicArticleMetaData(item);
        //        item.Description = ApplyTemplateToDescription(item, BasicTemplate);
        //    }

        //    log.LogInformation(JsonConvert.SerializeObject(item, Newtonsoft.Json.Formatting.Indented));
        //}
    }
}
