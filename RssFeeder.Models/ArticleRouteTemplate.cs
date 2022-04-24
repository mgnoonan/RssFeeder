namespace RssFeeder.Models;

public record ArticleRouteTemplate
{
    public string Name { get; set; }
    public string Parser { get; set; }
    public string Template { get; set; }
    public string ArticleSelector { get; set; }
    public string ParagraphSelector { get; set; }
}
