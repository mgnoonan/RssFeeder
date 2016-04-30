using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RssFeeder.Console.Models
{
    public class Feed
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string Filename { get; set; }
        public string CustomParser { get; set; }
        public List<FeedItem> Items { get; set; }
        public List<string> Filters { get; set; }
    }
}
