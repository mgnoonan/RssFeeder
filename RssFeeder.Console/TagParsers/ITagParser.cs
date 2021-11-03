using RssFeeder.Models;

namespace RssFeeder.Console.Parsers
{
    public interface ITagParser
    {
        string GetArticleBySelector(string html, SiteArticleDefinition options);
        string GetArticleBySelector(string html, string bodySelector, string pargraphSelector);
    }
}
