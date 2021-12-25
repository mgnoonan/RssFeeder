namespace RssFeeder.Console.Exporters;

public interface IArticleExporter
{
    ExportFeedItem FormatItem(RssFeedItem item, RssFeed feed);
}
