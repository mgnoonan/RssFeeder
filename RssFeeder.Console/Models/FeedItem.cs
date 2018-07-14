using System;

namespace RssFeeder.Console.Models
{
    public class FeedItem
    {
        public int Id { get; set; }
        public int FeedId { get; set; }
        public string Url { get; set; }
        public string UrlHash { get; set; }
        public string ImageUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DateAdded { get; set; }
        public string FileName { get; set; }
    }
}
