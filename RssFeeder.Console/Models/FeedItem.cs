using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

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
    }
}
