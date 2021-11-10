using RssFeeder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssFeeder.Console.Exporters
{
    internal interface IArticleExporter
    {
        ExportFeedItem FormatItem(RssFeedItem item, RssFeed feed);
    }
}
