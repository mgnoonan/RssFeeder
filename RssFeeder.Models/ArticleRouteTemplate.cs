namespace RssFeeder.Models;

public record ArticleRouteTemplate
{
    public string Name { get; init; }
    public string Parser { get; init; }
    public string Template { get; init; }
    public string ArticleSelector { get; init; }
    public string ParagraphSelector { get; init; }
    public string EmbeddedArticleUrlSelector { get; init; }
}
