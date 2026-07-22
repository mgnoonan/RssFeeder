namespace RssFeeder.Console;

public interface IWebCrawler
{
    void Initialize(IFeedBuilderResolver feedBuilderResolver, ITagParserResolver tagParserResolver, string crawlerCollectionName, string exportCollectionName);
    void Crawl(Guid runID, RssFeed feed);
    List<RssFeedItem> Audit(Guid runID, RssFeed feed);
    void Export(Guid runID, RssFeed feed, DateTime startDate);
    void Purge(RssFeed feed);
}
