namespace RssFeeder.Console.Database;

public interface ICrawlerConfigProvider
{
    CrawlerConfig GetConfig();
}