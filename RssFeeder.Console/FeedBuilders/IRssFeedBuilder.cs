using RssFeeder.Models;
using System.Collections.Generic;

namespace RssFeeder.Console.FeedBuilders
{
    public interface IRssFeedBuilder
    {
        List<RssFeedItem> ParseRssFeedItems(RssFeed feed, string html);
        List<RssFeedItem> ParseRssFeedItems(string html, List<string> filters);
    }
}
