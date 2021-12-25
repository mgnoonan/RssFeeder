namespace RssFeeder.Models;

public class SiteArticleDefinition
{
    public string Id { get; set; }
    public string SiteName { get; set; }
    public string BulletSelector { get; set; }
    public string ArticleSelector { get; set; }
    public string ParagraphSelector { get; set; }
    public string Parser { get; set; }
    public string TestArticleFilename { get; set; }
    public string TestArticleUrl { get; set; }
    public string TestFeedFilename { get; set; }
}
