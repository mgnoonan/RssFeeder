namespace RssFeeder.Console.Models;

public class CrawlerConfigOptions
{
    public const string SectionName = "CrawlerConfig";

    public CrawlerConfigSource? Source { get; set; }
    public string FilePath { get; set; }
}