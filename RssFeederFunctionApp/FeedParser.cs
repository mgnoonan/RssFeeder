using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RssFeederFunctionApp.Models;

namespace RssFeederFunctionApp
{
    public static class FeedParser
    {
        [FunctionName("GetArticles")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "rssfeeder",
                collectionName: "drudge-report",
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = "select c.UrlHash from c order by c._ts desc")]IEnumerable<RssFeedItem> existingItems,
            ILogger log,
            //[Blob("working/drudge-report", FileAccess.Write)] Stream workingBlob
            [Queue("feed-items")] IAsyncCollector<string> outputQueue)
        {
            log.LogInformation($"GetArticles trigger function");

            string jsonContent = await new StreamReader(req.Body).ReadToEndAsync();
            RssFeed feed = JsonConvert.DeserializeObject<RssFeed>(jsonContent);

            var items = BuildFeedLinks(log, feed, existingItems, null);

            //Write to a queue and promptly return
            await outputQueue.AddAsync(JsonConvert.SerializeObject(items));

            return (ActionResult)new OkObjectResult($"{items.Count} queued for crawling");
        }

        private static List<RssFeedItem> BuildFeedLinks(ILogger log, RssFeed feed, IEnumerable<RssFeedItem> existingItems, Stream workingBlob)
        {
            string builderName = feed.CustomParser;
            if (string.IsNullOrWhiteSpace(builderName))
                return new List<RssFeedItem>();

            //Type type = Assembly.GetExecutingAssembly().GetType(builderName);
            IRssFeedBuilder builder = (IRssFeedBuilder)Activator.CreateInstance(typeof(DrudgeReportFeedBuilder));
            var list = builder.ParseRssFeedItems(log, feed, out string html)
                //.Take(10) FOR DEBUG PURPOSES
                ;

            // Save the feed source for posterity
            //string filepath = $"{DateTime.Now.ToUniversalTime().ToString("yyyyMMddhhmmss")}_{feed.Url.Replace("://", "_").Replace(".", "_")}.html";
            //log.LogInformation($"Saving file '{filepath}'");
            //using (var writer = new StreamWriter(workingBlob))
            //{
            //    writer.WriteLine(html);
            //}

            var newItems = new List<RssFeedItem>();
            foreach (var item in list)
            {
                if (existingItems.Any(i => i.UrlHash == item.UrlHash))
                {
                    log.LogDebug($"Filter out {item.UrlHash}:{item.Url}");
                    continue;
                }

                newItems.Add(item);
            }

            return newItems;
        }
    }
}

