namespace RssFeeder.Console;

public interface IWebCrawler
{
    void Initialize(IContainer container, string crawlerCollectionName, string exportCollectionName);
    void Crawl(Guid runID, RssFeed feed);
    void Export(Guid runID, RssFeed feed, DateTime startDate);
    void Purge(RssFeed feed);
}
