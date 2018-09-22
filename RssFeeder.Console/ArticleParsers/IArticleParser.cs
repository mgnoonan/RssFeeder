using RssFeeder.Models;

namespace RssFeeder.Console.Parsers
{
    public interface IArticleParser
    {
        string GetArticleBySelector(string html, SiteArticleDefinition options);
    }
}
