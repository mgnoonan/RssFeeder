using System.Collections.Generic;
using RssFeeder.Models;

namespace RssFeeder.Console.FeedBuilders
{
    public interface IRssFeedBuilder
    {
        List<RssFeedItem> ParseRssFeedItems(RssFeed feed, string html);
        List<RssFeedItem> ParseRssFeedItems(string feedCollectionName, string feedUrl, List<string> feedFilters, string html);
        List<RssFeedItem> ParseRssFeedItems(string html, List<string> filters);
    }
}
