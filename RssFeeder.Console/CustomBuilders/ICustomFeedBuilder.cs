using log4net;
using RssFeeder.Console.Models;
using System.Collections.Generic;

namespace RssFeeder.Console.CustomBuilders
{
    public interface ICustomFeedBuilder
    {
        List<FeedItem> ParseFeedItems(ILog log, Feed feed);
    }
}
