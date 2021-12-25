using System.Collections.Generic;

namespace RssFeeder.Models;

    public class RssFeed
    {
        public int Id { get; set; }
        public bool Enabled { get; set; }
        public bool EnableThumbnail { get; set; }
        public bool Exportable { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string OutputFile { get; set; }
        public string CustomParser { get; set; }
        public string CollectionName { get; set; }
        public short FileRetentionDays { get; set; }
        public short DatabaseRetentionDays { get; set; }
        public List<string> Filters { get; set; }
    }
