using Newtonsoft.Json;
using System;

namespace RssFeeder.Models
{
    public class FeedItem
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }
        [JsonProperty(PropertyName = "feed")]
        public int FeedID { get; set; }
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
        [JsonProperty(PropertyName = "urlhash")]
        public string UrlHash { get; set; }
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        [JsonProperty(PropertyName = "dateadded")]
        public DateTime DateAdded { get; set; }        
    }
}
