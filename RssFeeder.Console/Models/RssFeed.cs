﻿using System.Collections.Generic;

namespace RssFeeder.Console.Models
{
    public class RssFeed
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string OutputFile { get; set; }
        public string CustomParser { get; set; }
        public List<RssFeedItem> Items { get; set; }
        public List<string> Filters { get; set; }
    }
}
