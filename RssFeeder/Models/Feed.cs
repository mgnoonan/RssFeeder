using Newtonsoft.Json;
using System.Collections.Generic;

namespace RssFeeder.Models
{
    public class Feed
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        [JsonProperty(PropertyName = "language")]
        public string Language { get; set; }
        [JsonProperty(PropertyName = "customparser")]
        public string CustomParser { get; set; }
        [JsonProperty(PropertyName = "filters")]
        public List<string> Filters { get; set; }
    }
}
