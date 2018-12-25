using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RssFeederFunctionApp.Models;

namespace RssFeederFunctionApp
{
    public interface IRssFeedBuilder
    {
        List<RssFeedItem> ParseRssFeedItems(ILogger log, RssFeed feed, out string html);
        List<RssFeedItem> ParseRssFeedItems(ILogger log, string html, List<string> filters);
    }
}
