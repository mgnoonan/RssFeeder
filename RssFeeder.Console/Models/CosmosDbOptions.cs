namespace RssFeeder.Console.Models;

public class CosmosDbOptions
{
    public const string SectionName = "CosmosDb";

    public string Account { get; set; }
    public string Key { get; set; }
    public string DatabaseName { get; set; } = "rssfeeder";
}