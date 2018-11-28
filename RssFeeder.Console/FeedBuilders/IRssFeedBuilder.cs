using System.Collections.Generic;
using log4net;
using RssFeeder.Models;

namespace RssFeeder.Console.CustomBuilders
{
    public interface IRssFeedBuilder
    {
        List<RssFeedItem> ParseRssFeedItems(ILog log, RssFeed feed, out string html);
        List<RssFeedItem> ParseRssFeedItems(ILog log, string html, List<string> filters);
    }
}
