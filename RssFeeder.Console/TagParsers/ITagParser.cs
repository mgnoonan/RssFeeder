using RssFeeder.Models;

namespace RssFeeder.Console.Parsers
{
    public interface ITagParser
    {
        string ParseTagsBySelector(string html, SiteArticleDefinition options);
        string ParseTagsBySelector(string html, string bodySelector, string pargraphSelector);
    }
}
