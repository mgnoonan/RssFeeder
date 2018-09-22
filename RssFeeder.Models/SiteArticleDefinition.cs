namespace RssFeeder.Models
{
    public class SiteArticleDefinition
    {
        public string Id { get; set; }
        public string SiteName { get; set; }
        public string BulletSelector { get; set; }
        public string ArticleSelector { get; set; }
        public string ParagraphSelector { get; set; }
        public string Parser { get; set; }
        public string TestFilename { get; set; }
        public string TestUrl { get; set; }
    }
}
