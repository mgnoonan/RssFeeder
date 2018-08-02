using RssFeeder.Core.Models;

namespace RssFeeder.Core.Parsers
{
    public interface IArticleParser
    {
        string GetArticleBySelector(string html, SiteArticleDefinition options);
    }
}
