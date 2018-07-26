namespace RssFeeder.Console.Parsers
{
    public interface ISiteParser
    {
        string GetArticleBySelector(string html, string articleSelector, string paragraphSelector);
        string GetArticleText(string html);
    }
}
