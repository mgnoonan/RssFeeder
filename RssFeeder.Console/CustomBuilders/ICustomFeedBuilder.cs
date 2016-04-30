using log4net;
using RssFeeder.Console.Models;
using System.Collections.Generic;

namespace RssFeeder.Console.CustomBuilders
{
    public interface ICustomFeedBuilder
    {
        List<FeedItem> Build(ILog log, Feed feed);
    }
}
