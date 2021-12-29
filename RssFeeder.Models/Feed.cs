namespace RssFeeder.Models;

public record Feed
{
    public string id { get; set; }
    public string description { get; set; }
    public string url { get; set; }
}
