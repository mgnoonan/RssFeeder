namespace RssFeeder.Models;

public record RouteTemplate
{
    public string Name { get; set; }
    public string Parser { get; set; }
    public string Path { get; set; }
    public string ArticleSelector { get; set; }
    public string ParagraphSelector { get; set; }
}
