namespace RssFeeder.Models;

public record SiteArticleDefinition
{
    public string Id { get; init; }
    public string SiteName { get; init; }
    public string ArticleSelector { get; init; }
    public string ParagraphSelector { get; init; }
    public string Parser { get; init; }
    public ArticleRouteTemplate[] RouteTemplates { get; init; }
}
