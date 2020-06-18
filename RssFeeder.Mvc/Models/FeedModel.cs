using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RssFeeder.Mvc.Models
{
    public class FeedModel
    {
        [Required]
        public string id { get; set; }
        [Required]
        public string title { get; set; }
        [Required]
        public string url { get; set; }
        [Required]
        public string description { get; set; }
        [Required]
        public string outputfile { get; set; }
        [Required]
        public string language { get; set; }
        [Required]
        public string customparser { get; set; }
        public List<string> filters { get; set; }
        public string collectionname { get; set; }
        public string authoremail { get; set; }
    }
}
