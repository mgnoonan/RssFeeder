using RssFeeder.Console.Models;

namespace RssFeeder.Console.Parsers
{
    public interface IArticleParser
    {
        string GetArticleBySelector(string html, SiteArticleDefinition options);
    }
}
