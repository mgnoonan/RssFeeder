﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RssFeeder.Models
{
    public class ExportFeedItem
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string FeedId { get; set; }
        public string Url { get; set; }
        public string UrlHash { get; set; }
        public string ImageUrl { get; set; }
        public string VideoUrl { get; set; }
        public int VideoHeight { get; set; }
        public int VideoWidth { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string ArticleText { get; set; }
        public string Description { get; set; }
        public string MetaDescription { get; set; }
        public DateTime DateAdded { get; set; }
        public string FileName { get; set; }
        public string SiteName { get; set; }
        public string HostName { get; set; }
        public string LinkLocation { get; set; }
    }
}
