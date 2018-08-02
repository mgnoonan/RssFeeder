using log4net;
using RssFeeder.Core.Models;
using System.Collections.Generic;

namespace RssFeeder.Core.CustomBuilders
{
    public interface IRssFeedBuilder
    {
        List<RssFeedItem> ParseRssFeedItems(ILog log, RssFeed feed);
    }
}
