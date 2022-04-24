namespace RssFeeder.Models;

public record SiteArticleDefinition
{
    public string Id { get; set; }
    public string SiteName { get; set; }
    public string ArticleSelector { get; set; }
    public string ParagraphSelector { get; set; }
    public string Parser { get; set; }
    public ArticleRouteTemplate[] RouteTemplates { get; set; }
}
