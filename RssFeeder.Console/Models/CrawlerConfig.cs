namespace RssFeeder.Console.Models;

public record CrawlerConfig
{
    public string[] Exclusions { get; init; }
    public string[] VideoHosts { get; init; }
    public string[] IncludeScripts { get; init; }
    public string[] WebDriver { get; init; }
}
