using System.Collections.Generic;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.FeedBuilders
{
    public interface IRssFeedBuilder
    {
        List<RssFeedItem> ParseRssFeedItems(ILogger log, RssFeed feed, string html);
        List<RssFeedItem> ParseRssFeedItems(ILogger log, string html, List<string> filters);
    }
}
