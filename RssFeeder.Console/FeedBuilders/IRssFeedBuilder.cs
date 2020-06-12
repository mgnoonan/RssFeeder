using System.Collections.Generic;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.CustomBuilders
{
    public interface IRssFeedBuilder
    {
        List<RssFeedItem> ParseRssFeedItems(ILogger log, RssFeed feed, out string html);
        List<RssFeedItem> ParseRssFeedItems(ILogger log, string html, List<string> filters);
    }
}
