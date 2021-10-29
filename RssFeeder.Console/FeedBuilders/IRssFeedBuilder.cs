﻿using System.Collections.Generic;
using RssFeeder.Models;

namespace RssFeeder.Console.FeedBuilders
{
    public interface IRssFeedBuilder
    {
        List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html);
        List<RssFeedItem> GenerateRssFeedItemList(string feedCollectionName, string feedUrl, List<string> feedFilters, string html);
        List<RssFeedItem> GenerateRssFeedItemList(string html, List<string> filters);
    }
}
