namespace RssFeeder.Console.TagParsers;

public interface ITagParser
{
    void Initialize(string sourceHtml, RssFeedItem item);
    string ParseTagsBySelector(ArticleRouteTemplate template);
    void PreParse();
    void PostParse();
}
