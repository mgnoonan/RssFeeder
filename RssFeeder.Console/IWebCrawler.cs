namespace RssFeeder.Console;

public interface IWebCrawler
{
    void Initialize(IContainer container, string crawlerCollectionName, string exportCollectionName);
    void Crawl(RssFeed feed);
    void Export(RssFeed feed, DateTime startDate);
    void Purge(RssFeed feed);
}
