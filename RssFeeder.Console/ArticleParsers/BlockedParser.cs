using RssFeeder.Models;

namespace RssFeeder.Console.Parsers
{
    public class BlockedParser : IArticleParser
    {
        public string GetArticleBySelector(string html, SiteArticleDefinition options)
        {
            return $"<p>This site does not allow content crawling. Click the link below to read the article in your browser.</p>";
        }
    }
}
