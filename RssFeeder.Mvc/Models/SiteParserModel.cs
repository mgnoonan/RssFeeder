using System.ComponentModel.DataAnnotations;

namespace RssFeeder.Mvc.Models
{
    public class SiteParserModel
    {
        [Required]
        public string id { get; set; }
        [Required]
        public string SiteName { get; set; }
        [Required]
        public string ArticleSelector { get; set; }
        [Required]
        public string ParagraphSelector { get; set; }
        [Required]
        public string Parser { get; set; }
    }
}
