namespace RssFeeder.Console.TagParsers;

public interface ITagParser
{
    void Initialize(string sourceHtml, RssFeedItem item);
    string ParseTagsBySelector(SiteArticleDefinition options);
    string ParseTagsBySelector(string bodySelector, string paragraphSelector);
    void PreParse();
    void PostParse();
}
