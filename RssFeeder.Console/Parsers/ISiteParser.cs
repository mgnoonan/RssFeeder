using RssFeeder.Console.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssFeeder.Console.Parsers
{
    public interface ISiteParser
    {
        void Load(FeedItem item);
        void Parse();
    }
}
