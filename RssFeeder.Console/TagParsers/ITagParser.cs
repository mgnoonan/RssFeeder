namespace RssFeeder.Console.TagParsers;

public interface ITagParser
{
    string ParseTagsBySelector(string html, SiteArticleDefinition options);
    string ParseTagsBySelector(string html, string bodySelector, string paragraphSelector);
}
