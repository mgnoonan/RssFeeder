using System.ComponentModel.DataAnnotations;

namespace RssFeeder.Mvc.Models
{
    public class SiteParserModel
    {
        public string id { get; set; }
        public string SiteName { get; set; }
        public string ArticleSelector { get; set; }
        public string ParagraphSelector { get; set; }
        public string Parser { get; set; }
    }
}
