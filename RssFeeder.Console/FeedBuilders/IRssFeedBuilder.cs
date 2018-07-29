using log4net;
using RssFeeder.Console.Models;
using System.Collections.Generic;

namespace RssFeeder.Console.CustomBuilders
{
    public interface IRssFeedBuilder
    {
        List<RssFeedItem> ParseRssFeedItems(ILog log, RssFeed feed);
    }
}
